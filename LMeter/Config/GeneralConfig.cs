using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Dalamud.Bindings.ImGui;
using LMeter.Helpers;
using Newtonsoft.Json;

namespace LMeter.Config
{
    public class GeneralConfig : IConfigPage
    {
        [JsonIgnore]
        private static readonly string[] m_meterTypeOptions = Enum.GetNames<MeterDataType>();

        [JsonIgnore]
        public bool Preview = false;

        [JsonIgnore]
        public bool Active { get; set; }

        public string Name => "General";
        public Vector2 Position = Vector2.Zero;
        public Vector2 Size = new(ImGui.GetMainViewport().Size.Y * 16 / 90, ImGui.GetMainViewport().Size.Y / 10);
        public bool Lock = false;
        public bool ClickThrough = false;
        public ConfigColor BackgroundColor = new(0, 0, 0, 0.5f);
        public bool ShowBorder = true;
        public bool BorderAroundBars = false;
        public ConfigColor BorderColor = new(30f / 255f, 30f / 255f, 30f / 255f, 230f / 255f);
        public int BorderThickness = 2;
        public MeterDataType DataType = MeterDataType.Damage;
        public bool ReturnToCurrent = true;
        public RoundingOptions Rounding = new(false, 10f, RoundingFlag.Bottom);
        public RoundingOptions BorderRounding = new(false, 10f, RoundingFlag.All);

        public IConfigPage GetDefault() => new GeneralConfig();

        public void DrawConfig(Vector2 size, float padX, float padY, bool border = true)
        {
            if (ImGui.BeginChild($"##{this.Name}", new Vector2(size.X, size.Y), border))
            {
                Vector2 screenSize = ImGui.GetMainViewport().Size;
                ImGui.DragFloat2("Position", ref this.Position, 1, -screenSize.X / 2, screenSize.X / 2);
                ImGui.DragFloat2("Size", ref this.Size, 1, 0, screenSize.Y);
                ImGui.Checkbox("Lock", ref this.Lock);
                ImGui.Checkbox("Click Through", ref this.ClickThrough);
                ImGui.Checkbox("Preview", ref this.Preview);
                ImGui.NewLine();

                DrawHelpers.DrawColorSelector("Background Color", this.BackgroundColor);
                DrawHelpers.DrawRoundingOptions("Use Rounded Corners", 0, this.Rounding);

                ImGui.NewLine();
                ImGui.Checkbox("Show Border", ref this.ShowBorder);
                if (this.ShowBorder)
                {
                    DrawHelpers.DrawNestIndicator(1);
                    ImGui.DragInt("Border Thickness", ref this.BorderThickness, 1, 1, 20);

                    DrawHelpers.DrawNestIndicator(1);
                    DrawHelpers.DrawColorSelector("Border Color", this.BorderColor);

                    DrawHelpers.DrawNestIndicator(1);
                    DrawHelpers.DrawRoundingOptions("Use Rounded Corners##Border", 1, this.BorderRounding);

                    DrawHelpers.DrawNestIndicator(1);
                    ImGui.Checkbox("Hide border around Header", ref this.BorderAroundBars);
                }

                ImGui.NewLine();
                ImGui.Combo(
                    "Sort Type",
                    ref Unsafe.As<MeterDataType, int>(ref this.DataType),
                    m_meterTypeOptions,
                    m_meterTypeOptions.Length
                );

                ImGui.Checkbox("Return to Current Data when entering combat", ref this.ReturnToCurrent);
            }

            ImGui.EndChild();
        }
    }
}
