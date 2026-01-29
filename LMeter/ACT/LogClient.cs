using System;
using System.Collections.Generic;
using System.Globalization;
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
    public abstract class LogClient(ActConfig config) : IPluginDisposable
    {
        protected const string SUBSCRIPTION_MESSAGE = "{\"call\":\"subscribe\",\"events\":[\"CombatData\"]}";

        protected ActConfig Config { get; set; } = config;

        public ConnectionStatus Status { get; protected set; } = ConnectionStatus.NotConnected;
        public List<ActEvent> PastEvents { get; protected init; } = [];

        private ActEvent? m_lastEvent;
        private ActEvent? m_currentEvent;
        private readonly JsonSerializer m_jsonSerializer = new() { Culture = CultureInfo.CurrentCulture };

        public abstract void Start();
        public abstract void Shutdown();
        public abstract void Reset();

        public ActEvent? GetEvent(int index = -1)
        {
            if (index >= 0 && index < this.PastEvents.Count)
            {
                return this.PastEvents[index];
            }

            return m_currentEvent;
        }

        protected void ParseLogData(string data)
        {
            this.ParseLogData(JObject.Parse(data));
        }

        protected void ParseLogData(JObject data)
        {
            this.HandleNewEvent(data.ToObject<ActEvent>(m_jsonSerializer));
        }

        private void HandleNewEvent(ActEvent? newEvent)
        {
            if (
                newEvent?.Encounter is not null
                && newEvent?.Combatants is not null
                && newEvent.Combatants.Count != 0
                && !newEvent.Equals(m_lastEvent)
            )
            {
                if (!newEvent.IsEncounterActive() && !newEvent.Equals(this.PastEvents.LastOrDefault()))
                {
                    this.PastEvents.Add(newEvent);
                    while (this.PastEvents.Count > Config.EncounterHistorySize)
                    {
                        this.PastEvents.RemoveAt(0);
                    }
                }

                m_lastEvent = newEvent;
                m_currentEvent = newEvent;
            }
        }

        public virtual void EndEncounter()
        {
            IChatGui chat = Singletons.Get<IChatGui>();
            XivChatEntry message = new() { Message = "end", Type = XivChatType.Echo };

            chat.Print(message);
        }

        public virtual void Clear()
        {
            m_currentEvent = null;
            this.PastEvents.Clear();
            if (Config.ClearAct)
            {
                IChatGui chat = Singletons.Get<IChatGui>();
                XivChatEntry message = new() { Message = "clear", Type = XivChatType.Echo };

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
