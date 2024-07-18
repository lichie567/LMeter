using System.Numerics;
using ImGuiNET;
using LMeter.Helpers;

namespace LMeter.Config
{
    public class BarConfig : IConfigPage
    {
        public string Name => "Bars";

        private static readonly string[] _jobIconStyleOptions = ["Style 1", "Style 2"];

        public int BarHeightType = 0;
        public int BarCount = 8;
        public int BarGaps = 1;
        public float BarHeight = 25;

        public bool ShowJobIcon = true;
        public int JobIconSizeType = 0;
        public Vector2 JobIconSize = new(25, 25);
        public int JobIconStyle = 0;
        public Vector2 JobIconOffset = new(0, 0);
        public bool ThousandsSeparators = true;

        public bool UseJobColor = true;
        public ConfigColor BarColor = new(.3f, .3f, .3f, 1f);

        public bool ShowRankText = false;
        public string RankTextFormat = "[rank].";
        public DrawAnchor RankTextAlign = DrawAnchor.Right;
        public Vector2 RankTextOffset = new(0, 0);
        public bool RankTextJobColor = false;
        public ConfigColor RankTextColor = new(1, 1, 1, 1);
        public bool RankTextShowOutline = true;
        public ConfigColor RankTextOutlineColor = new(0, 0, 0, 0.5f);
        public string RankTextFontKey = FontsManager.DalamudFontKey;
        public int RankTextFontId = 0;
        public bool AlwaysShowSelf = false;

        public string LeftTextFormat = "[name]";
        public Vector2 LeftTextOffset = new(0, 0);
        public bool LeftTextJobColor = false;
        public ConfigColor BarNameColor = new(1, 1, 1, 1);
        public bool BarNameShowOutline = true;
        public ConfigColor BarNameOutlineColor = new(0, 0, 0, 0.5f);
        public string BarNameFontKey = FontsManager.DalamudFontKey;
        public int BarNameFontId = 0;
        public bool UseCharacterName = false;

        public string RightTextFormat = "[damagetotal:k.1]  ([dps:k.1], [damagepct])";
        public Vector2 RightTextOffset = new(0, 0);
        public bool RightTextJobColor = false;
        public ConfigColor BarDataColor = new(1, 1, 1, 1);
        public bool BarDataShowOutline = true;
        public ConfigColor BarDataOutlineColor = new(0, 0, 0, 0.5f);
        public string BarDataFontKey = FontsManager.DalamudFontKey;
        public int BarDataFontId = 0;

        public bool ShowColumnHeader;
        public float ColumnHeaderHeight = 25;
        public ConfigColor ColumnHeaderColor = new(0, 0, 0, 0.5f);
        public ConfigColor ColumnHeaderTextColor = new(0, 0, 0, 0.5f);
        public bool ColumnHeaderShowOutline = true;
        public ConfigColor ColumnHeaderOutlineColor = new(0, 0, 0, 0.5f);

        public IConfigPage GetDefault()
        {
            BarConfig defaultConfig = new()
            {
                BarNameFontKey = FontsManager.DefaultSmallFontKey,
                BarNameFontId = FontsManager.GetFontIndex(FontsManager.DefaultSmallFontKey),

                BarDataFontKey = FontsManager.DefaultSmallFontKey,
                BarDataFontId = FontsManager.GetFontIndex(FontsManager.DefaultSmallFontKey),

                RankTextFontKey = FontsManager.DefaultSmallFontKey,
                RankTextFontId = FontsManager.GetFontIndex(FontsManager.DefaultSmallFontKey)
            };

            return defaultConfig;
        }

        public void DrawConfig(Vector2 size, float padX, float padY, bool border = true)
        {
            string[] fontOptions = FontsManager.GetFontList();
            if (fontOptions.Length == 0)
            {
                return;
            }

            if (ImGui.BeginChild($"##{this.Name}", new Vector2(size.X, size.Y), border))
            {
                ImGui.Text("Bar Height Type");
                ImGui.RadioButton("Constant Bar Number", ref this.BarHeightType, 0);
                ImGui.SameLine();
                ImGui.RadioButton("Constant Bar Height", ref this.BarHeightType, 1);

                if (this.BarHeightType == 0)
                {
                    ImGui.DragInt("Num Bars to Display", ref this.BarCount, 1, 1, 48);
                }
                else if (this.BarHeightType == 1)
                {
                    ImGui.DragFloat("Bar Height", ref this.BarHeight, .1f, 1, 100);
                }
                
                ImGui.DragInt("Bar Gap Size", ref this.BarGaps, 1, 0, 20);

                ImGui.NewLine();
                ImGui.Checkbox("Show Job Icon", ref this.ShowJobIcon);
                if (this.ShowJobIcon)
                {
                    DrawHelpers.DrawNestIndicator(1);
                    ImGui.SameLine();
                    ImGui.RadioButton("Automatic Size", ref this.JobIconSizeType, 0);
                    ImGui.SameLine();
                    ImGui.RadioButton("Manual Size", ref this.JobIconSizeType, 1);

                    if (this.JobIconSizeType == 1)
                    {
                        DrawHelpers.DrawNestIndicator(1);
                        ImGui.DragFloat2("Size##JobIconSize", ref this.JobIconSize);
                    }

                    DrawHelpers.DrawNestIndicator(1);
                    ImGui.DragFloat2("Job Icon Offset", ref this.JobIconOffset);

                    DrawHelpers.DrawNestIndicator(1);
                    ImGui.Combo("Job Icon Style", ref this.JobIconStyle, _jobIconStyleOptions, _jobIconStyleOptions.Length);
                }

                ImGui.NewLine();
                ImGui.Checkbox("Use Job Colors for Bars", ref this.UseJobColor);
                if (!this.UseJobColor)
                {
                    DrawHelpers.DrawNestIndicator(1);
                    Vector4 vector = this.BarColor.Vector;
                    ImGui.ColorEdit4("Bar Color", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
                    this.BarColor.Vector = vector;
                }

                ImGui.Checkbox("Use your name instead of 'YOU'", ref this.UseCharacterName);
                ImGui.Checkbox("Always show your own bar", ref this.AlwaysShowSelf);

                ImGui.NewLine();
                ImGui.Checkbox("Show Column Header Bar", ref this.ShowColumnHeader);
                if (this.ShowColumnHeader)
                {
                    DrawHelpers.DrawNestIndicator(1);
                    ImGui.DragFloat("Column Header Height", ref this.ColumnHeaderHeight);

                    DrawHelpers.DrawNestIndicator(1);
                    Vector4 vector = this.ColumnHeaderColor.Vector;
                    ImGui.ColorEdit4("Background Color", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
                    this.ColumnHeaderColor.Vector = vector;

                    DrawHelpers.DrawNestIndicator(1);
                    vector = this.ColumnHeaderTextColor.Vector;
                    ImGui.ColorEdit4("Text Color", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
                    this.ColumnHeaderTextColor.Vector = vector;

                    DrawHelpers.DrawNestIndicator(1);
                    ImGui.Checkbox("Show Outline", ref this.ColumnHeaderShowOutline);
                    if (this.ColumnHeaderShowOutline)
                    {
                        DrawHelpers.DrawNestIndicator(2);
                        vector = this.ColumnHeaderOutlineColor.Vector;
                        ImGui.ColorEdit4("Column Header Color", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
                        this.ColumnHeaderOutlineColor.Vector = vector;
                    }
                }
            }

            ImGui.EndChild();
        }
    }
}
