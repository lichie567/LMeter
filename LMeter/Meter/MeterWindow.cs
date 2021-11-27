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
        [JsonIgnore] protected bool LastFrameWasPreview = false;
        [JsonIgnore] protected bool LastFrameWasDragging = false;
        [JsonIgnore] public bool Preview = false;
        [JsonIgnore] public bool Hovered = false;
        [JsonIgnore] public bool Dragging = false;
        [JsonIgnore] public bool Locked = false;

        public string Name { get; set; }

        public GeneralConfig GeneralConfig { get; init; }

        public BarConfig BarConfig { get; init; }

        public BarColorsConfig BarColorsConfig { get; init; }

        public VisibilityConfig VisibilityConfig { get; init; }

        public MeterWindow(string name)
        {
            this.Name = name;
            this.ID = $"LMeter_{GetType().Name}_{Guid.NewGuid()}";
            this.GeneralConfig = new GeneralConfig();
            this.BarConfig = new BarConfig();
            this.BarColorsConfig = new BarColorsConfig();
            this.VisibilityConfig = new VisibilityConfig();
        }

        public IEnumerable<IConfigPage> GetConfigPages()
        {
            yield return this.GeneralConfig;
            yield return this.BarConfig;
            yield return this.BarColorsConfig;
            yield return this.VisibilityConfig;
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

                drawList.AddRectFilled(localPos, localPos + size, this.GeneralConfig.BackgroundColor.Base);

                if (ACTClient.GetLastEvent(out ACTEvent? actEvent))
                {
                    // draw bars
                }

                if (this.GeneralConfig.ShowBorder)
                {
                    for (int i = 0; i < this.GeneralConfig.BorderThickness; i++)
                    {
                        Vector2 offset = new Vector2(i, i);
                        drawList.AddRect(localPos + offset, localPos + size - offset, this.GeneralConfig.BorderColor.Base);
                    }
                }
            });

            this.LastFrameWasPreview = this.Preview;
        }
    }
}
