using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace LMeter.Act.DataStructures;

public class Encounter : IActData<Encounter>
{
    [JsonIgnore]
    public static string[] TextTags { get; } = 
        typeof(Encounter).GetMembers().Where(x => Attribute.IsDefined(x, typeof(TextTagAttribute))).Select(x => $"[{x.Name.ToLower()}]").ToArray();

    private static readonly Dictionary<string, MemberInfo> _textTagMembers = 
        typeof(Encounter).GetMembers().Where(x => Attribute.IsDefined(x, typeof(TextTagAttribute))).ToDictionary((x) => x.Name.ToLower());

    public string GetFormattedString(string format, string numberFormat)
    {
        return TextTagFormatter.TextTagRegex.Replace(format, new TextTagFormatter(this, numberFormat, _textTagMembers).Evaluate);
    }

    [TextTag]
    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;

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
    [JsonProperty("enchps")]
    [JsonConverter(typeof(LazyFloatConverter))]
    public LazyFloat? Hps { get; set; }

    [TextTag]
    [JsonProperty("healed")]
    [JsonConverter(typeof(LazyFloatConverter))]
    public LazyFloat? HealingTotal { get; set; }

    [TextTag]
    [JsonProperty("damagetaken")]
    [JsonConverter(typeof(LazyFloatConverter))]
    public LazyFloat? DamageTaken { get; set; }

    [TextTag]
    [JsonProperty("deaths")]
    public string? Deaths { get; set; }

    [TextTag]
    [JsonProperty("kills")]
    public string? Kills { get; set; }

    public Encounter()
    {
        this.Duration = new LazyString<string?>(() => this.DurationRaw, LazyStringConverters.Duration);
    }

    public static Encounter GetTestData()
    {
        float damage = IActData<Encounter>.Random.Next(212345 * 12);
        float healing = IActData<Encounter>.Random.Next(41234 * 12);

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