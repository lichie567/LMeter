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
        private Vector2 _windowSize;
        private readonly Stack<IConfigurable> _configStack;

        public ConfigWindow(string id, Vector2 position, Vector2 size) : base(id)
        {
            this.Flags =
                ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoCollapse |
                ImGuiWindowFlags.NoScrollWithMouse |
                ImGuiWindowFlags.NoSavedSettings;

            this.Position = position - size / 2;
            this.PositionCondition = ImGuiCond.Appearing;
            this.SizeConstraints = new WindowSizeConstraints()
            {
                MinimumSize = new Vector2(size.X, 160),
                MaximumSize = ImGui.GetMainViewport().Size
            };

            _windowSize = size;
            _configStack = new Stack<IConfigurable>();
        }

        public void PushConfig(IConfigurable configItem)
        {
            _configStack.Push(configItem);
            _name = configItem.Name;
            this.IsOpen = true;
        }

        public override void PreDraw()
        {
            if (_configStack.Count != 0)
            {
                this.WindowName = string.Join("  >  ", _configStack.Reverse().Select(c => c.Name));
                ImGui.SetNextWindowSize(_windowSize);
            }
        }

        public override void Draw()
        {
            if (_configStack.Count == 0)
            {
                this.IsOpen = false;
                return;
            }

            IConfigurable configItem = _configStack.Peek();
            Vector2 spacing = ImGui.GetStyle().ItemSpacing;
            Vector2 size = _windowSize - spacing * 2;
            bool drawNavBar = _configStack.Count > 1;

            if (drawNavBar)
            {
                size -= new Vector2(0, NavBarHeight + spacing.Y);
            }

            IConfigPage? openPage = null;
            if (ImGui.BeginTabBar($"##{this.WindowName}"))
            {
                foreach (IConfigPage page in configItem.GetConfigPages())
                {
                    page.Active = ImGui.BeginTabItem($"{page.Name}##{this.WindowName}");
                    if (page.Active)
                    {
                        openPage = page;
                        page.DrawConfig(size.AddY(-ImGui.GetCursorPosY()), spacing.X, spacing.Y);
                        ImGui.EndTabItem();
                    }
                }

                ImGui.EndTabBar();
            }

            if (drawNavBar)
            {
                this.DrawNavBar(openPage, size, spacing.X);
            }

            this.Position = ImGui.GetWindowPos();
            _windowSize = ImGui.GetWindowSize();
        }

        private void DrawNavBar(IConfigPage? openPage, Vector2 size, float padX)
        {
            Vector2 buttonsize = new(40, 0);
            float textInputWidth = 150;

            if (ImGui.BeginChild($"##{this.WindowName}_NavBar", new Vector2(size.X, NavBarHeight), true))
            {
                DrawHelpers.DrawButton(string.Empty, FontAwesomeIcon.LongArrowAltLeft, () => _back = true, "Back", buttonsize);

                ImGui.SameLine();
                if (_configStack.Count > 2)
                {
                    DrawHelpers.DrawButton(string.Empty, FontAwesomeIcon.Home, () => _home = true, "Home", buttonsize);
                    ImGui.SameLine();
                }
                else
                {
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 40 + padX);
                }

                // calculate empty horizontal space based on size of buttons and text box
                float offset = size.X - buttonsize.X * 5 - textInputWidth - padX * 7;
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);

                DrawHelpers.DrawButton(string.Empty, FontAwesomeIcon.UndoAlt, () => Reset(openPage), $"Reset {openPage?.Name} to Defaults", buttonsize);

                ImGui.SameLine();
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
                DrawHelpers.DrawButton(string.Empty, FontAwesomeIcon.Upload, () => Export(openPage), $"Export {openPage?.Name}", buttonsize);

                ImGui.SameLine();
                DrawHelpers.DrawButton(string.Empty, FontAwesomeIcon.Download, () => Import(), $"Import {openPage?.Name}", buttonsize);
            }

            ImGui.EndChild();
        }

        private void Reset(IConfigPage? openPage)
        {
            if (openPage is not null)
            {
                _configStack.Peek().ImportPage(openPage.GetDefault());
            }
        }

        private void Export(IConfigPage? openPage)
        {
            if (openPage is not null)
            {
                ConfigHelpers.ExportToClipboard<IConfigPage>(openPage);
            }
        }

        private void Import()
        {
            string importString = ImGui.GetClipboardText();
            IConfigPage? page = ConfigHelpers.GetFromImportString<IConfigPage>(importString);

            if (page is not null)
            {
                _configStack.Peek().ImportPage(page);
            }
        }

        private void Rename(string name)
        {
            if (_configStack.Count != 0)
            {
                _configStack.Peek().Name = name;
            }
        }

        public override void PostDraw()
        {
            if (_home)
            {
                while (_configStack.Count > 1)
                {
                    _configStack.Pop();
                }
            }
            else if (_back)
            {
                _configStack.Pop();
            }

            if ((_home || _back) && _configStack.Count > 1)
            {
                _name = _configStack.Peek().Name;
            }

            _home = false;
            _back = false;
        }

        public override void OnClose()
        {
            ConfigHelpers.SaveConfig();
            _configStack.Clear();

            LMeterConfig config = Singletons.Get<LMeterConfig>();
            foreach (MeterWindow meter in config.MeterList.Meters)
            {
                meter.GeneralConfig.Preview = false;
            }
        }
    }
}