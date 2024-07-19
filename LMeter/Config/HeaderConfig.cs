using System.Numerics;
using System.Text.Json.Serialization;
using ImGuiNET;
using LMeter.Helpers;

namespace LMeter.Config
{
    public class HeaderConfig : IConfigPage
    {
        [JsonIgnore]
        public bool Active { get; set; }
        
        public string Name => "Header/Footer";

        public bool ShowHeader = true;
        public int HeaderHeight = 25;
        public ConfigColor BackgroundColor = new(30f / 255f, 30f / 255f, 30f / 255f, 230 / 255f);

        public bool ShowFooter = false;
        public int FooterHeight = 25;
        public ConfigColor FooterBackgroundColor = new(0, 0, 0, .5f);

        public bool ShowEncounterDuration = true;
        public ConfigColor DurationColor = new(0f / 255f, 190f / 255f, 225f / 255f, 1f);
        public bool DurationShowOutline = true;
        public ConfigColor DurationOutlineColor = new(0, 0, 0, 0.5f);
        public DrawAnchor DurationAlign = DrawAnchor.Left;
        public Vector2 DurationOffset = new(0, 0);
        public int DurationFontId = 0;
        public string DurationFontKey = FontsManager.DefaultSmallFontKey;

        public bool ShowEncounterName = true;
        public ConfigColor NameColor = new(1, 1, 1, 1);
        public bool NameShowOutline = true;
        public ConfigColor NameOutlineColor = new(0, 0, 0, 0.5f);
        public DrawAnchor NameAlign = DrawAnchor.Left;
        public Vector2 NameOffset = new(0, 0);
        public int NameFontId = 0;
        public string NameFontKey = FontsManager.DefaultSmallFontKey;

        public bool ShowRaidStats = true;
        public ConfigColor RaidStatsColor = new(0.5f, 0.5f, 0.5f, 1f);
        public bool StatsShowOutline = true;
        public ConfigColor StatsOutlineColor = new(0, 0, 0, 0.5f);
        public DrawAnchor StatsAlign = DrawAnchor.Right;
        public Vector2 StatsOffset = new(0, 0);
        public int StatsFontId = 0;
        public string StatsFontKey = FontsManager.DefaultSmallFontKey;
        public string RaidStatsFormat = "[dps]rdps [hps]rhps Deaths: [deaths]";
        public bool ThousandsSeparators = true;

        public bool ShowVersion = true;
        public Vector2 VersionOffset = new(0, 0);
        public int VersionFontId = 0;
        public ConfigColor VersionColor = new(0f / 255f, 190f / 255f, 225f / 255f, 1f);
        public bool VersionShowOutline = true;
        public ConfigColor VersionOutlineColor = new(0, 0, 0, 0.5f);
        public string VersionFontKey = FontsManager.DefaultSmallFontKey;

        public IConfigPage GetDefault()
        {
            HeaderConfig defaultConfig = new()
            {
                DurationFontKey = FontsManager.DefaultSmallFontKey,
                DurationFontId = FontsManager.GetFontIndex(FontsManager.DefaultSmallFontKey),

                NameFontKey = FontsManager.DefaultSmallFontKey,
                NameFontId = FontsManager.GetFontIndex(FontsManager.DefaultSmallFontKey),

                StatsFontKey = FontsManager.DefaultSmallFontKey,
                StatsFontId = FontsManager.GetFontIndex(FontsManager.DefaultSmallFontKey)
            };

            return defaultConfig;
        }

        public void DrawConfig(Vector2 size, float padX, float padY, bool border = true)
        {
            if (ImGui.BeginChild($"##{this.Name}", size, border))
            {
                ImGui.Checkbox("Show Header", ref this.ShowHeader);
                if (this.ShowHeader)
                {
                    DrawHelpers.DrawNestIndicator(1);
                    ImGui.DragInt("Height##Header", ref this.HeaderHeight, 1, 0, 100);

                    DrawHelpers.DrawNestIndicator(1);
                    DrawHelpers.DrawColorSelector("Background Color##Header", ref this.BackgroundColor);

                    DrawHelpers.DrawNestIndicator(1);
                    ImGui.Checkbox("Show LMeter Version when Cleared", ref this.ShowVersion);
                    if (this.ShowVersion)
                    {
                        DrawHelpers.DrawNestIndicator(2);
                        ImGui.DragFloat2("Offset##Version", ref this.VersionOffset);

                        DrawHelpers.DrawNestIndicator(2);
                        DrawHelpers.DrawFontSelector("Font##Version", ref this.VersionFontKey, ref this.VersionFontId);

                        DrawHelpers.DrawNestIndicator(2);
                        DrawHelpers.DrawColorSelector("Text Color##Version", ref this.VersionColor);

                        DrawHelpers.DrawNestIndicator(2);
                        ImGui.Checkbox("Show Outline##Version", ref this.VersionShowOutline);
                        if (this.VersionShowOutline)
                        {
                            DrawHelpers.DrawNestIndicator(3);
                            DrawHelpers.DrawColorSelector("Outline Color##Version", ref this.VersionOutlineColor);
                        }
                    }

                    ImGui.NewLine();
                }

                ImGui.Checkbox("Show Footer", ref this.ShowFooter);
                if (this.ShowFooter)
                {
                    DrawHelpers.DrawNestIndicator(1);
                    ImGui.DragInt("Height##Footer", ref this.FooterHeight, 1, 0, 100);

                    DrawHelpers.DrawNestIndicator(1);
                    DrawHelpers.DrawColorSelector("Background Color##Footer", ref this.BackgroundColor);
                }
                
                ImGui.EndChild();
            }
        }
    }
}
