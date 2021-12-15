using System.Linq;
using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using LMeter.Config;
using LMeter.Helpers;
using LMeter.ACT;
using Newtonsoft.Json;

namespace LMeter.Meter
{
    public class MeterWindow : IConfigurable
    {
        [JsonIgnore] public readonly string ID;

        [JsonIgnore] private bool _lastFrameWasUnlocked = false;
        [JsonIgnore] private bool _lastFrameWasDragging = false;
        [JsonIgnore] private bool _lastFrameWasPreview = false;
        [JsonIgnore] private bool _unlocked = false;
        [JsonIgnore] private bool _hovered = false;
        [JsonIgnore] private bool _dragging = false;
        [JsonIgnore] private bool _locked = false;
        [JsonIgnore] private ACTEvent? _previewEvent = null;
        [JsonIgnore] private int _scrollPosition = 0;
        [JsonIgnore] private DateTime? _lastSortedTimestamp = null;
        [JsonIgnore] private List<Combatant> _lastSortedCombatants = new List<Combatant>();

        public string Name { get; set; }

        public GeneralConfig GeneralConfig { get; set; }

        public HeaderConfig HeaderConfig { get; set; }

        public BarConfig BarConfig { get; set; }

        public BarColorsConfig BarColorsConfig { get; set; }

        public VisibilityConfig VisibilityConfig { get; set; }

        public MeterWindow(string name)
        {
            this.Name = name;
            this.ID = $"LMeter_MeterWindow_{Guid.NewGuid()}";
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
            newMeter.HeaderConfig = (HeaderConfig)newMeter.HeaderConfig.GetDefault();
            newMeter.BarConfig = (BarConfig)newMeter.BarConfig.GetDefault();
            return newMeter;
        }

        public void Clear()
        {
            this._lastSortedCombatants = new List<Combatant>();
            this._lastSortedTimestamp = null;
        }
        
        // Dont ask
        protected void UpdateDragData(Vector2 pos, Vector2 size, bool locked)
        {
            this._unlocked = !locked;
            this._hovered = ImGui.IsMouseHoveringRect(pos, pos + size);
            this._dragging = this._lastFrameWasDragging && ImGui.IsMouseDown(ImGuiMouseButton.Left);
            this._locked = (this._unlocked && !this._lastFrameWasUnlocked || !this._hovered) && !this._dragging;
            this._lastFrameWasDragging = this._hovered || this._dragging;
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
                this._scrollPosition -= (int)ImGui.GetIO().MouseWheel;
            }

            this.UpdateDragData(localPos, size, this.GeneralConfig.Lock);
            bool needsInput = !this.GeneralConfig.ClickThrough;
            DrawHelpers.DrawInWindow($"##{this.ID}", localPos, size, needsInput, this._locked || this.GeneralConfig.Lock, (drawList) =>
            {
                if (this._unlocked)
                {
                    if (this._lastFrameWasDragging)
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

                if (this.GeneralConfig.Preview && !this._lastFrameWasPreview)
                {
                    this._previewEvent = ACTEvent.GetTestData();
                }

                ACTEvent? actEvent = this.GeneralConfig.Preview ? this._previewEvent : ACTClient.GetLastEvent();
                localPos = this.HeaderConfig.DrawHeader(localPos, size, actEvent?.Encounter, drawList);
                size = size.AddY(-this.HeaderConfig.HeaderHeight);

                drawList.AddRectFilled(localPos, localPos + size, this.GeneralConfig.BackgroundColor.Base);

                this.DrawBars(drawList, localPos, size, actEvent);
            });

            this._lastFrameWasUnlocked = this._unlocked;
            this._lastFrameWasPreview = this.GeneralConfig.Preview;
        }

        private void DrawBars(ImDrawListPtr drawList, Vector2 localPos, Vector2 size, ACTEvent? actEvent)
        {                
            if (actEvent?.Combatants is not null && actEvent.Combatants.Any())
            {
                List<Combatant> sortedCombatants = this.GetSortedCombatants(actEvent, this.GeneralConfig.DataType);
                
                float top = this.GeneralConfig.DataType switch
                {
                    MeterDataType.Damage => sortedCombatants[0].DamageTotal?.Value ?? 0,
                    MeterDataType.Healing => sortedCombatants[0].HealingTotal?.Value ?? 0,
                    MeterDataType.DamageTaken => sortedCombatants[0].DamageTaken?.Value ?? 0,
                    _ => 0
                };

                int i = 0;
                if (sortedCombatants.Count > this.BarConfig.BarCount)
                {
                    i = Math.Clamp(this._scrollPosition, 0, sortedCombatants.Count - this.BarConfig.BarCount);
                    this._scrollPosition = i;
                }

                int maxIndex = Math.Min(i + this.BarConfig.BarCount, sortedCombatants.Count);
                for (; i < maxIndex; i++)
                {
                    Combatant combatant = sortedCombatants[i];

                    float current = this.GeneralConfig.DataType switch
                    {
                        MeterDataType.Damage => combatant.DamageTotal?.Value ?? 0,
                        MeterDataType.Healing => combatant.HealingTotal?.Value ?? 0,
                        MeterDataType.DamageTaken => combatant.DamageTaken?.Value ?? 0,
                        _ => 0
                    };

                    ConfigColor barColor;
                    if (this.BarConfig.UseJobColor)
                    {
                        barColor = this.BarColorsConfig.GetColor(combatant.Job);
                    }
                    else
                    {
                        barColor = this.BarConfig.BarColor;
                    }
                    
                    localPos = this.BarConfig.DrawBar(drawList, localPos, size, combatant, barColor, top, current);
                }
            }
        }

        private List<Combatant> GetSortedCombatants(ACTEvent actEvent, MeterDataType dataType)
        {
            if (actEvent.Combatants is null ||
                this._lastSortedTimestamp.HasValue &&
                this._lastSortedTimestamp.Value == actEvent.Timestamp &&
                !this.GeneralConfig.Preview)
            {
                return this._lastSortedCombatants;
            }

            List<Combatant> sortedCombatants = actEvent.Combatants.Values.ToList();

            sortedCombatants.Sort((x, y) =>
            {
                float xFloat = dataType switch
                {
                    MeterDataType.Damage => x.DamageTotal?.Value ?? 0,
                    MeterDataType.Healing => x.HealingTotal?.Value ?? 0,
                    MeterDataType.DamageTaken => x.DamageTaken?.Value ?? 0,
                    _ => 0
                };

                float yFloat = dataType switch
                {
                    MeterDataType.Damage => y.DamageTotal?.Value ?? 0,
                    MeterDataType.Healing => y.HealingTotal?.Value ?? 0,
                    MeterDataType.DamageTaken => y.DamageTaken?.Value ?? 0,
                    _ => 0
                };
                
                return (int)(yFloat - xFloat);
            });

            this._lastSortedTimestamp = actEvent.Timestamp;
            this._lastSortedCombatants = sortedCombatants;
            return sortedCombatants;
        }
    }
}
