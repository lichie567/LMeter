using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LMeter.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace LMeter.ACT
{
    public class ACTEvent
    {
        [JsonIgnore]
        private bool _parsedActive = false;

        [JsonIgnore]
        private bool _active = false;

        [JsonIgnore]
        public DateTime Timestamp;

        [JsonProperty("type")]
        public string EventType = string.Empty;
        
        [JsonProperty("isActive")]
        public string IsActive = string.Empty;
        
        [JsonProperty("Encounter")]
        public Encounter? Encounter;
        
        [JsonProperty("Combatant")]
        public Dictionary<string, Combatant>? Combatants;

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
        public static string[] TextTags { get; } = typeof(Encounter).GetFields().Select(x => $"[{x.Name.ToLower()}]").ToArray();

        [JsonIgnore]
        private static readonly Random _rand = new Random();
        
        [JsonIgnore]
        private static readonly Dictionary<string, FieldInfo> _fields = typeof(Encounter).GetFields().ToDictionary((x) => x.Name.ToLower());

        public string GetFormattedString(string format, string numberFormat)
        {
            return TextTagFormatter.TextTagRegex.Replace(format, new TextTagFormatter(this, numberFormat, _fields).Evaluate);
        }

        [JsonProperty("title")]
        public string Title = string.Empty;

        [JsonProperty("duration")]
        public string Duration = string.Empty;

        [JsonProperty("DURATION")]
        private string _duration = string.Empty;
        
        [JsonProperty("encdps")]
        [JsonConverter(typeof(LazyFloatConverter))]
        public LazyFloat? Dps;

        [JsonProperty("damage")]
        [JsonConverter(typeof(LazyFloatConverter))]
        public LazyFloat? DamageTotal;
        
        [JsonProperty("enchps")]
        [JsonConverter(typeof(LazyFloatConverter))]
        public LazyFloat? Hps;

        [JsonProperty("healed")]
        [JsonConverter(typeof(LazyFloatConverter))]
        public LazyFloat? HealingTotal;

        [JsonProperty("damagetaken")]
        [JsonConverter(typeof(LazyFloatConverter))]
        public LazyFloat? DamageTaken;

        [JsonProperty("deaths")]
        public string? Deaths;

        [JsonProperty("kills")]
        public string? Kills;

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
        public static string[] TextTags { get; } = typeof(Combatant).GetFields().Select(x => $"[{x.Name.ToLower()}]").ToArray();

        [JsonIgnore]
        private static readonly Random _rand = new Random();

        [JsonIgnore]
        private static readonly Dictionary<string, FieldInfo> _fields = typeof(Combatant).GetFields().ToDictionary((x) => x.Name.ToLower());

        public string GetFormattedString(string format, string numberFormat)
        {
            return TextTagFormatter.TextTagRegex.Replace(format, new TextTagFormatter(this, numberFormat, _fields).Evaluate);
        }

        [JsonProperty("name")]
        public string Name = string.Empty;

        [JsonIgnore]
        public LazyString<string?>? Name_First;

        [JsonIgnore]
        public LazyString<string?>? Name_Last;

        [JsonIgnore]
        public string Rank = string.Empty;

        [JsonProperty("job")]
        [JsonConverter(typeof(EnumConverter))]
        public Job Job;

        [JsonIgnore]
        public LazyString<Job>? JobName;

        [JsonProperty("duration")]
        public string Duration = string.Empty;
        
        [JsonProperty("encdps")]
        [JsonConverter(typeof(LazyFloatConverter))]
        public LazyFloat? EncDps;

        [JsonProperty("dps")]
        [JsonConverter(typeof(LazyFloatConverter))]
        public LazyFloat? Dps;

        [JsonProperty("damage")]
        [JsonConverter(typeof(LazyFloatConverter))]
        public LazyFloat? DamageTotal;

        [JsonProperty("damage%")]
        public string DamagePct = string.Empty;

        [JsonProperty("crithit%")]
        public string CritHitPct = string.Empty;

        [JsonProperty("DirectHitPct")]
        public string DirectHitPct = string.Empty;

        [JsonProperty("CritDirectHitPct")]
        public string CritDirectHitPct = string.Empty;
        
        [JsonProperty("enchps")]
        [JsonConverter(typeof(LazyFloatConverter))]
        public LazyFloat? EncHps;
        
        [JsonProperty("hps")]
        [JsonConverter(typeof(LazyFloatConverter))]
        public LazyFloat? Hps;

        public LazyFloat? EffectiveHealing;

        [JsonProperty("healed")]
        [JsonConverter(typeof(LazyFloatConverter))]
        public LazyFloat? HealingTotal;

        [JsonProperty("healed%")]
        public string HealingPct = string.Empty;

        [JsonProperty("overHeal")]
        [JsonConverter(typeof(LazyFloatConverter))]
        public LazyFloat? OverHeal;

        [JsonProperty("OverHealPct")]
        public string OverHealPct = string.Empty;

        [JsonProperty("damagetaken")]
        [JsonConverter(typeof(LazyFloatConverter))]
        public LazyFloat? DamageTaken;

        [JsonProperty("deaths")]
        public string Deaths = string.Empty;

        [JsonProperty("kills")]
        public string Kills = string.Empty;

        [JsonProperty("maxhit")]
        public string MaxHit = string.Empty;

        [JsonProperty("MAXHIT")]
        private string _maxHit = string.Empty;

        public LazyString<string?> MaxHitName;

        public LazyFloat? MaxHitValue;

        public Combatant()
        {
            this.Name_First = new LazyString<string?>(() => this.Name, LazyStringConverters.FirstName);
            this.Name_Last = new LazyString<string?>(() => this.Name, LazyStringConverters.LastName);
            this.JobName = new LazyString<Job>(() => this.Job, LazyStringConverters.JobName);
            this.EffectiveHealing = new LazyFloat(() => (this.HealingTotal?.Value ?? 0) - (this.OverHeal?.Value ?? 0));
            this.MaxHitName = new LazyString<string?>(() => this.MaxHit, LazyStringConverters.MaxHitName);
            this.MaxHitValue = new LazyFloat(() => LazyStringConverters.MaxHitValue(this.MaxHit));
        }

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
                Name = "Firstname Lastname",
                Duration = "00:30",
                Job = Enum.Parse<Job>(jobs[_rand.Next(jobs.Length)]),
                DamageTotal = new LazyFloat(damage.ToString()),
                Dps = new LazyFloat((damage / 30).ToString()),
                EncDps = new LazyFloat((damage / 30).ToString()),
                HealingTotal = new LazyFloat(healing.ToString()),
                OverHeal = new LazyFloat(5000),
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