
using System.Globalization;

namespace LMeter.ACT
{
    public class LazyFloat
    {
        private float _value = 0;

        public string? Input { get; private set; }

        public bool WasGenerated { get; private set; }
        
        public float Value
        {
            get
            {
                if(this.WasGenerated)
                {
                    return this._value;
                }

                if (float.TryParse(this.Input, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) &&
                    !float.IsNaN(parsed))
                {
                    this._value = parsed;
                }
                else
                {
                    this._value = 0;
                }

                this.WasGenerated = true;
                return this._value;
            }
        }

        public LazyFloat(string? input)
        {
            this.Input = input;
        }

        public LazyFloat(float value)
        {
            this._value = value;
            this.WasGenerated = true;
        }

        public string? ToString(string format, bool kilo)
        {
            return kilo ? KiloFormat(this.Value, format) : this.Value.ToString(format, CultureInfo.InvariantCulture);
        }

        public override string? ToString()
        {
            return this.Value.ToString();
        }

        private static string KiloFormat(float num, string format) => num switch
        {
            >= 1000000 => (num / 1000000f).ToString(format, CultureInfo.InvariantCulture) + "M",
            >= 1000 => (num / 1000f).ToString(format, CultureInfo.InvariantCulture) + "K",
            _ => num.ToString(format, CultureInfo.InvariantCulture)
        };
    }
}