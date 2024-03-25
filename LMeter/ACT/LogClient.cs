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

        protected ActConfig Config { get; set; }
        
        public ConnectionStatus Status { get; protected set; }
        public List<ActEvent> PastEvents { get; protected init; }

        private ActEvent? _lastEvent;
        private ActEvent? _currentEvent;

        public LogClient(ActConfig config)
        {
            this.Config = config;
            this.Status = ConnectionStatus.NotConnected;
            this.PastEvents = [];
        }
        
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
                    !newEvent.Equals(this.PastEvents.LastOrDefault()))
                {
                    this.PastEvents.Add(newEvent);
                    while (this.PastEvents.Count > Config.EncounterHistorySize)
                    {
                        this.PastEvents.RemoveAt(0);
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
            this.PastEvents.Clear();
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
            }
        }
    }
}