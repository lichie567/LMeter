using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using LMeter.Config;
using LMeter.Helpers;
using Newtonsoft.Json.Linq;

namespace LMeter.ACT
{
    public class IINACTClient : IACTClient
    {
        private readonly ACTConfig _config;
        private readonly DalamudPluginInterface _dpi;
        private ACTEvent? _lastEvent;
        private readonly ICallGateProvider<JObject, bool> _combatEventReaderIpc;

        private const string LMeterSubscriptionIpcEndpoint = "LMeter.SubscriptionReceiver";
        private const string IINACTSubscribeIpcEndpoint = "IINACT.CreateSubscriber";
        private const string IINACTUnsubscribeIpcEndpoint = "IINACT.Unsubscribe";
        private const string IINACTProviderEditEndpoint = "IINACT.IpcProvider." + LMeterSubscriptionIpcEndpoint;
        private readonly JObject SubscriptionMessageObject = JObject.Parse(ACTClient.SubscriptionMessage);

        public ConnectionStatus Status { get; private set; }
        public List<ACTEvent> PastEvents { get; private set; }

        public IINACTClient(ACTConfig config, DalamudPluginInterface dpi)
        {
            _config = config;
            _dpi = dpi;
            Status = ConnectionStatus.NotConnected;
            PastEvents = new List<ACTEvent>();

            _combatEventReaderIpc = _dpi.GetIpcProvider<JObject, bool>(LMeterSubscriptionIpcEndpoint);
            _combatEventReaderIpc.RegisterFunc(ReceiveIpcMessage);
        }

        public ACTEvent? GetEvent(int index = -1)
        {
            if (index >= 0 && index < PastEvents.Count)
            {
                return PastEvents[index];
            }
            
            return _lastEvent;
        }

        public void EndEncounter()
        {
            ChatGui chat = Singletons.Get<ChatGui>();
            XivChatEntry message = new XivChatEntry()
            {
                Message = "end",
                Type = XivChatType.Echo
            };

            chat.PrintChat(message);
        }

        public void Clear()
        {
            _lastEvent = null;
            PastEvents = new List<ACTEvent>();
            if (_config.ClearACT)
            {
                ChatGui chat = Singletons.Get<ChatGui>();
                XivChatEntry message = new XivChatEntry()
                {
                    Message = "clear",
                    Type = XivChatType.Echo
                };

                chat.PrintChat(message);
            }
        }

        public void RetryConnection()
        {
            Reset();
            Start();
        }

        public void Start()
        {
            if (Status != ConnectionStatus.NotConnected)
            {
                PluginLog.Error("Cannot start, IINACTClient needs to be reset!");
                return;
            }

            if (!Connect()) return;

            Status = ConnectionStatus.Connected;
            PluginLog.Information("Successfully subscribed to IINACT");
        }

        private bool Connect()
        {
            Status = ConnectionStatus.Connecting;

            try
            {
                var connectSuccess = _dpi
                    .GetIpcSubscriber<string, bool>(IINACTSubscribeIpcEndpoint)
                    .InvokeFunc(LMeterSubscriptionIpcEndpoint);
                if (!connectSuccess) return false;
            }
            catch (Exception ex)
            {
                Status = ConnectionStatus.ConnectionFailed;
                PluginLog.Debug("Failed to setup IINACT subscription!");
                PluginLog.Verbose(ex.ToString());
                return false;
            }
            
            try
            {
                _dpi
                    .GetIpcSubscriber<JObject, bool>(IINACTProviderEditEndpoint)
                    .InvokeAction(SubscriptionMessageObject);
            }
            catch (Exception ex)
            {
                Status = ConnectionStatus.ConnectionFailed;
                PluginLog.Debug("Failed to finalize IINACT subscription!");
                PluginLog.Verbose(ex.ToString());
                return false;
            }

            return true;
        }

        private bool ReceiveIpcMessage(JObject data)
        {
            try
            {
                ACTEvent? newEvent = data.ToObject<ACTEvent?>();

                if (newEvent?.Encounter is not null &&
                    newEvent?.Combatants is not null &&
                    newEvent.Combatants.Any() &&
                    (CharacterState.IsInCombat() || !newEvent.IsEncounterActive()))
                {
                    if (!(_lastEvent is not null &&
                          _lastEvent.IsEncounterActive() == newEvent.IsEncounterActive() &&
                          _lastEvent.Encounter is not null &&
                          _lastEvent.Encounter.Duration.Equals(newEvent.Encounter.Duration)))
                    {
                        if (!newEvent.IsEncounterActive())
                        {
                            PastEvents.Add(newEvent);

                            while (PastEvents.Count > _config.EncounterHistorySize)
                            {
                                PastEvents.RemoveAt(0);
                            }
                        }

                        newEvent.Timestamp = DateTime.UtcNow;
                        _lastEvent = newEvent;
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLog.Verbose(ex.ToString());
                return false;
            }

            return true;
        }
        
        public void Shutdown()
        {
            try
            {
                var success = _dpi
                    .GetIpcSubscriber<string, bool>(IINACTUnsubscribeIpcEndpoint)
                    .InvokeFunc(LMeterSubscriptionIpcEndpoint);

                PluginLog.Information(
                    success
                        ? "Successfully unsubscribed from IINACT"
                        : "Failed to unsubscribe from IINACT"
                );
            }
            catch (Exception)
            {
                // don't throw when closing
            }

            Status = ConnectionStatus.NotConnected;
        }

        public void Reset()
        {
            this.Shutdown();
            Status = ConnectionStatus.NotConnected;
        }

        public void Dispose()
        {
            _combatEventReaderIpc.UnregisterFunc();
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.Shutdown();
            }
        }
    }
}
