using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using LMeter.Config;
using LMeter.Helpers;
using LMeter.Meter;

namespace LMeter.Windows
{
    public class ConfigWindow : Window
    {
        private const float NAVBAR_HEIGHT = 40;

        private bool m_firstOpen = true;
        private bool m_back = false;
        private bool m_home = false;
        private string m_name = string.Empty;
        private Vector2 m_windowSize;
        private readonly Stack<IConfigurable> m_configStack;

        public ConfigWindow(string id, Vector2 size)
            : base(id)
        {
            this.Flags =
                ImGuiWindowFlags.NoScrollbar
                | ImGuiWindowFlags.NoCollapse
                | ImGuiWindowFlags.NoScrollWithMouse
                | ImGuiWindowFlags.NoSavedSettings;

            m_windowSize = size;
            m_configStack = new Stack<IConfigurable>();
        }

        public void PushConfig(IConfigurable configItem)
        {
            m_configStack.Push(configItem);
            m_name = configItem.Name;
            this.IsOpen = true;
        }

        public override void PreDraw()
        {
            if (m_configStack.Count != 0)
            {
                if (m_firstOpen)
                {
                    Vector2 viewPort = ImGui.GetMainViewport().Size;
                    this.PositionCondition = ImGuiCond.Appearing;
                    this.SizeConstraints = new WindowSizeConstraints()
                    {
                        MinimumSize = new Vector2(m_windowSize.X, 160),
                        MaximumSize = ImGui.GetMainViewport().Size,
                    };

                    this.Position = viewPort / 2f - (m_windowSize / 2);
                    m_firstOpen = false;
                }

                this.WindowName = string.Join("  >  ", m_configStack.Reverse().Select(c => c.Name));
                ImGui.SetNextWindowSize(m_windowSize);
            }
        }

        public override void Draw()
        {
            if (m_configStack.Count == 0)
            {
                this.IsOpen = false;
                return;
            }

            IConfigurable configItem = m_configStack.Peek();
            Vector2 spacing = ImGui.GetStyle().ItemSpacing;
            Vector2 size = m_windowSize - spacing * 2;
            bool drawNavBar = m_configStack.Count > 1;

            if (drawNavBar)
            {
                size -= new Vector2(0, NAVBAR_HEIGHT + spacing.Y);
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
            m_windowSize = ImGui.GetWindowSize();
        }

        private void DrawNavBar(IConfigPage? openPage, Vector2 size, float padX)
        {
            Vector2 buttonsize = new(40, 0);
            float textInputWidth = 150;

            if (ImGui.BeginChild($"##{this.WindowName}_NavBar", new Vector2(size.X, NAVBAR_HEIGHT), true))
            {
                DrawHelpers.DrawButton(
                    string.Empty,
                    FontAwesomeIcon.LongArrowAltLeft,
                    () => m_back = true,
                    "Back",
                    buttonsize
                );

                ImGui.SameLine();
                if (m_configStack.Count > 2)
                {
                    DrawHelpers.DrawButton(string.Empty, FontAwesomeIcon.Home, () => m_home = true, "Home", buttonsize);
                    ImGui.SameLine();
                }
                else
                {
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 40 + padX);
                }

                // calculate empty horizontal space based on size of buttons and text box
                float offset = size.X - buttonsize.X * 5 - textInputWidth - padX * 7;
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);

                DrawHelpers.DrawButton(
                    string.Empty,
                    FontAwesomeIcon.UndoAlt,
                    () => Reset(openPage),
                    $"Reset {openPage?.Name} to Defaults",
                    buttonsize
                );

                ImGui.SameLine();
                ImGui.PushItemWidth(textInputWidth);
                if (ImGui.InputText("##Input", ref m_name, 64, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    Rename(m_name);
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Rename");
                }

                ImGui.PopItemWidth();

                ImGui.SameLine();
                DrawHelpers.DrawButton(
                    string.Empty,
                    FontAwesomeIcon.Upload,
                    () => Export(openPage),
                    $"Export {openPage?.Name}",
                    buttonsize
                );

                ImGui.SameLine();
                DrawHelpers.DrawButton(
                    string.Empty,
                    FontAwesomeIcon.Download,
                    () => Import(),
                    $"Import {openPage?.Name}",
                    buttonsize
                );
            }

            ImGui.EndChild();
        }

        private void Reset(IConfigPage? openPage)
        {
            if (openPage is not null)
            {
                m_configStack.Peek().ImportPage(openPage.GetDefault());
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
                m_configStack.Peek().ImportPage(page);
            }
        }

        private void Rename(string name)
        {
            if (m_configStack.Count != 0)
            {
                m_configStack.Peek().Name = name;
            }
        }

        public override void PostDraw()
        {
            if (m_home)
            {
                while (m_configStack.Count > 1)
                {
                    m_configStack.Pop();
                }
            }
            else if (m_back)
            {
                m_configStack.Pop();
            }

            if ((m_home || m_back) && m_configStack.Count > 1)
            {
                m_name = m_configStack.Peek().Name;
            }

            m_home = false;
            m_back = false;
        }

        public override void OnClose()
        {
            ConfigHelpers.SaveConfig();
            m_configStack.Clear();

            LMeterConfig config = Singletons.Get<LMeterConfig>();
            foreach (MeterWindow meter in config.MeterList.Meters)
            {
                meter.GeneralConfig.Preview = false;
            }
        }
    }
}
