using System.Collections.Generic;
using Newtonsoft.Json;

namespace LMeter.ACT
{
    public class ACTEvent
    {
        [JsonProperty("type")]
        public string EventType { get; private set; } = string.Empty;
        
        [JsonProperty("isActive")]
        public string IsActive { get; private set; } = string.Empty;
        
        [JsonProperty("Encounter")]
        public Encounter Encounter { get; private set; } = new Encounter();
        
        [JsonProperty("Combatant")]
        public Dictionary<string, Combatant> Combatants { get; private set; } = new Dictionary<string, Combatant>();
    }

    public class Encounter
    {
        [JsonProperty("title")]
        public string Title { get; private set; } = string.Empty;

        [JsonProperty("duration")]
        public string Duration { get; private set; } = string.Empty;
        
        [JsonProperty("encdps")]
        public string Dps { get; private set; } = string.Empty;

        [JsonProperty("damage")]
        public string DamageTotal { get; private set; } = string.Empty;
        
        [JsonProperty("enchps")]
        public string Hps { get; private set; } = string.Empty;

        [JsonProperty("healed")]
        public string HealingTotal { get; private set; } = string.Empty;

        [JsonProperty("damagetaken")]
        public string DamageTaken { get; private set; } = string.Empty;

        [JsonProperty("deaths")]
        public string Deaths { get; private set; } = string.Empty;

        [JsonProperty("kills")]
        public string Kills { get; private set; } = string.Empty;
    }

    public class Combatant
    {
        [JsonProperty("name")]
        public string Name { get; private set; } = string.Empty;

        [JsonProperty("job")]
        public string Job { get; private set; } = string.Empty;
        
        [JsonProperty("isActive")]
        public string IsActive { get; private set; } = string.Empty;

        [JsonProperty("duration")]
        public string Duration { get; private set; } = string.Empty;
        
        [JsonProperty("encdps")]
        public string EncDps { get; private set; } = string.Empty;

        [JsonProperty("dps")]
        public string Dps { get; private set; } = string.Empty;

        [JsonProperty("damage")]
        public string DamageTotal { get; private set; } = string.Empty;

        [JsonProperty("crithit%")]
        public string CritHitPercent { get; private set; } = string.Empty;

        [JsonProperty("DirectHitPct")]
        public string DirectHitPercent { get; private set; } = string.Empty;

        [JsonProperty("CritDirectHitPct")]
        public string CritDirectHitPercent { get; private set; } = string.Empty;
        
        [JsonProperty("enchps")]
        public string EncHps { get; private set; } = string.Empty;
        
        [JsonProperty("hps")]
        public string Hps { get; private set; } = string.Empty;

        [JsonProperty("healed")]
        public string HealingTotal { get; private set; } = string.Empty;

        [JsonProperty("damagetaken")]
        public string DamageTaken { get; private set; } = string.Empty;

        [JsonProperty("deaths")]
        public string Deaths { get; private set; } = string.Empty;

        [JsonProperty("kills")]
        public string Kills { get; private set; } = string.Empty;
    }
}