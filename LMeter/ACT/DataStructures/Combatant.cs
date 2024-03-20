using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LMeter.Helpers;
using Newtonsoft.Json;

namespace LMeter.Act.DataStructures;

public class Combatant
{
    [JsonIgnore]
    public static string[] TextTags { get; } = typeof(Combatant).GetFields().Select(x => $"[{x.Name.ToLower()}]").ToArray();

    // TODO: move this to a global place so it can be shared between encounter and combatant
    private static readonly Random _rand = new();
    private static readonly Dictionary<string, MemberInfo> _members = typeof(Combatant).GetMembers().ToDictionary((x) => x.Name.ToLower());

    [JsonProperty("name")]
    public string OriginalName { get; set; } = string.Empty;
    
    public string? NameOverwrite { get; set; } = null;

    [JsonIgnore]
    public string Name => NameOverwrite ?? OriginalName;

    [JsonIgnore]
    public LazyString<string?>? Name_First;

    [JsonIgnore]
    public LazyString<string?>? Name_Last;

    [JsonIgnore]
    public string Rank = string.Empty;

    [JsonProperty("Job")]
    [JsonConverter(typeof(JobConverter))]
    public Job Job { get; set; }

    [JsonIgnore]
    public LazyString<Job>? JobName;

    [JsonProperty("duration")]
    public string DurationRaw { get; set; } = string.Empty;

    [JsonIgnore]
    public LazyString<string?>? Duration;
    
        
    [JsonProperty("encdps")]
    [JsonConverter(typeof(LazyFloatConverter))]
    public LazyFloat? EncDps { get; set; }

    [JsonProperty("dps")]
    [JsonConverter(typeof(LazyFloatConverter))]
    public LazyFloat? Dps { get; set; }

    [JsonProperty("damage")]
    [JsonConverter(typeof(LazyFloatConverter))]
    public LazyFloat? DamageTotal { get; set; }

    [JsonProperty("damage%")]
    public string DamagePct { get; set; } = string.Empty;

    [JsonProperty("crithit%")]
    public string CritHitPct { get; set; } = string.Empty;

    [JsonProperty("DirectHitPct")]
    public string DirectHitPct { get; set; } = string.Empty;

    [JsonProperty("CritDirectHitPct")]
    public string CritDirectHitPct { get; set; } = string.Empty;
        
    [JsonProperty("enchps")]
    [JsonConverter(typeof(LazyFloatConverter))]
    public LazyFloat? EncHps { get; set; }
        
    [JsonProperty("hps")]
    [JsonConverter(typeof(LazyFloatConverter))]
    public LazyFloat? Hps { get; set; }

    public LazyFloat? EffectiveHealing { get; set; }

    [JsonProperty("healed")]
    [JsonConverter(typeof(LazyFloatConverter))]
    public LazyFloat? HealingTotal { get; set; }

    [JsonProperty("healed%")]
    public string HealingPct  { get; set; }= string.Empty;

    [JsonProperty("overHeal")]
    [JsonConverter(typeof(LazyFloatConverter))]
    public LazyFloat? OverHeal { get; set; }

    [JsonProperty("OverHealPct")]
    public string OverHealPct  { get; set; }= string.Empty;

    [JsonProperty("damagetaken")]
    [JsonConverter(typeof(LazyFloatConverter))]
    public LazyFloat? DamageTaken { get; set; }

    [JsonProperty("deaths")]
    public string Deaths  { get; set; }= string.Empty;

    [JsonProperty("kills")]
    public string Kills  { get; set; }= string.Empty;

    [JsonProperty("maxhit")]
    public string MaxHit  { get; set; } = string.Empty;

    [JsonProperty("MAXHIT")]
    private string _maxHit  { get; set; } = string.Empty;

    public LazyString<string?> MaxHitName { get; set; }

    public LazyFloat? MaxHitValue { get; set; }

    public Combatant()
    {
        this.Duration = new LazyString<string?>(() => this.DurationRaw, LazyStringConverters.Duration);
        this.Name_First = new LazyString<string?>(() => this.Name, LazyStringConverters.FirstName);
        this.Name_Last = new LazyString<string?>(() => this.Name, LazyStringConverters.LastName);
        this.JobName = new LazyString<Job>(() => this.Job, LazyStringConverters.JobName);
        this.EffectiveHealing = new LazyFloat(() => (this.HealingTotal?.Value ?? 0) - (this.OverHeal?.Value ?? 0));
        this.MaxHitName = new LazyString<string?>(() => this.MaxHit, LazyStringConverters.MaxHitName);
        this.MaxHitValue = new LazyFloat(() => LazyStringConverters.MaxHitValue(this.MaxHit));
    }
    
    public string GetFormattedString(string format, string numberFormat)
    {
        return TextTagFormatter.TextTagRegex.Replace(format, new TextTagFormatter(this, numberFormat, _members).Evaluate);
    }

    public static Dictionary<string, Combatant> GetTestData()
    {
        Dictionary<string, Combatant> mockCombatants = new()
        {
            { "1", GetCombatant("GNB", "DRK", "WAR", "PLD") },
            { "2", GetCombatant("GNB", "DRK", "WAR", "PLD") },
            { "3", GetCombatant("WHM", "AST", "SCH", "SGE") },
            { "4", GetCombatant("WHM", "AST", "SCH", "SGE") },
            { "5", GetCombatant("SAM", "DRG", "MNK", "NIN", "RPR") },
            { "6", GetCombatant("SAM", "DRG", "MNK", "NIN", "RPR") },
            { "7", GetCombatant("BLM", "SMN", "RDM") },
            { "8", GetCombatant("DNC", "MCH", "BRD") },
            { "9", GetCombatant("SAM", "DRG", "MNK", "NIN", "RPR") },
            { "10", GetCombatant("SAM", "DRG", "MNK", "NIN", "RPR") },
            { "11", GetCombatant("BLM", "SMN", "RDM") },
            { "12", GetCombatant("DNC", "MCH", "BRD") }
        };
        
        return mockCombatants;
    }

    private static Combatant GetCombatant(params string[] jobs)
    {
        int damage = _rand.Next(212345);
        int healing = _rand.Next(41234);

        return new Combatant()
        {
            OriginalName = "Firstname Lastname",
            DurationRaw = "00:30",
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