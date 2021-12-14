using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LMeter.ACT
{
    public class TextTagFormatter
    {
        private string Format { get; set; }
        private Dictionary<string, PropertyInfo> Properties { get; set; }
        private object Source { get; set; }

        public TextTagFormatter(
            object source,
            string format,
            Dictionary<string, PropertyInfo> properties)
        {
            this.Source = source;
            this.Format = format;
            this.Properties = properties;
        }

        public string Evaluate(Match m)
        {
            if (m.Groups.Count != 4)
            {
                return m.Value;
            }

            string format = string.IsNullOrEmpty(m.Groups[3].Value)
                ? $"{this.Format}0"
                : $"{this.Format}{m.Groups[3].Value}";
            
            string? value = null;
            string key = m.Groups[1].Value;
            
            if (this.Properties.ContainsKey(key))
            {
                object? propValue = this.Properties[m.Groups[1].Value].GetValue(this.Source);

                if (propValue is LazyFloat lazyFloat)
                {
                    bool kilo = !string.IsNullOrEmpty(m.Groups[2].Value);
                    value = lazyFloat.ToString(format, kilo);
                }
                else
                {
                    value = propValue?.ToString();
                }
            }

            return value ?? m.Value;
        }
    }
}