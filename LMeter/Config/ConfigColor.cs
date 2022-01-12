using System.Numerics;
using ImGuiNET;
using Newtonsoft.Json;

namespace LMeter.Config
{
    public class ConfigColor
    {
        [JsonIgnore] private float[] _colorMapRatios = { -.8f, -.3f, .1f };
        
        // Constructor for deserialization
        public ConfigColor() : this(Vector4.Zero) { }

        public ConfigColor(Vector4 vector, float[]? colorMapRatios = null)
        {
            if (colorMapRatios != null && colorMapRatios.Length == 3)
            {
                _colorMapRatios = colorMapRatios;
            }

            this.Vector = vector;
        }

        public ConfigColor(float r, float g, float b, float a, float[]? colorMapRatios = null) : this(new Vector4(r, g, b, a), colorMapRatios)
        {
        }

        [JsonIgnore] private Vector4 _vector;
        public Vector4 Vector
        {
            get => _vector;
            set
            {
                if (_vector == value)
                {
                    return;
                }

                _vector = value;

                Update();
            }
        }

        [JsonIgnore] public uint Base { get; private set; }

        [JsonIgnore] public uint Background { get; private set; }

        [JsonIgnore] public uint TopGradient { get; private set; }

        [JsonIgnore] public uint BottomGradient { get; private set; }

        private void Update()
        {
            Base = ImGui.ColorConvertFloat4ToU32(_vector);
            // Background = ImGui.ColorConvertFloat4ToU32(_vector.AdjustColor(_colorMapRatios[0]));
            // TopGradient = ImGui.ColorConvertFloat4ToU32(_vector.AdjustColor(_colorMapRatios[1]));
            // BottomGradient = ImGui.ColorConvertFloat4ToU32(_vector.AdjustColor(_colorMapRatios[2]));
        }
    }
}
