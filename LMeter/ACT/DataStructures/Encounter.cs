using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace LMeter.Act.DataStructures;

public class Encounter
{
    [JsonIgnore]
    public static string[] TextTags { get; } = 
        typeof(Encounter).GetMembers().Where(x => x is PropertyInfo || x is FieldInfo).Select(x => $"[{x.Name.ToLower()}]").ToArray();

    private static readonly Dictionary<string, MemberInfo> _members = 
        typeof(Encounter).GetMembers().Where(x => x is PropertyInfo || x is FieldInfo).ToDictionary((x) => x.Name.ToLower());

    private static readonly Random _rand = new();

    public string GetFormattedString(string format, string numberFormat)
    {
        return TextTagFormatter.TextTagRegex.Replace(format, new TextTagFormatter(this, numberFormat, _members).Evaluate);
    }

    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;

    [JsonProperty("duration")]
    public string DurationRaw { get; set; } = string.Empty;

    [JsonIgnore]
    public LazyString<string?>? Duration;

    [JsonProperty("encdps")]
    [JsonConverter(typeof(LazyFloatConverter))]
    public LazyFloat? Dps { get; set; }

    [JsonProperty("damage")]
    [JsonConverter(typeof(LazyFloatConverter))]
    public LazyFloat? DamageTotal { get; set; }

    [JsonProperty("enchps")]
    [JsonConverter(typeof(LazyFloatConverter))]
    public LazyFloat? Hps { get; set; }

    [JsonProperty("healed")]
    [JsonConverter(typeof(LazyFloatConverter))]
    public LazyFloat? HealingTotal { get; set; }

    [JsonProperty("damagetaken")]
    [JsonConverter(typeof(LazyFloatConverter))]
    public LazyFloat? DamageTaken { get; set; }

    [JsonProperty("deaths")]
    public string? Deaths { get; set; }

    [JsonProperty("kills")]
    public string? Kills { get; set; }

    public Encounter()
    {
        this.Duration = new LazyString<string?>(() => this.DurationRaw, LazyStringConverters.Duration);
    }

    public static Encounter GetTestData()
    {
        float damage = _rand.Next(212345 * 8);
        float healing = _rand.Next(41234 * 8);

        return new Encounter()
        {
            DurationRaw = "00:30",
            Title = "Preview",
            Dps = new LazyFloat(damage / 30),
            Hps = new LazyFloat(healing / 30),
            Deaths = "0",
            DamageTotal = new LazyFloat(damage),
            HealingTotal = new LazyFloat(healing)
        };
    }
}