using System;
using System.Numerics;
using ImGuiNET;
using LMeter.Helpers;
using Newtonsoft.Json;

namespace LMeter.Config
{
    public class HeaderConfig : IConfigPage
    {
        [JsonIgnore]
        private static readonly string[] _anchorOptions = Enum.GetNames(typeof(DrawAnchor));

        public string Name => "Header";

        public bool ShowHeader = true;
        public int HeaderHeight = 25;
        public ConfigColor BackgroundColor = new(30f / 255f, 30f / 255f, 30f / 255f, 230 / 255f);

        public bool ShowEncounterDuration = true;
        public ConfigColor DurationColor = new(0f / 255f, 190f / 255f, 225f / 255f, 1f);
        public bool DurationShowOutline = true;
        public ConfigColor DurationOutlineColor = new(0, 0, 0, 0.5f);
        public DrawAnchor DurationAlign = DrawAnchor.Left;
        public Vector2 DurationOffset = new(0, 0);
        public int DurationFontId = 0;
        public string DurationFontKey = FontsManager.DalamudFontKey;

        public bool ShowEncounterName = true;
        public ConfigColor NameColor = new(1, 1, 1, 1);
        public bool NameShowOutline = true;
        public ConfigColor NameOutlineColor = new(0, 0, 0, 0.5f);
        public DrawAnchor NameAlign = DrawAnchor.Left;
        public Vector2 NameOffset = new(0, 0);
        public int NameFontId = 0;
        public string NameFontKey = FontsManager.DalamudFontKey;

        public bool ShowRaidStats = true;
        public ConfigColor RaidStatsColor = new(0.5f, 0.5f, 0.5f, 1f);
        public bool StatsShowOutline = true;
        public ConfigColor StatsOutlineColor = new(0, 0, 0, 0.5f);
        public DrawAnchor StatsAlign = DrawAnchor.Right;
        public Vector2 StatsOffset = new(0, 0);
        public int StatsFontId = 0;
        public string StatsFontKey = FontsManager.DalamudFontKey;
        public string RaidStatsFormat = "[dps]rdps [hps]rhps Deaths: [deaths]";
        public bool ThousandsSeparators = true;

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
            string[] fontOptions = FontsManager.GetFontList();
            if (fontOptions.Length == 0)
            {
                return;
            }

            if (ImGui.BeginChild($"##{this.Name}", size, border))
            {
                ImGui.Checkbox("Show Header", ref this.ShowHeader);
                if (this.ShowHeader)
                {
                    DrawHelpers.DrawNestIndicator(1);
                    ImGui.DragInt("Header Height", ref this.HeaderHeight, 1, 0, 100);

                    DrawHelpers.DrawNestIndicator(1);
                    Vector4 vector = this.BackgroundColor.Vector;
                    ImGui.ColorEdit4("Background Color", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
                    this.BackgroundColor.Vector = vector;
                }
                
                ImGui.EndChild();
            }
        }
    }
}
