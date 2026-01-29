using System;
using System.Globalization;

namespace LMeter.Act
{
    public class LazyFloat
    {
        private readonly Func<string>? m_getStringInput;
        private readonly Func<float>? m_getFloatInput;
        private float m_value = 0;
        private string? m_input;

        public bool Generated { get; private set; }

        public float Value
        {
            get
            {
                if (this.Generated)
                {
                    return m_value;
                }

                if (m_input is null)
                {
                    if (m_getFloatInput is not null)
                    {
                        m_value = m_getFloatInput.Invoke();
                        this.Generated = true;
                        return m_value;
                    }
                    else if (m_getStringInput is not null)
                    {
                        m_input = m_getStringInput.Invoke();
                    }
                }

                if (
                    float.TryParse(m_input, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed)
                    && !float.IsNaN(parsed)
                )
                {
                    m_value = parsed;
                }
                else
                {
                    m_value = 0;
                }

                this.Generated = true;
                return m_value;
            }
        }

        public LazyFloat(string? input)
        {
            m_input = input;
        }

        public LazyFloat(float value)
        {
            m_value = value;
            this.Generated = true;
        }

        public LazyFloat(Func<float> input)
        {
            m_getFloatInput = input;
        }

        public LazyFloat(Func<string> input)
        {
            m_getStringInput = input;
        }

        public override string? ToString()
        {
            return this.Value.ToString();
        }

        public string? ToString(string format, bool kilo, bool emptyIfZero)
        {
            if (emptyIfZero && this.Value == 0f)
            {
                return string.Empty;
            }

            return kilo ? KiloFormat(this.Value, format) : this.Value.ToString(format, CultureInfo.InvariantCulture);
        }

        private static string KiloFormat(float num, string format) =>
            num switch
            {
                >= 1000000 => (num / 1000000f).ToString(format, CultureInfo.InvariantCulture) + "M",
                >= 1000 => (num / 1000f).ToString(format, CultureInfo.InvariantCulture) + "K",
                _ => num.ToString(format, CultureInfo.InvariantCulture),
            };
    }
}
