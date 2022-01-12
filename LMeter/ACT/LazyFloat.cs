
using System;
using System.Globalization;

namespace LMeter.ACT
{
    public class LazyFloat
    {
        private Func<string>? _getStringInput;
        private Func<float>? _getFloatInput;
        private float _value = 0;

        public string? Input { get; private set; }

        public bool WasGenerated { get; private set; }
        
        public float Value
        {
            get
            {
                if (this.WasGenerated)
                {
                    return _value;
                }

                if (this.Input is null)
                {
                    if (_getFloatInput is not null)
                    {
                        _value = _getFloatInput.Invoke();
                        this.WasGenerated = true;
                        return _value;
                    }
                    else if (_getStringInput is not null)
                    {
                        this.Input = _getStringInput.Invoke();
                    }
                }

                if (float.TryParse(this.Input, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) &&
                    !float.IsNaN(parsed))
                {
                    _value = parsed;
                }
                else
                {
                    _value = 0;
                }

                this.WasGenerated = true;
                return _value;
            }
        }

        public LazyFloat(string? input)
        {
            this.Input = input;
        }

        public LazyFloat(float value)
        {
            _value = value;
            this.WasGenerated = true;
        }

        public LazyFloat(Func<float> input)
        {
            _getFloatInput = input;
        }

        public LazyFloat(Func<string> input)
        {
            _getStringInput = input;
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