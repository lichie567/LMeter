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

        private readonly object _source = source;
        private readonly string _format = format;
        private readonly bool _emptyIfZero = emptyIfZero;
        private readonly Dictionary<string, MemberInfo> _members = members;

        public string Evaluate(Match m)
        {
            if (m.Groups.Count != 4)
            {
                return m.Value;
            }

            string key = m.Groups[1].Value;
            if (!_members.TryGetValue(key, out MemberInfo? memberInfo))
            {
                return m.Value;
            }

            object? memberValue = memberInfo?.MemberType switch
            {
                MemberTypes.Field => ((FieldInfo)memberInfo).GetValue(_source),
                MemberTypes.Property => ((PropertyInfo)memberInfo).GetValue(_source),
                _ => null,
            };

            string? value = null;
            if (memberValue is LazyFloat lazyFloat)
            {
                string format = string.IsNullOrEmpty(m.Groups[3].Value)
                    ? $"{_format}0"
                    : $"{_format}{m.Groups[3].Value}";

                bool kilo = !string.IsNullOrEmpty(m.Groups[2].Value);
                value = lazyFloat.ToString(format, kilo, _emptyIfZero) ?? m.Value;
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

                if (_emptyIfZero && float.TryParse(value, out float val) && val == 0f)
                {
                    return string.Empty;
                }
            }

            return value ?? m.Value;
        }
    }
}
