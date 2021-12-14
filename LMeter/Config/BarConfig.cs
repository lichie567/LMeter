using System;
using System.Numerics;
using ImGuiNET;
using LMeter.Helpers;
using LMeter.ACT;
using Dalamud.Game.ClientState;

namespace LMeter.Config
{
    public class BarConfig : IConfigPage
    {
        public string Name => "Bars";

        private static string[] _jobIconStyleOptions = new string[] { "Style 1", "Style 2" };

        public int BarCount = 8;
        public int BarGaps = 0;

        public bool ShowJobIcon = true;
        public int JobIconStyle = 0;
        public Vector2 JobIconOffset = new Vector2(0, 0);
        
        public bool ThousandsSeparators = true;

        public bool UseJobColor = true;
        public ConfigColor BarColor = new ConfigColor(.3f, .3f, .3f, 1f);

        public string BarNameFormat = " [name]";
        public ConfigColor BarNameColor = new ConfigColor(1, 1, 1, 1);
        public bool BarNameShowOutline = true;
        public ConfigColor BarNameOutlineColor = new ConfigColor(0, 0, 0, 0.5f);
        public string BarNameFontKey = FontsManager.DalamudFontKey;
        public int BarNameFontId = 0;
        public bool UseCharacterName = false;

        public string BarDataFormat = "[damagetotal-k.1]  ([encdps-k.1], [damagepct])  ";
        public ConfigColor BarDataColor = new ConfigColor(1, 1, 1, 1);
        public bool BarDataShowOutline = true;
        public ConfigColor BarDataOutlineColor = new ConfigColor(0, 0, 0, 0.5f);
        public string BarDataFontKey = FontsManager.DalamudFontKey;
        public int BarDataFontId = 0;

        public Vector2 DrawBar(
            ImDrawListPtr drawList,
            Vector2 localPos,
            Vector2 size,
            Combatant combatant,
            ConfigColor barColor,
            float top,
            float current)
        {
            float barHeight = (size.Y - (this.BarCount - 1) * this.BarGaps) / this.BarCount;
            Vector2 barSize = new Vector2(size.X, barHeight);
            Vector2 barFillSize = new Vector2(size.X * (current / top), barHeight);
            drawList.AddRectFilled(localPos, localPos + barFillSize, barColor.Base);

            if (this.ShowJobIcon && Enum.TryParse<Job>(combatant.Job, true, out Job job))
            {
                uint jobIconId = 62000u + (uint)job + 100u * (uint)this.JobIconStyle;
                Vector2 jobIconSize = Vector2.One * barHeight;
                DrawHelpers.DrawIcon(jobIconId, localPos + this.JobIconOffset, jobIconSize, drawList);
            }

            bool fontPushed = FontsManager.PushFont(this.BarNameFontKey);
            string nameText = combatant.GetFormattedString(this.BarNameFormat, this.ThousandsSeparators ? "N" : "F");
            if (this.UseCharacterName && combatant.Name.Contains("YOU"))
            {
                string characterName = Singletons.Get<ClientState>().LocalPlayer?.Name.ToString() ?? "YOU";
                nameText = nameText.Replace("YOU", characterName);
            }

            Vector2 nameTextSize = ImGui.CalcTextSize(nameText);
            Vector2 namePos = Utils.GetAnchoredPosition(localPos, -barSize, DrawAnchor.Left);
            namePos = Utils.GetAnchoredPosition(namePos, nameTextSize, DrawAnchor.Left);
            DrawHelpers.DrawText(drawList,
                nameText,
                namePos.AddX(this.ShowJobIcon ? barHeight : 5),
                this.BarNameColor.Base,
                this.BarNameShowOutline,
                this.BarNameOutlineColor.Base);

            if (fontPushed)
            {
                ImGui.PopFont();
            }

            fontPushed = FontsManager.PushFont(this.BarDataFontKey);
            string dataText = combatant.GetFormattedString(this.BarDataFormat, this.ThousandsSeparators ? "N" : "F");
            Vector2 dataTextSize = ImGui.CalcTextSize(dataText);
            Vector2 dataPos = Utils.GetAnchoredPosition(localPos, -barSize, DrawAnchor.Right);
            dataPos = Utils.GetAnchoredPosition(dataPos, dataTextSize, DrawAnchor.Right);
            DrawHelpers.DrawText(drawList,
                dataText,
                dataPos,
                this.BarDataColor.Base,
                this.BarDataShowOutline,
                this.BarDataOutlineColor.Base);

            if (fontPushed)
            {
                ImGui.PopFont();
            }

            return localPos.AddY(barHeight + this.BarGaps);
        }

        public void DrawConfig(Vector2 size, float padX, float padY)
        {            
            string[] fontOptions = FontsManager.GetFontList();
            if (fontOptions.Length == 0)
            {
                return;
            }

            if (ImGui.BeginChild($"##{this.Name}", new Vector2(size.X, size.Y), true))
            {
                ImGui.DragInt("Num Bars to Display", ref this.BarCount, 1, 1, 48);
                ImGui.DragInt("Bar Gap Size", ref this.BarGaps, 1, 0, 20);

                ImGui.Checkbox("Show Job Icon", ref this.ShowJobIcon);
                if (this.ShowJobIcon)
                {
                    DrawHelpers.DrawNestIndicator(1);
                    ImGui.DragFloat2("Job Icon Offset", ref this.JobIconOffset);

                    DrawHelpers.DrawNestIndicator(1);
                    ImGui.Combo("Job Icon Style", ref this.JobIconStyle, _jobIconStyleOptions, _jobIconStyleOptions.Length);
                }

                ImGui.Checkbox("Use Job Colors for Bars", ref this.UseJobColor);
                Vector4 vector = Vector4.Zero;
                if (!this.UseJobColor)
                {
                    DrawHelpers.DrawNestIndicator(1);
                    vector = this.BarColor.Vector;
                    ImGui.ColorEdit4("Bar Color", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
                    this.BarColor.Vector = vector;
                }
                
                ImGui.Checkbox("Use Thousands Separators for Numbers", ref this.ThousandsSeparators);

                ImGui.NewLine();
                ImGui.Checkbox("Use your name instead of 'YOU'", ref this.UseCharacterName);
                ImGui.InputText("Name Format", ref this.BarNameFormat, 128);

                if (ImGui.IsItemHovered())
                {
                    string tooltip = $"Available Data Tags:\n\n{string.Join("\n", Combatant.GetTags())}";
                    ImGui.SetTooltip(tooltip);
                }

                if (!FontsManager.ValidateFont(fontOptions, this.BarNameFontId, this.BarNameFontKey))
                {
                    this.BarNameFontId = 0;
                    this.BarNameFontKey = FontsManager.DalamudFontKey;
                }
                
                ImGui.Combo("Font##Name", ref this.BarNameFontId, fontOptions, fontOptions.Length);
                this.BarNameFontKey = fontOptions[this.BarNameFontId];
                
                vector = this.BarNameColor.Vector;
                ImGui.ColorEdit4("Text Color##Name", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
                this.BarNameColor.Vector = vector;

                ImGui.Checkbox("Show Outline##Name", ref this.BarNameShowOutline);
                if (this.BarNameShowOutline)
                {
                    DrawHelpers.DrawNestIndicator(1);
                    vector = this.BarNameOutlineColor.Vector;
                    ImGui.ColorEdit4("Outline Color##Name", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
                    this.BarNameOutlineColor.Vector = vector;
                }

                ImGui.NewLine();
                ImGui.InputText("Data Format", ref this.BarDataFormat, 128);

                if (ImGui.IsItemHovered())
                {
                    string tooltip = $"Available Data Tags:\n\n{string.Join("\n", Combatant.GetTags())}";
                    ImGui.SetTooltip(tooltip);
                }

                if (!FontsManager.ValidateFont(fontOptions, this.BarDataFontId, this.BarDataFontKey))
                {
                    this.BarDataFontId = 0;
                    this.BarDataFontKey = FontsManager.DalamudFontKey;
                }
                
                ImGui.Combo("Font##Data", ref this.BarDataFontId, fontOptions, fontOptions.Length);
                this.BarDataFontKey = fontOptions[this.BarDataFontId];
                
                vector = this.BarDataColor.Vector;
                ImGui.ColorEdit4("Text Color##Data", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
                this.BarDataColor.Vector = vector;

                ImGui.Checkbox("Show Outline##Data", ref this.BarDataShowOutline);
                if (this.BarDataShowOutline)
                {
                    DrawHelpers.DrawNestIndicator(1);
                    vector = this.BarDataOutlineColor.Vector;
                    ImGui.ColorEdit4("Outline Color##Data", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
                    this.BarDataOutlineColor.Vector = vector;
                }

                ImGui.EndChild();
            }
        }
    }
}
