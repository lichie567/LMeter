using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;

namespace LMeter.ACT
{
    public class ACTEvent
    {
        [JsonIgnore]
        public DateTime Timestamp;

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
        public static string[] GetTags()
        {
            return typeof(Encounter).GetProperties().Select(x => $"[{x.Name.ToLower()}]").ToArray();
        }

        public string GetFormattedString(string format)
        {
            foreach (PropertyInfo prop in this.GetType().GetProperties())
            {
                string? value = prop.GetValue(this)?.ToString();
                if (value is not null)
                {
                    format = format.Replace($"[{prop.Name.ToLower()}]", value);
                }
            }

            return format;
        }

        [JsonProperty("title")]
        public string Title { get; private set; } = string.Empty;

        [JsonProperty("duration")]
        public string Duration { get; private set; } = string.Empty;

        [JsonProperty("DURATION")]
        private string _duration { get; set; } = string.Empty;
        
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
        public static string[] GetTags()
        {
            return typeof(Combatant).GetProperties().Select(x => $"[{x.Name.ToLower()}]").ToArray();
        }

        public string GetFormattedString(string format)
        {
            foreach (PropertyInfo prop in this.GetType().GetProperties())
            {
                string? value = prop.GetValue(this)?.ToString();
                if (value is not null)
                {
                    format = format.Replace($"[{prop.Name.ToLower()}]", value);
                }
            }

            return format;
        }

        [JsonProperty("name")]
        public string Name { get; private set; } = string.Empty;

        [JsonProperty("job")]
        public string Job { get; private set; } = string.Empty;

        [JsonProperty("duration")]
        public string Duration { get; private set; } = string.Empty;
        
        [JsonProperty("encdps")]
        public string EncDps { get; private set; } = string.Empty;

        [JsonProperty("dps")]
        public string Dps { get; private set; } = string.Empty;

        [JsonProperty("damage")]
        public string DamageTotal { get; private set; } = string.Empty;

        [JsonProperty("damage%")]
        public string DamagePct { get; private set; } = string.Empty;

        [JsonProperty("crithit%")]
        public string CritHitPct { get; private set; } = string.Empty;

        [JsonProperty("DirectHitPct")]
        public string DirectHitPct { get; private set; } = string.Empty;

        [JsonProperty("CritDirectHitPct")]
        public string CritDirectHitPct { get; private set; } = string.Empty;
        
        [JsonProperty("enchps")]
        public string EncHps { get; private set; } = string.Empty;
        
        [JsonProperty("hps")]
        public string Hps { get; private set; } = string.Empty;

        [JsonProperty("healed")]
        public string HealingTotal { get; private set; } = string.Empty;

        [JsonProperty("healed%")]
        public string HealingPct { get; private set; } = string.Empty;

        [JsonProperty("damagetaken")]
        public string DamageTaken { get; private set; } = string.Empty;

        [JsonProperty("deaths")]
        public string Deaths { get; private set; } = string.Empty;

        [JsonProperty("kills")]
        public string Kills { get; private set; } = string.Empty;

        [JsonProperty("maxhit")]
        public string MaxHit { get; private set; } = string.Empty;
        [JsonProperty("MAXHIT")]
        private string _maxHit { get; set; } = string.Empty;
    }
}