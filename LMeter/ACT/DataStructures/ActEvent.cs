using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace LMeter.Act.DataStructures
{
    public class ActEvent
    {
        private bool _parsedActive;
        private bool _active;
        public DateTime Timestamp;

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