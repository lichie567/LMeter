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
            this.Config = config;
            this.Status = ConnectionStatus.NotConnected;
            _pastEvents = [];
        }
        
        public abstract void Start();
        public abstract void Shutdown();
        public abstract void Reset();
        
        public ActEvent? GetEvent(int index = -1)
        {
            if (index >= 0 && index < _pastEvents.Count)
            {
                return _pastEvents[index];
            }

            return _currentEvent;
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
            if (newEvent?.Encounter is not null &&
                    newEvent?.Combatants is not null &&
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