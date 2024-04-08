using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace LMeter.Act.DataStructures
{
    public class ActEvent
    {
        private bool _parsedActive;
        private bool _active;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Data { get; set; } = string.Empty;

        [JsonProperty("type")]
        public string EventType { get; set; } = string.Empty;

        [JsonProperty("isActive")]
        public string IsActive { get; set; } = string.Empty;

        [JsonProperty("Encounter")]
        public Encounter? Encounter { get; set; }

        [JsonProperty("Combatant")]
        public Dictionary<string, Combatant>? Combatants { get; set; }

        public bool IsEncounterActive()
        {
            if (_parsedActive)
            {
                return _active;
            }

            bool.TryParse(this.IsActive, out _active);
            _parsedActive = true;
            return _active;
        }

        public bool Equals(ActEvent? actEvent)
        {
            if (actEvent is null)
            {
                return false;
            }

            if (this.Encounter is null ^ actEvent.Encounter is null ||
                this.Combatants is null ^ actEvent.Combatants is null)
            {
                return false;
            }

            if (this.Encounter is not null && actEvent.Encounter is not null &&
                this.Combatants is not null && actEvent.Combatants is not null)
            {
                return this.Encounter.DurationRaw.Equals(actEvent.Encounter.DurationRaw) &&
                       this.Encounter.Title.Equals(actEvent.Encounter.Title) &&
                       (this.Encounter.DamageTotal?.Value ?? 0f) == (actEvent.Encounter.DamageTotal?.Value ?? 0f) &&
                       this.IsEncounterActive() == actEvent.IsEncounterActive() &&
                       this.Combatants.Count == actEvent.Combatants.Count;
            }

            return true;
        }

        public static ActEvent GetTestData()
        {
            return new ActEvent()
            {
                Encounter = Encounter.GetTestData(),
                Combatants = Combatant.GetTestData()
            };
        }
    }
}