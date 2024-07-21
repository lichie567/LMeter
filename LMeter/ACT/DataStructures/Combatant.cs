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
    public LazyString<string?>? Name_First;

    [TextTag]
    [JsonIgnore]
    public LazyString<string?>? Name_Last;

    [TextTag]
    [JsonIgnore]
    public string Rank = string.Empty;

    [TextTag]
    [JsonProperty("Job")]
    [JsonConverter(typeof(JobConverter))]
    public Job Job { get; set; }

    [JsonIgnore]
    public LazyString<Job>? JobName;

    [TextTag]
    [JsonProperty("duration")]
    public string DurationRaw { get; set; } = string.Empty;

    [TextTag]
    [JsonIgnore]
    public LazyString<string?>? Duration;

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
    [JsonProperty("crithit%")]
    public string CritHitPct { get; set; } = string.Empty;

    [TextTag]
    [JsonProperty("DirectHitPct")]
    public string DirectHitPct { get; set; } = string.Empty;

    [TextTag]
    [JsonProperty("CritDirectHitPct")]
    public string CritDirectHitPct { get; set; } = string.Empty;

    [TextTag]
    [JsonProperty("enchps")]
    [JsonConverter(typeof(LazyFloatConverter))]
    public LazyFloat? Hps { get; set; }

    [TextTag]
    public LazyFloat? EffectiveHealing { get; set; }

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

    [TextTag]
    public LazyString<string?> MaxHitName { get; set; }

    [TextTag]
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
        return TextTagFormatter.TextTagRegex.Replace(format, new TextTagFormatter(this, numberFormat, _textTagMembers).Evaluate);
    }

    public static Combatant GetTestData()
    {
        float damage = IActData<Combatant>.Random.Next(212345);
        float healing = IActData<Combatant>.Random.Next(41234);

        return new Combatant()
        {
            OriginalName = "Firstname Lastname",
            DurationRaw = "00:30",
            Job = (Job)IActData<Combatant>.Random.Next(Enum.GetNames(typeof(Job)).Length - 1) + 1,
            DamageTotal = new LazyFloat(damage.ToString()),
            Dps = new LazyFloat((damage / 30).ToString()),
            HealingTotal = new LazyFloat(healing.ToString()),
            OverHeal = new LazyFloat(5000),
            Hps = new LazyFloat((healing / 30).ToString()),
            DamagePct = IActData<Combatant>.Random.Next(100) + "%",
            HealingPct = "100%",
            CritHitPct = $"{IActData<Combatant>.Random.Next(50)}%",
            DirectHitPct = $"{IActData<Combatant>.Random.Next(50)}%",
            CritDirectHitPct = $"{IActData<Combatant>.Random.Next(10)}%",
            DamageTaken = new LazyFloat((damage / 20).ToString()),
            Deaths = IActData<Combatant>.Random.Next(4).ToString(),
            MaxHit = "Full Thrust-42069"
        };
    }
}