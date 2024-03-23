using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Text;
using Dalamud.Plugin.Services;
using LMeter.Act.DataStructures;
using LMeter.Config;
using LMeter.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LMeter.Act
{
    public abstract class LogClient : IPluginDisposable
    {
        protected const string SubscriptionMessage = "{\"call\":\"subscribe\",\"events\":[\"CombatData\"]}";

        protected  ActConfig Config { get; set; }
        protected ConnectionStatus Status { get; set; }
        private ActEvent? _lastEvent;
        private ActEvent? _currentEvent;
        private List<ActEvent> _pastEvents;

        public static ConnectionStatus GetStatus() => Singletons.Get<LogClient>().Status;
        public static List<ActEvent> PastEvents => Singletons.Get<LogClient>()._pastEvents;

        public LogClient(ActConfig config)
        {
            Config = config;
            Status = ConnectionStatus.NotConnected;
            _pastEvents = [];
        }
        
        public abstract void Start();
        public abstract void Shutdown();
        public abstract void Reset();
        
        public static ActEvent? GetEvent(int index = -1)
        {
            LogClient client = Singletons.Get<LogClient>();
            if (index >= 0 && index < client._pastEvents.Count)
            {
                return client._pastEvents[index];
            }

            return client._currentEvent;
        }

        public static void EndEncounter()
        {
            IChatGui chat = Singletons.Get<IChatGui>();
            XivChatEntry message = new()
            {
                Message = "end",
                Type = XivChatType.Echo
            };

            chat.Print(message);
        }

        public static void RetryConnection()
        {
            LogClient client = Singletons.Get<LogClient>();
            client.Reset();
        }

        protected void ParseLogData(string data)
        {
            this.HandleNewEvent(JsonConvert.DeserializeObject<ActEvent>(data));
        }

        protected void ParseLogData(JObject data)
        {
            this.HandleNewEvent(data.ToObject<ActEvent>());
        }

        private void HandleNewEvent(ActEvent? newEvent)
        {
            if (newEvent is not null)
            {
                newEvent.Timestamp = DateTime.UtcNow;

                if (newEvent?.Encounter is not null &&
                    newEvent?.Combatants is not null &&
                    newEvent.Combatants.Count != 0 &&
                    !newEvent.Equals(_lastEvent))
                {
                    if (!newEvent.IsEncounterActive() &&
                        !newEvent.Equals(_pastEvents.LastOrDefault()))
                    {
                        _pastEvents.Add(newEvent);
                        while (_pastEvents.Count > Config.EncounterHistorySize)
                        {
                            _pastEvents.RemoveAt(0);
                        }
                    }

                    _lastEvent = newEvent;
                    _currentEvent = newEvent;
                }
            }
        }

        public void Clear()
        {
            _currentEvent = null;
            _pastEvents = [];
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

        protected void LogConnectionFailure(string error, Exception? ex = null)
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
            }
        }
    }
}