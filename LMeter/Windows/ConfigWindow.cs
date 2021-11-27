using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using LMeter.Config;
using LMeter.Helpers;
using LMeter.Meter;

namespace LMeter.Windows
{
    public class ConfigWindow : Window
    {
        private const float NavBarHeight = 40;

        private bool _back = false;
        private bool _home = false;
        private string _name = string.Empty;

        private Vector2 WindowSize { get; set; }

        private Stack<IConfigurable> ConfigStack { get; init; }

        public ConfigWindow(string id, Vector2 position, Vector2 size) : base(id)
        {
            this.Flags =
                ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoCollapse |
                ImGuiWindowFlags.NoScrollWithMouse |
                ImGuiWindowFlags.NoSavedSettings;

            this.WindowSize = size;
            this.Position = position - size / 2;
            this.PositionCondition = ImGuiCond.Appearing;
            this.SizeConstraints = new WindowSizeConstraints()
            {
                MinimumSize = new Vector2(size.X, 160),
                MaximumSize = ImGui.GetMainViewport().Size
            };

            this.ConfigStack = new Stack<IConfigurable>();
        }

        public void PushConfig(IConfigurable configItem)
        {
            this.ConfigStack.Push(configItem);
            this._name = configItem.Name;
            this.IsOpen = true;
        }

        public override void PreDraw()
        {
            if (this.ConfigStack.Any())
            {
                this.WindowName = this.GetWindowTitle();
                ImGui.SetNextWindowSize(this.WindowSize);
            }
        }        

        private string GetWindowTitle()
        {
            string title = string.Empty;
            title = string.Join("  >  ", this.ConfigStack.Reverse().Select(c => c.Name));
            return title;
        }

        public override void Draw()
        {
            if (!this.ConfigStack.Any())
            {
                this.IsOpen = false;
                return;
            }

            IConfigurable configItem = this.ConfigStack.Peek();
            Vector2 spacing = ImGui.GetStyle().ItemSpacing;
            Vector2 size = this.WindowSize - spacing * 2;
            bool drawNavBar = this.ConfigStack.Count > 1;

            if (drawNavBar)
            {
                size -= new Vector2(0, NavBarHeight + spacing.Y);
            }

            if (ImGui.BeginTabBar($"##{this.WindowName}"))
            {
                foreach (IConfigPage page in configItem.GetConfigPages())
                {
                    if (ImGui.BeginTabItem($"{page.Name}##{this.WindowName}"))
                    {
                        page.DrawConfig(size.AddY(-ImGui.GetCursorPosY()), spacing.X, spacing.Y);
                        ImGui.EndTabItem();
                    }
                }

                ImGui.EndTabBar();
            }

            if (drawNavBar)
            {
                this.DrawNavBar(size, spacing.X);
            }

            this.Position = ImGui.GetWindowPos();
            this.WindowSize = ImGui.GetWindowSize();
        }
                
        private void DrawNavBar(Vector2 size, float padX)
        {
            Vector2 buttonsize = new Vector2(40, 0);
            float textInputWidth = 150;

            if (ImGui.BeginChild($"##{this.WindowName}_NavBar", new Vector2(size.X, NavBarHeight), true))
            {
                DrawHelpers.DrawButton(string.Empty, FontAwesomeIcon.LongArrowAltLeft, () => _back = true, "Back", buttonsize);
                ImGui.SameLine();

                if (this.ConfigStack.Count > 2)
                {
                    DrawHelpers.DrawButton(string.Empty, FontAwesomeIcon.Home, () => _home = true, "Home", buttonsize);
                    ImGui.SameLine();
                }
                else
                {
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 40 + padX);
                }

                // calculate empty horizontal space based on size of buttons and text box
                float offset = size.X - buttonsize.X * 3 - textInputWidth - padX * 5;

                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);

                ImGui.PushItemWidth(textInputWidth);
                if (ImGui.InputText("##Input", ref _name, 64, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    Rename(_name);
                }
                
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Rename");
                }

                ImGui.PopItemWidth();
                ImGui.SameLine();

                DrawHelpers.DrawButton(string.Empty, FontAwesomeIcon.Upload, () => Export(), "Export", buttonsize);
                ImGui.SameLine();

                ImGui.EndChild();
            }
        }

        private void Export()
        {
            if (this.ConfigStack.Any() &&
                this.ConfigStack.Peek() is MeterWindow meter)
            {
                ConfigHelpers.ExportToClipboard(meter);
            }
        }

        private void Rename(string name)
        {
            if (this.ConfigStack.Any())
            {
                this.ConfigStack.Peek().Name = name;
            }
        }

        public override void PostDraw()
        {
            if (this._home)
            {
                while (this.ConfigStack.Count > 1)
                {
                    this.ConfigStack.Pop();
                }
            }
            else if (this._back)
            {
                this.ConfigStack.Pop();
            }

            if ((this._home || this._back) && this.ConfigStack.Count > 1)
            {
                this._name = this.ConfigStack.Peek().Name;
            }

            this._home = false;
            this._back = false;
        }

        public override void OnClose()
        {
            ConfigHelpers.SaveConfig();
            this.ConfigStack.Clear();
        }
    }
}