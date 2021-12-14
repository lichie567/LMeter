using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;
using LMeter.Helpers;
using System.Text.RegularExpressions;

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

        public bool IsEncounterActive() => bool.TryParse(this.IsActive, out bool active) && active;

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
        [JsonIgnore]
        private static readonly Random _rand = new Random();

        [JsonIgnore]
        private static readonly Regex _regex = new Regex(@"\[(\w*)(-k)?\.?(\d+)?\]", RegexOptions.Compiled);
        
        [JsonIgnore]
        private static readonly Dictionary<string, PropertyInfo> _properties = typeof(Encounter).GetProperties().ToDictionary((x) => x.Name.ToLower());

        public static string[] GetTags()
        {
            return typeof(Encounter).GetProperties().Select(x => $"[{x.Name.ToLower()}]").ToArray();
        }

        public string GetFormattedString(string format, string numberFormat)
        {
            return _regex.Replace(format, new TextTagFormatter(this, numberFormat, _properties).Evaluate);
        }

        [JsonProperty("title")]
        public string Title { get; private set; } = string.Empty;

        [JsonProperty("duration")]
        public string Duration { get; private set; } = string.Empty;

        [JsonProperty("DURATION")]
        private string _duration { get; set; } = string.Empty;
        
        [JsonProperty("encdps")]
        [JsonConverter(typeof(LazyFloatConverter))]
        public LazyFloat? Dps { get; private set; }

        [JsonProperty("damage")]
        [JsonConverter(typeof(LazyFloatConverter))]
        public LazyFloat? DamageTotal { get; private set; }
        
        [JsonProperty("enchps")]
        [JsonConverter(typeof(LazyFloatConverter))]
        public LazyFloat? Hps { get; private set; }

        [JsonProperty("healed")]
        [JsonConverter(typeof(LazyFloatConverter))]
        public LazyFloat? HealingTotal { get; private set; }

        [JsonProperty("damagetaken")]
        [JsonConverter(typeof(LazyFloatConverter))]
        public LazyFloat? DamageTaken { get; private set; }

        [JsonProperty("deaths")]
        public string? Deaths { get; private set; }

        [JsonProperty("kills")]
        public string? Kills { get; private set; }

        public static Encounter GetTestData()
        {
            float damage = _rand.Next(212345 * 8);
            float healing = _rand.Next(41234 * 8);

            return new Encounter()
            {
                Duration = "00:30",
                Title = "Preview",
                Dps = new LazyFloat(damage / 30),
                Hps = new LazyFloat(healing / 30),
                Deaths = "0",
                DamageTotal = new LazyFloat(damage),
                HealingTotal = new LazyFloat(healing)
            };
        }
    }

    public class Combatant
    {
        [JsonIgnore]
        private static readonly Random _rand = new Random();

        [JsonIgnore]
        private static readonly Regex _regex = new Regex(@"\[(\w*)(-k)?\.?(\d+)?\]", RegexOptions.Compiled);

        [JsonIgnore]
        private static readonly Dictionary<string, PropertyInfo> _properties = typeof(Combatant).GetProperties().ToDictionary((x) => x.Name.ToLower());

        public static string[] GetTags()
        {
            return typeof(Combatant).GetProperties().Select(x => $"[{x.Name.ToLower()}]").ToArray();
        }

        public string GetFormattedString(string format, string numberFormat)
        {
            return _regex.Replace(format, new TextTagFormatter(this, numberFormat, _properties).Evaluate);
        }

        [JsonProperty("name")]
        public string Name { get; private set; } = string.Empty;

        [JsonProperty("job")]
        public string Job { get; private set; } = string.Empty;

        [JsonProperty("duration")]
        public string Duration { get; private set; } = string.Empty;
        
        [JsonProperty("encdps")]
        [JsonConverter(typeof(LazyFloatConverter))]
        public LazyFloat? EncDps { get; private set; }

        [JsonProperty("dps")]
        [JsonConverter(typeof(LazyFloatConverter))]
        public LazyFloat? Dps { get; private set; }

        [JsonProperty("damage")]
        [JsonConverter(typeof(LazyFloatConverter))]
        public LazyFloat? DamageTotal { get; private set; }

        [JsonProperty("damage%")]
        public string DamagePct { get; private set; } = string.Empty;

        [JsonProperty("crithit%")]
        public string CritHitPct { get; private set; } = string.Empty;

        [JsonProperty("DirectHitPct")]
        public string DirectHitPct { get; private set; } = string.Empty;

        [JsonProperty("CritDirectHitPct")]
        public string CritDirectHitPct { get; private set; } = string.Empty;
        
        [JsonProperty("enchps")]
        [JsonConverter(typeof(LazyFloatConverter))]
        public LazyFloat? EncHps { get; private set; }
        
        [JsonProperty("hps")]
        [JsonConverter(typeof(LazyFloatConverter))]
        public LazyFloat? Hps { get; private set; }

        [JsonProperty("healed")]
        [JsonConverter(typeof(LazyFloatConverter))]
        public LazyFloat? HealingTotal { get; private set; }

        [JsonProperty("healed%")]
        public string HealingPct { get; private set; } = string.Empty;

        [JsonProperty("damagetaken")]
        [JsonConverter(typeof(LazyFloatConverter))]
        public LazyFloat? DamageTaken { get; private set; }

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
            int damage = _rand.Next(212345);
            int healing = _rand.Next(41234);

            return new Combatant()
            {
                Name = "Fake Name",
                Duration = "00:30",
                Job = jobs.Select(x => x.ToString()).ElementAt(_rand.Next(jobs.Length)),
                DamageTotal = new LazyFloat(damage.ToString()),
                Dps = new LazyFloat((damage / 30).ToString()),
                EncDps = new LazyFloat((damage / 30).ToString()),
                HealingTotal = new LazyFloat(healing.ToString()),
                Hps = new LazyFloat((healing / 30).ToString()),
                EncHps = new LazyFloat((healing / 30).ToString()),
                DamagePct = "100%",
                HealingPct = "100%",
                CritHitPct = "20%",
                DirectHitPct = "25%",
                CritDirectHitPct = "5%",
                DamageTaken = new LazyFloat((damage / 20).ToString()),
                Deaths = _rand.Next(2).ToString(),
                MaxHit = "Full Thrust-42069"
            };
        }
    }
}