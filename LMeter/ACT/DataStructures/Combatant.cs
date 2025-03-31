using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LMeter.Helpers;
using Newtonsoft.Json;

namespace LMeter.Act.DataStructures;

public class Combatant : IActData<Combatant>
{
    [JsonIgnore]
    public static string[] TextTags { get; } = 
        typeof(Combatant).GetMembers().Where(x => Attribute.IsDefined(x, typeof(TextTagAttribute))).Select(x => $"[{x.Name.ToLower()}]").ToArray();

    private static readonly Dictionary<string, MemberInfo> _textTagMembers = 
        typeof(Combatant).GetMembers().Where(x => Attribute.IsDefined(x, typeof(TextTagAttribute))).ToDictionary((x) => x.Name.ToLower());

    [JsonProperty("name")]
    public string OriginalName { get; set; } = string.Empty;

    public string? NameOverwrite { get; set; } = null;

    [TextTag]
    [JsonIgnore]
    public string Name => NameOverwrite ?? OriginalName;

    [TextTag]
    [JsonIgnore]
    public LazyString<string?> Name_First;

    [TextTag]
    [JsonIgnore]
    public LazyString<string?> Name_Last;

    [TextTag]
    [JsonIgnore]
    public int Rank = 1;

    [TextTag]
    [JsonProperty("Job")]
    [JsonConverter(typeof(JobConverter))]
    public Job Job { get; set; }

    [JsonIgnore]
    public LazyString<Job> JobName;

    [TextTag]
    [JsonProperty("duration")]
    public string DurationRaw { get; set; } = string.Empty;

    [TextTag]
    [JsonIgnore]
    public LazyString<string?> Duration;

    [TextTag]
    [JsonProperty("encdps")]
    [JsonConverter(typeof(LazyFloatConverter))]
    public LazyFloat? Dps { get; set; }

    [TextTag]
    [JsonProperty("damage")]
    [JsonConverter(typeof(LazyFloatConverter))]
    public LazyFloat? DamageTotal { get; set; }

    [TextTag]
    [JsonProperty("damage%")]
    public string DamagePct { get; set; } = string.Empty;

    [TextTag]
    [JsonIgnore]
    public LazyFloat CritHitPct { get; set; }
    [TextTag]
    [JsonIgnore]
    public LazyFloat DirectHitPct  { get; set; }
    [TextTag]
    [JsonIgnore]
    public LazyFloat CritDirectHitPct   { get; set; }

    // [TextTag]
    [JsonProperty("crithit%")]
    private string _critHitPctString { get; set; } = string.Empty;

    // [TextTag]
    [JsonProperty("DirectHitPct")]
    private string _directHitPctString { get; set; } = string.Empty;

    // [TextTag]
    [JsonProperty("CritDirectHitPct")]
    private string _critDirectHitPctString { get; set; } = string.Empty;

    [TextTag]
    [JsonProperty("enchps")]
    [JsonConverter(typeof(LazyFloatConverter))]
    public LazyFloat? Hps { get; set; }

    [TextTag]
    public LazyFloat EffectiveHealing { get; set; }

    [TextTag]
    [JsonProperty("healed")]
    [JsonConverter(typeof(LazyFloatConverter))]
    public LazyFloat? HealingTotal { get; set; }

    [TextTag]
    [JsonProperty("healed%")]
    public string HealingPct { get; set; }= string.Empty;

    [TextTag]
    [JsonProperty("overHeal")]
    [JsonConverter(typeof(LazyFloatConverter))]
    public LazyFloat? OverHeal { get; set; }

    [TextTag]
    [JsonProperty("OverHealPct")]
    public string OverHealPct { get; set; }= string.Empty;

    [TextTag]
    [JsonProperty("damagetaken")]
    [JsonConverter(typeof(LazyFloatConverter))]
    public LazyFloat? DamageTaken { get; set; }

    [TextTag]
    [JsonProperty("deaths")]
    public string Deaths { get; set; }= string.Empty;

    [TextTag]
    [JsonProperty("kills")]
    public string Kills { get; set; }= string.Empty;

    [TextTag]
    [JsonProperty("maxhit")]
    public string MaxHit { get; set; } = string.Empty;

    [JsonProperty("MAXHIT")]
    private string _maxHit { get; set; } = string.Empty;

    [TextTag]
    public LazyString<string?> MaxHitName { get; set; }

    [TextTag]
    public LazyFloat MaxHitValue { get; set; }

    [JsonProperty("hits")]
    [JsonConverter(typeof(LazyFloatConverter))]
    private LazyFloat? _hits { get; set; }

    [JsonProperty("crithits")]
    [JsonConverter(typeof(LazyFloatConverter))]
    private LazyFloat? _critHits { get; set; }

    [JsonProperty("DirectHitCount")]
    [JsonConverter(typeof(LazyFloatConverter))]
    private LazyFloat? _directHits { get; set; }

    [JsonProperty("CritDirectHitCount")]
    [JsonConverter(typeof(LazyFloatConverter))]
    private LazyFloat? _critdirectHits { get; set; }

    public Combatant()
    {
        this.Duration = new LazyString<string?>(() => this.DurationRaw, LazyStringConverters.Duration);
        this.Name_First = new LazyString<string?>(() => this.Name, LazyStringConverters.FirstName);
        this.Name_Last = new LazyString<string?>(() => this.Name, LazyStringConverters.LastName);
        this.JobName = new LazyString<Job>(() => this.Job, LazyStringConverters.JobName);
        this.EffectiveHealing = new LazyFloat(() => (this.HealingTotal?.Value ?? 0) - (this.OverHeal?.Value ?? 0));
        this.MaxHitName = new LazyString<string?>(() => this.MaxHit, LazyStringConverters.MaxHitName);
        this.MaxHitValue = new LazyFloat(() => LazyStringConverters.MaxHitValue(this.MaxHit));
        this.CritHitPct = new LazyFloat(() => GetPercent(_critHits, _hits));
        this.DirectHitPct = new LazyFloat(() => GetPercent(_directHits, _hits));
        this.CritDirectHitPct = new LazyFloat(() => GetPercent(_critdirectHits, _hits));
    }

    public string GetFormattedString(string format, string numberFormat, bool emptyIfZero)
    {
        return TextTagFormatter.TextTagRegex.Replace(format, new TextTagFormatter(this, numberFormat, emptyIfZero, _textTagMembers).Evaluate);
    }

    public float GetValueForDataType(MeterDataType type) => type switch
    {
        MeterDataType.Damage => this.DamageTotal?.Value ?? 0,
        MeterDataType.Healing => this.EffectiveHealing?.Value ?? 0,
        MeterDataType.DamageTaken => this.DamageTaken?.Value ?? 0,
        _ => 0
    };

    private static float GetPercent(LazyFloat? val1, LazyFloat? val2)
    {

        if (val1 is not null && val2 is not null)
        {
            if (val2.Value == 0)
            {
                return 0;
            }

            return val1.Value / val2.Value * 100;
        }

        return 0;
    }

    public static Combatant GetTestData()
    {
        float damage = IActData<Combatant>.Random.Next(212345);
        float healing = IActData<Combatant>.Random.Next(41234);

        return new Combatant()
        {
            OriginalName = "Firstname Lastname",
            DurationRaw = "00:30",
            Job = (Job)IActData<Combatant>.Random.Next(Enum.GetNames<Job>().Length - 1) + 1,
            DamageTotal = new LazyFloat(damage.ToString()),
            Dps = new LazyFloat((damage / 30).ToString()),
            HealingTotal = new LazyFloat(healing.ToString()),
            OverHeal = new LazyFloat(5000),
            Hps = new LazyFloat((healing / 30).ToString()),
            DamagePct = IActData<Combatant>.Random.Next(100) + "%",
            HealingPct = "100%",
            CritHitPct = new LazyFloat($"{IActData<Combatant>.Random.Next(50)}%"),
            DirectHitPct = new LazyFloat($"{IActData<Combatant>.Random.Next(50)}%"),
            CritDirectHitPct = new LazyFloat($"{IActData<Combatant>.Random.Next(10)}%"),
            DamageTaken = new LazyFloat((damage / 20).ToString()),
            Deaths = IActData<Combatant>.Random.Next(4).ToString(),
            MaxHit = "Full Thrust-42069"
        };
    }
}