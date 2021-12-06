using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;
using LMeter.Helpers;

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

        public static ACTEvent GetTestData()
        {
            return new ACTEvent()
            {
                Encounter = LMeter.ACT.Encounter.GetTestData(),
                Combatants = Combatant.GetTestData()
            };
        }
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

        public static Encounter GetTestData()
        {
            return new Encounter()
            {
                Duration = "00:15",
                Title = "Preview",
                Dps = "69420",
                Hps = "42069",
                Deaths = "0",
                DamageTotal = "69420",
                HealingTotal = "42069"
            };
        }
    }

    public class Combatant
    {
        [JsonIgnore]
        private static Random _rand = new Random();

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

        public static Dictionary<string, Combatant> GetTestData()
        {
            Dictionary<string, Combatant> mockCombatants = new Dictionary<string, Combatant>();
            mockCombatants.Add("1", GetCombatant("GNB", "DRK", "WAR", "PLD"));
            mockCombatants.Add("2", GetCombatant("GNB", "DRK", "WAR", "PLD"));

            mockCombatants.Add("3", GetCombatant("WHM", "AST", "SCH", "SGE"));
            mockCombatants.Add("4", GetCombatant("WHM", "AST", "SCH", "SGE"));

            mockCombatants.Add("5", GetCombatant("SAM", "DRG", "MNK", "NIN", "RPR"));
            mockCombatants.Add("6", GetCombatant("SAM", "DRG", "MNK", "NIN", "RPR"));
            mockCombatants.Add("7", GetCombatant("BLM", "SMN", "RDM"));
            mockCombatants.Add("8", GetCombatant("DNC", "MCH", "BRD"));

            return mockCombatants;
        }

        private static Combatant GetCombatant(params string[] jobs)
        {
            int damage = _rand.Next(200000);
            int healing = _rand.Next(50000);

            return new Combatant()
            {
                Name = "Fake Name",
                Duration = "00:15",
                Job = jobs.Select(x => x.ToString()).ElementAt(_rand.Next(jobs.Length)),
                DamageTotal = damage.ToString(),
                Dps = (damage / 15).ToString(),
                EncDps = (damage / 15).ToString(),
                HealingTotal = healing.ToString(),
                Hps = (healing / 15).ToString(),
                EncHps = (healing / 15).ToString(),
                DamagePct = "100%",
                HealingPct = "100%",
                CritHitPct = "20%",
                DirectHitPct = "25%",
                CritDirectHitPct = "5%",
                DamageTaken = (damage / 20).ToString(),
                Deaths = _rand.Next(2).ToString(),
                MaxHit = "Full Thrust-42069"
            };
        }
    }
}