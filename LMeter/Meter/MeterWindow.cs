using System.Runtime.CompilerServices;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using LMeter.Config;
using LMeter.Helpers;
using LMeter.ACT;
using Newtonsoft.Json;
using System.Globalization;
using Dalamud.Game.ClientState;

namespace LMeter.Meter
{
    public class MeterWindow : IConfigurable
    {
        [JsonIgnore] public readonly string ID;
        [JsonIgnore] protected bool LastFrameWasPreview = false;
        [JsonIgnore] protected bool LastFrameWasDragging = false;
        [JsonIgnore] public bool Preview = false;
        [JsonIgnore] public bool Hovered = false;
        [JsonIgnore] public bool Dragging = false;
        [JsonIgnore] public bool Locked = false;

        public string Name { get; set; }

        public GeneralConfig GeneralConfig { get; init; }

        public HeaderConfig HeaderConfig { get; init; }

        public BarConfig BarConfig { get; init; }

        public BarColorsConfig BarColorsConfig { get; init; }

        public VisibilityConfig VisibilityConfig { get; init; }

        public MeterWindow(string name)
        {
            this.Name = name;
            this.ID = $"LMeter_{GetType().Name}_{Guid.NewGuid()}";
            this.GeneralConfig = new GeneralConfig();
            this.HeaderConfig = new HeaderConfig();
            this.BarConfig = new BarConfig();
            this.BarColorsConfig = new BarColorsConfig();
            this.VisibilityConfig = new VisibilityConfig();
        }

        public IEnumerable<IConfigPage> GetConfigPages()
        {
            yield return this.GeneralConfig;
            yield return this.HeaderConfig;
            yield return this.BarConfig;
            yield return this.BarColorsConfig;
            yield return this.VisibilityConfig;
        }

        public static MeterWindow GetDefaultMeter(string name)
        {
            MeterWindow newMeter = new MeterWindow(name);
            newMeter.HeaderConfig.DurationFontKey = FontsManager.DefaultSmallFontKey;
            newMeter.HeaderConfig.DurationFontId = Singletons.Get<FontsManager>().GetFontIndex(FontsManager.DefaultSmallFontKey);
            newMeter.HeaderConfig.NameFontKey = FontsManager.DefaultSmallFontKey;
            newMeter.HeaderConfig.NameFontId = Singletons.Get<FontsManager>().GetFontIndex(FontsManager.DefaultSmallFontKey);
            newMeter.HeaderConfig.StatsFontKey = FontsManager.DefaultSmallFontKey;
            newMeter.HeaderConfig.StatsFontId = Singletons.Get<FontsManager>().GetFontIndex(FontsManager.DefaultSmallFontKey);

            newMeter.BarConfig.BarNameFontKey = FontsManager.DefaultSmallFontKey;
            newMeter.BarConfig.BarNameFontId = Singletons.Get<FontsManager>().GetFontIndex(FontsManager.DefaultSmallFontKey);
            newMeter.BarConfig.BarDataFontKey = FontsManager.DefaultSmallFontKey;
            newMeter.BarConfig.BarDataFontId = Singletons.Get<FontsManager>().GetFontIndex(FontsManager.DefaultSmallFontKey);

            return newMeter;
        }
        
        // Dont ask
        protected void UpdateDragData(Vector2 pos, Vector2 size, bool locked)
        {
            this.Preview = !locked;
            this.Hovered = ImGui.IsMouseHoveringRect(pos, pos + size);
            this.Dragging = this.LastFrameWasDragging && ImGui.IsMouseDown(ImGuiMouseButton.Left);
            this.Locked = (this.Preview && !this.LastFrameWasPreview || !this.Hovered) && !this.Dragging;
            this.LastFrameWasDragging = this.Hovered || this.Dragging;
        }

        public void Draw(Vector2 pos)
        {
            if (!this.VisibilityConfig.IsVisible())
            {
                return;
            }

            Vector2 localPos = pos + this.GeneralConfig.Position;
            Vector2 size = this.GeneralConfig.Size;

            this.UpdateDragData(localPos, size, this.GeneralConfig.Lock);
            bool needsInput = this.Preview || !this.GeneralConfig.ClickThrough;
            DrawHelpers.DrawInWindow($"##{this.ID}", localPos, size, needsInput, this.Locked || this.GeneralConfig.Lock, (drawList) =>
            {
                if (this.Preview)
                {
                    if (this.LastFrameWasDragging)
                    {
                        localPos = ImGui.GetWindowPos();
                        this.GeneralConfig.Position = localPos - pos;

                        size = ImGui.GetWindowSize();
                        this.GeneralConfig.Size = size;
                    }
                }
                
                if (this.GeneralConfig.ShowBorder)
                {
                    for (int i = 0; i < this.GeneralConfig.BorderThickness; i++)
                    {
                        Vector2 offset = new Vector2(i, i);
                        drawList.AddRect(localPos + offset, localPos + size - offset, this.GeneralConfig.BorderColor.Base);
                    }

                    localPos += Vector2.One * this.GeneralConfig.BorderThickness;
                    size -= Vector2.One * this.GeneralConfig.BorderThickness * 2;
                }

                ACTClient.GetLastEvent(out ACTEvent? actEvent);
                localPos = this.HeaderConfig.DrawHeader(localPos, size, actEvent?.Encounter, drawList);

                drawList.AddRectFilled(localPos, localPos + size.AddY(-this.HeaderConfig.HeaderHeight), this.GeneralConfig.BackgroundColor.Base);

                if (actEvent is not null && actEvent.Combatants.Any())
                {
                    List<Combatant> sortedCombatants = this.GetSortedCombatants(actEvent.Combatants.Values, this.GeneralConfig.DataType);
                    
                    string topDataSource = this.GeneralConfig.DataType switch
                    {
                        MeterDataType.Damage => sortedCombatants[0].DamageTotal,
                        MeterDataType.Healing => sortedCombatants[0].HealingTotal,
                        MeterDataType.DamageTaken => sortedCombatants[0].DamageTaken,
                        _ => sortedCombatants[0].DamageTotal
                    };

                    if (float.TryParse(topDataSource, NumberStyles.Float, CultureInfo.InvariantCulture, out float top) && !float.IsNaN(top))
                    {
                        foreach (Combatant combatant in sortedCombatants)
                        {
                            string currentDataSource = this.GeneralConfig.DataType switch
                            {
                                MeterDataType.Damage => combatant.DamageTotal,
                                MeterDataType.Healing => combatant.HealingTotal,
                                MeterDataType.DamageTaken => combatant.DamageTaken,
                                _ => combatant.DamageTotal
                            };

                            if (!float.TryParse(currentDataSource, NumberStyles.Float, CultureInfo.InvariantCulture, out float current) || float.IsNaN(current))
                            {
                                continue;
                            }

                            Vector2 barSize = new Vector2(size.X, this.BarConfig.BarHeight);
                            ConfigColor barColor;
                            if (this.BarConfig.UseJobColor)
                            {
                                if (Enum.TryParse<Job>(combatant.Job, true, out Job job))
                                {
                                    barColor = this.BarColorsConfig.GetColor(job);
                                }
                                else
                                {
                                    barColor = this.BarColorsConfig.UKNColor;
                                }
                            }
                            else
                            {
                                barColor = this.BarConfig.BarColor;
                            }

                            Vector2 barFillSize = new Vector2(size.X * (current / top), this.BarConfig.BarHeight);
                            drawList.AddRectFilled(localPos, localPos + barFillSize, barColor.Base);

                            if (this.BarConfig.ShowJobIcon && Enum.TryParse<Job>(combatant.Job, true, out Job j))
                            {
                                uint jobIconId = 62000u + (uint)j + 100u * (uint)this.BarConfig.JobIconStyle;
                                Vector2 jobIconSize = new Vector2(this.BarConfig.BarHeight, this.BarConfig.BarHeight);
                                DrawHelpers.DrawIcon(jobIconId, localPos + this.BarConfig.JobIconOffset, jobIconSize, drawList);
                            }

                            bool fontPushed = FontsManager.PushFont(this.BarConfig.BarNameFontKey);
                            string nameText = combatant.GetFormattedString(this.BarConfig.BarNameFormat);
                            Vector2 nameTextSize = ImGui.CalcTextSize(nameText);
                            Vector2 namePos = Utils.GetAnchoredPosition(localPos, -barSize, DrawAnchor.Left);
                            namePos = Utils.GetAnchoredPosition(namePos, nameTextSize, DrawAnchor.Left);
                            DrawHelpers.DrawText(drawList,
                                nameText,
                                namePos,
                                this.BarConfig.BarNameColor.Base,
                                this.BarConfig.BarNameShowOutline,
                                this.BarConfig.BarNameOutlineColor.Base);

                            if (fontPushed)
                            {
                                ImGui.PopFont();
                            }

                            fontPushed = FontsManager.PushFont(this.BarConfig.BarDataFontKey);
                            string dataText = combatant.GetFormattedString(this.BarConfig.BarDataFormat);
                            Vector2 dataTextSize = ImGui.CalcTextSize(dataText);
                            Vector2 dataPos = Utils.GetAnchoredPosition(localPos, -barSize, DrawAnchor.Right);
                            dataPos = Utils.GetAnchoredPosition(dataPos, dataTextSize, DrawAnchor.Right);
                            DrawHelpers.DrawText(drawList,
                                dataText,
                                dataPos,
                                this.BarConfig.BarDataColor.Base,
                                this.BarConfig.BarDataShowOutline,
                                this.BarConfig.BarDataOutlineColor.Base);

                            if (fontPushed)
                            {
                                ImGui.PopFont();
                            }

                            localPos += new Vector2(0, this.BarConfig.BarHeight);
                        }
                    }
                }
            });

            this.LastFrameWasPreview = this.Preview;
        }

        private List<Combatant> GetSortedCombatants(IEnumerable<Combatant> combatants, MeterDataType dataType)
        {
            List<Combatant> sortedCombatants = combatants.ToList();

            sortedCombatants.Sort((x, y) =>
            {
                string xData = dataType switch
                {
                    MeterDataType.Damage => x.DamageTotal,
                    MeterDataType.Healing => x.HealingTotal,
                    MeterDataType.DamageTaken => x.DamageTaken,
                    _ => x.DamageTotal
                };

                string yData = dataType switch
                {
                    MeterDataType.Damage => x.EncDps,
                    MeterDataType.Healing => x.EncHps,
                    MeterDataType.DamageTaken => x.DamageTaken,
                    _ => x.EncDps
                };

                if (!float.TryParse(yData, NumberStyles.Float, CultureInfo.InvariantCulture, out float yFloat))
                {
                    return -1;
                }

                if (!float.TryParse(xData, NumberStyles.Float, CultureInfo.InvariantCulture, out float xFloat))
                {
                    return 1;
                }

                return (int)(xFloat - yFloat);
            });

            return sortedCombatants;
        }
    }
}
