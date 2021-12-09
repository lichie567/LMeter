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

namespace LMeter.Meter
{
    public class MeterWindow : IConfigurable
    {
        [JsonIgnore] public readonly string ID;
        [JsonIgnore] private bool LastFrameWasUnlocked = false;
        [JsonIgnore] private bool LastFrameWasDragging = false;
        [JsonIgnore] private bool LastFrameWasPreview = false;
        [JsonIgnore] private bool Unlocked = false;
        [JsonIgnore] private bool Hovered = false;
        [JsonIgnore] private bool Dragging = false;
        [JsonIgnore] private bool Locked = false;
        [JsonIgnore] private ACTEvent? PreviewEvent = null;
        [JsonIgnore] private int ScrollPosition = 0;

        [JsonIgnore] private DateTime? LastSortedTimestamp = null;
        [JsonIgnore] private List<Combatant> LastSortedCombatants = new List<Combatant>();

        public string Name { get; set; }

        public GeneralConfig GeneralConfig { get; set; }

        public HeaderConfig HeaderConfig { get; set; }

        public BarConfig BarConfig { get; set; }

        public BarColorsConfig BarColorsConfig { get; set; }

        public VisibilityConfig VisibilityConfig { get; set; }

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

        public void ImportPage(IConfigPage page)
        {
            if (page is GeneralConfig)
            {
                this.GeneralConfig = (GeneralConfig)page;
            }

            if (page is HeaderConfig)
            {
                this.HeaderConfig = (HeaderConfig)page;
            }
            
            if (page is BarConfig)
            {
                this.BarConfig = (BarConfig)page;
            }
            
            if (page is BarColorsConfig)
            {
                this.BarColorsConfig = (BarColorsConfig)page;
            }
            
            if (page is VisibilityConfig)
            {
                this.VisibilityConfig = (VisibilityConfig)page;
            }
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

        public void Clear()
        {
            this.LastSortedCombatants = new List<Combatant>();
            this.LastSortedTimestamp = null;
        }
        
        // Dont ask
        protected void UpdateDragData(Vector2 pos, Vector2 size, bool locked)
        {
            this.Unlocked = !locked;
            this.Hovered = ImGui.IsMouseHoveringRect(pos, pos + size);
            this.Dragging = this.LastFrameWasDragging && ImGui.IsMouseDown(ImGuiMouseButton.Left);
            this.Locked = (this.Unlocked && !this.LastFrameWasUnlocked || !this.Hovered) && !this.Dragging;
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

            if (ImGui.IsMouseHoveringRect(localPos, localPos + size))
            {
                this.ScrollPosition -= (int)ImGui.GetIO().MouseWheel;
            }

            this.UpdateDragData(localPos, size, this.GeneralConfig.Lock);
            bool needsInput = this.Unlocked || !this.GeneralConfig.ClickThrough;
            DrawHelpers.DrawInWindow($"##{this.ID}", localPos, size, needsInput, this.Locked || this.GeneralConfig.Lock, (drawList) =>
            {
                if (this.Unlocked)
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

                if (this.GeneralConfig.Preview && !this.LastFrameWasPreview)
                {
                    this.PreviewEvent = ACTEvent.GetTestData();
                }

                ACTEvent? actEvent = this.GeneralConfig.Preview ? this.PreviewEvent : ACTClient.GetLastEvent();
                localPos = this.HeaderConfig.DrawHeader(localPos, size, actEvent?.Encounter, drawList);
                size = size.AddY(-this.HeaderConfig.HeaderHeight);

                drawList.AddRectFilled(localPos, localPos + size, this.GeneralConfig.BackgroundColor.Base);

                this.DrawBars(drawList, localPos, size, actEvent);
            });

            this.LastFrameWasUnlocked = this.Unlocked;
            this.LastFrameWasPreview = this.GeneralConfig.Preview;
        }

        private void DrawBars(ImDrawListPtr drawList, Vector2 localPos, Vector2 size, ACTEvent? actEvent)
        {                
            if (actEvent is not null && actEvent.Combatants.Any())
            {
                List<Combatant> sortedCombatants = this.GetSortedCombatants(actEvent, this.GeneralConfig.DataType);
                
                string topDataSource = this.GeneralConfig.DataType switch
                {
                    MeterDataType.Damage => sortedCombatants[0].DamageTotal,
                    MeterDataType.Healing => sortedCombatants[0].HealingTotal,
                    MeterDataType.DamageTaken => sortedCombatants[0].DamageTaken,
                    _ => sortedCombatants[0].DamageTotal
                };

                if (float.TryParse(topDataSource, NumberStyles.Float, CultureInfo.InvariantCulture, out float top) && !float.IsNaN(top))
                {
                    int i = 0;
                    if (sortedCombatants.Count > this.BarConfig.BarCount)
                    {
                        i = Math.Clamp(this.ScrollPosition, 0, sortedCombatants.Count - this.BarConfig.BarCount);
                        this.ScrollPosition = i;
                    }

                    int max = Math.Min(i + this.BarConfig.BarCount, sortedCombatants.Count);
                    for (; i < max; i++)
                    {
                        Combatant combatant = sortedCombatants[i];

                        string currentDataSource = this.GeneralConfig.DataType switch
                        {
                            MeterDataType.Damage => combatant.DamageTotal,
                            MeterDataType.Healing => combatant.HealingTotal,
                            MeterDataType.DamageTaken => combatant.DamageTaken,
                            _ => combatant.DamageTotal
                        };

                        if (!float.TryParse(currentDataSource, NumberStyles.Float, CultureInfo.InvariantCulture, out float current) || float.IsNaN(current))
                        {
                            return;
                        }

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
                        
                        localPos = this.BarConfig.DrawBar(drawList, localPos, size, combatant, barColor, top, current);
                    }
                }
            }
        }

        private List<Combatant> GetSortedCombatants(ACTEvent actEvent, MeterDataType dataType)
        {
            if (this.LastSortedTimestamp.HasValue &&
                this.LastSortedTimestamp.Value == actEvent.Timestamp &&
                !this.GeneralConfig.Preview)
            {
                return this.LastSortedCombatants;
            }

            List<Combatant> sortedCombatants = actEvent.Combatants.Values.ToList();

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
                    MeterDataType.Damage => y.DamageTotal,
                    MeterDataType.Healing => y.HealingTotal,
                    MeterDataType.DamageTaken => y.DamageTaken,
                    _ => y.DamageTotal
                };

                if (!float.TryParse(yData, NumberStyles.Float, CultureInfo.InvariantCulture, out float yFloat))
                {
                    return -1;
                }

                if (!float.TryParse(xData, NumberStyles.Float, CultureInfo.InvariantCulture, out float xFloat))
                {
                    return 1;
                }
                
                return (int)(yFloat - xFloat);
            });

            this.LastSortedTimestamp = actEvent.Timestamp;
            this.LastSortedCombatants = sortedCombatants;
            return sortedCombatants;
        }
    }
}
