

using System.Text.Json.Serialization;
using Dalamud.Bindings.ImGui;
using LMeter.Helpers;

namespace LMeter.Config
{
    public class RoundingOptions(bool enabled = false, float rounding = 10, RoundingFlag flag = RoundingFlag.All)
    {
        public bool Enabled = enabled;
        public float Rounding = rounding;
        public RoundingFlag Flag = flag;

        public ImDrawFlags GetImDrawFlag() => this.Flag switch
        {
            RoundingFlag.All => ImDrawFlags.RoundCornersAll,
            RoundingFlag.Left => ImDrawFlags.RoundCornersLeft,
            RoundingFlag.Right => ImDrawFlags.RoundCornersRight,
            RoundingFlag.Top => ImDrawFlags.RoundCornersTop,
            RoundingFlag.TopRight => ImDrawFlags.RoundCornersTopRight,
            RoundingFlag.TopLeft => ImDrawFlags.RoundCornersTopLeft,
            RoundingFlag.Bottom => ImDrawFlags.RoundCornersBottom,
            RoundingFlag.BottomRight => ImDrawFlags.RoundCornersBottomRight,
            RoundingFlag.BottomLeft => ImDrawFlags.RoundCornersBottomLeft,
            _ => ImDrawFlags.RoundCornersAll,
        };
    }
}
