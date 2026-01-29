using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace LMeter.Act
{
    public partial class TextTagFormatter(
        object source,
        string format,
        bool emptyIfZero,
        Dictionary<string, MemberInfo> members
    )
    {
        [GeneratedRegex(@"\[(\w*)(:k)?\.?(\d+)?\]", RegexOptions.Compiled)]
        private static partial Regex GeneratedRegex();

        public static Regex TextTagRegex { get; } = GeneratedRegex();

        private readonly object m_source = source;
        private readonly string m_format = format;
        private readonly bool m_emptyIfZero = emptyIfZero;
        private readonly Dictionary<string, MemberInfo> m_members = members;

        public string Evaluate(Match m)
        {
            if (m.Groups.Count != 4)
            {
                return m.Value;
            }

            string key = m.Groups[1].Value;
            if (!m_members.TryGetValue(key, out MemberInfo? memberInfo))
            {
                return m.Value;
            }

            object? memberValue = memberInfo?.MemberType switch
            {
                MemberTypes.Field => ((FieldInfo)memberInfo).GetValue(m_source),
                MemberTypes.Property => ((PropertyInfo)memberInfo).GetValue(m_source),
                _ => null,
            };

            string? value = null;
            if (memberValue is LazyFloat lazyFloat)
            {
                string format = string.IsNullOrEmpty(m.Groups[3].Value)
                    ? $"{m_format}0"
                    : $"{m_format}{m.Groups[3].Value}";

                bool kilo = !string.IsNullOrEmpty(m.Groups[2].Value);
                value = lazyFloat.ToString(format, kilo, m_emptyIfZero) ?? m.Value;
            }
            else if (memberValue is not null)
            {
                value = memberValue.ToString();
                if (
                    !string.IsNullOrEmpty(value)
                    && int.TryParse(m.Groups[3].Value, out int trim)
                    && trim < value.Length
                )
                {
                    value = memberValue?.ToString().AsSpan(0, trim).ToString();
                }

                if (m_emptyIfZero && float.TryParse(value, out float val) && val == 0f)
                {
                    return string.Empty;
                }
            }

            return value ?? m.Value;
        }
    }
}
