using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Text;
using Dalamud.Plugin.Services;
using LMeter.Act.DataStructures;
using LMeter.Config;
using LMeter.Helpers;
using Newtonsoft.Json.Linq;

namespace LMeter.Act
{
    public abstract class LogClient(ActConfig config) : IPluginDisposable
    {
        protected const string SubscriptionMessage = "{\"call\":\"subscribe\",\"events\":[\"CombatData\",\"LogLine\"]}";

        protected ActConfig Config { get; set; } = config;

        public ConnectionStatus Status { get; protected set; } = ConnectionStatus.NotConnected;
        public List<ActEvent> PastEvents { get; protected init; } = [];

        public FFLogsClient? _fflogsClient = config.UseFFLogs ? new FFLogsClient() : null;
        private ActEvent? _lastEvent;
        private ActEvent? _currentEvent;

        public abstract void Start();
        public abstract void Shutdown();
        public abstract void Reset();

        public ActEvent? GetEvent(int index = -1)
        {
            if (index >= 0 && index < this.PastEvents.Count)
            {
                return this.PastEvents[index];
            }

            return _currentEvent;
        }

        public void ToggleFFlogsUsage()
        {
            if (this.Config.UseFFLogs)
            {
                _fflogsClient?.Dispose();
                _fflogsClient = new FFLogsClient();
            }
            else
            {
                _fflogsClient?.Dispose();
                _fflogsClient = null;
            }
        }

        protected void ParseLogData(string data)
        {
            this.ParseLogData(JObject.Parse(data));
        }

        protected void ParseLogData(JObject data)
        {
            if (data.ContainsKey("type"))
            {
                string? value = data.GetValue("type")?.ToString();
                if (!string.IsNullOrEmpty(value))
                {
                    if (this.Config.UseFFLogs &&
                        (!this.Config.DisableFFLogsOutsideDuty || CharacterState.IsInDuty()) &&
                        value.Equals("LogLine"))
                    {
                        string? logLine = data.GetValue("rawLine")?.ToString();
                        if (!string.IsNullOrEmpty(logLine))
                        {
                            _fflogsClient?.ParseLine(logLine);
                        }
                    }
                    else if (value.Equals("CombatData"))
                    {
                        this.HandleNewEvent(data.ToObject<ActEvent>());
                    }
                }
            }
        }

        private void HandleNewEvent(ActEvent? newEvent)
        {
            if (newEvent?.Encounter is not null &&
                newEvent?.Combatants is not null &&
                newEvent.Combatants.Count != 0 &&
                !newEvent.Equals(_lastEvent))
            {
                if (!newEvent.IsEncounterActive() &&
                    !newEvent.Equals(this.PastEvents.LastOrDefault()))
                {
                    this.PastEvents.Add(newEvent);
                    while (this.PastEvents.Count > Config.EncounterHistorySize)
                    {
                        this.PastEvents.RemoveAt(0);
                    }
                }

                if (this.Config.UseFFLogs &&
                    (!this.Config.DisableFFLogsOutsideDuty || CharacterState.IsInDuty()))
                {
                    newEvent.InjectFFLogsData(_fflogsClient?.CollectMeters());
                }

                _lastEvent = newEvent;
                _currentEvent = newEvent;
            }
        }

        public virtual void EndEncounter()
        {
            IChatGui chat = Singletons.Get<IChatGui>();
            XivChatEntry message = new()
            {
                Message = "end",
                Type = XivChatType.Echo
            };

            chat.Print(message);
        }

        public virtual void Clear()
        {
            _currentEvent = null;
            this.PastEvents.Clear();
            _fflogsClient?.Reset();
            if (Config.ClearAct)
            {
                IChatGui chat = Singletons.Get<IChatGui>();
                XivChatEntry message = new()
                {
                    Message = "clear",
                    Type = XivChatType.Echo
                };

                chat.Print(message);
            }
        }

        protected static void LogConnectionFailure(string error, Exception? ex = null)
        {
            Singletons.Get<IPluginLog>().Error(error);
            if (ex is not null)
            {
                Singletons.Get<IPluginLog>().Error(ex.ToString());
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.Shutdown();
                this._fflogsClient?.Dispose();
            }
        }
    }
}
