using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Command;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using LMeter.Act;
using LMeter.Config;
using LMeter.Helpers;
using LMeter.Meter;
using LMeter.Windows;

namespace LMeter
{
    public class PluginManager : IPluginDisposable
    {
        private readonly Vector2 m_configSize = new(650, 750);
        private readonly IClientState m_clientState;
        private readonly IDalamudPluginInterface m_pluginInterface;
        private readonly ICommandManager m_commandManager;
        private readonly WindowSystem m_windowSystem;
        private readonly ConfigWindow m_configRoot;
        private readonly LMeterConfig m_config;
        private DateTime? m_lastCombatTime { get; set; }
        private DateTime? m_lastConnectionAttempt { get; set; }
        private Vector2 m_origin;

        private readonly ImGuiWindowFlags m_mainWindowFlags =
            ImGuiWindowFlags.NoTitleBar
            | ImGuiWindowFlags.NoScrollbar
            | ImGuiWindowFlags.AlwaysAutoResize
            | ImGuiWindowFlags.NoBackground
            | ImGuiWindowFlags.NoInputs
            | ImGuiWindowFlags.NoBringToFrontOnFocus
            | ImGuiWindowFlags.NoSavedSettings
            | ImGuiWindowFlags.NoFocusOnAppearing;

        public PluginManager(
            IClientState clientState,
            ICommandManager commandManager,
            IDalamudPluginInterface pluginInterface,
            LMeterConfig config
        )
        {
            m_clientState = clientState;
            m_commandManager = commandManager;
            m_pluginInterface = pluginInterface;
            m_config = config;

            m_origin = ImGui.GetMainViewport().Size / 2f;
            m_configRoot = new ConfigWindow("LMeter_ConfigRoot", m_configSize);
            m_windowSystem = new WindowSystem("LMeter");
            m_windowSystem.AddWindow(m_configRoot);

            m_commandManager.AddHandler(
                "/lm",
                new CommandInfo(PluginCommand)
                {
                    HelpMessage =
                        "Opens the LMeter configuration window.\n"
                        + "/lm end → Ends current Act Encounter.\n"
                        + "/lm clear → Clears all Act encounter log data.\n"
                        + "/lm ct <number> → Toggles clickthrough status for the given profile.\n"
                        + "/lm toggle <number> [on|off] → Toggles visibility for the given profile.",
                    ShowInHelp = true,
                }
            );

            m_clientState.Logout += OnLogout;
            m_pluginInterface.UiBuilder.OpenConfigUi += OpenConfigUi;
            m_pluginInterface.UiBuilder.Draw += Draw;
        }

        private void Draw()
        {
            if (Singletons.Get<IObjectTable>().LocalPlayer is null || CharacterState.IsCharacterBusy())
            {
                return;
            }

            m_origin = ImGui.GetMainViewport().Size / 2f;
            m_windowSystem.Draw();
            this.TryConnect();
            this.TryEndEncounter();

            ImGuiHelpers.ForceNextWindowMainViewport();
            ImGui.SetNextWindowPos(Vector2.Zero);
            ImGui.SetNextWindowSize(ImGui.GetMainViewport().Size);
            if (ImGui.Begin("LMeter_Root", m_mainWindowFlags))
            {
                CharacterState.UpdateCurrentCharacter();
                Singletons.Get<ClipRectsHelper>().Update();
                foreach (MeterWindow meter in m_config.MeterList.Meters)
                {
                    meter.Draw(m_origin);
                }
            }

            ImGui.End();
        }

        public void Clear()
        {
            Singletons.Get<LogClient>().Clear();
            foreach (MeterWindow meter in m_config.MeterList.Meters)
            {
                meter.Clear();
            }
        }

        public void ChangeClientType(int clientType)
        {
            if (!Singletons.IsRegistered<LogClient>())
            {
                return;
            }

            LogClient oldClient = Singletons.Get<LogClient>();
            oldClient.Shutdown();

            LogClient newClient = clientType switch
            {
                1 => new IpcClient(m_config.ActConfig),
                _ => new WebSocketClient(m_config.ActConfig),
            };

            newClient.Start();
            Singletons.Update(newClient);
        }

        public void TryConnect()
        {
            ConnectionStatus status = Singletons.Get<LogClient>().Status;
            if (status == ConnectionStatus.NotConnected || status == ConnectionStatus.ConnectionFailed)
            {
                if (!m_lastConnectionAttempt.HasValue)
                {
                    m_lastConnectionAttempt = DateTime.UtcNow;
                    Singletons.Get<LogClient>().Start();
                }
                else if (
                    m_config.ActConfig.AutoReconnect
                    && m_lastConnectionAttempt
                        < DateTime.UtcNow - TimeSpan.FromSeconds(m_config.ActConfig.ReconnectDelay)
                )
                {
                    m_lastConnectionAttempt = DateTime.UtcNow;
                    Singletons.Get<LogClient>().Reset();
                }
            }
        }

        public void TryEndEncounter()
        {
            if (Singletons.Get<LogClient>().Status == ConnectionStatus.Connected)
            {
                if (m_config.ActConfig.AutoEnd && CharacterState.IsInCombat())
                {
                    m_lastCombatTime = DateTime.UtcNow;
                }
                else if (
                    m_lastCombatTime is not null
                    && m_lastCombatTime < DateTime.UtcNow - TimeSpan.FromSeconds(m_config.ActConfig.AutoEndDelay)
                )
                {
                    Singletons.Get<LogClient>().EndEncounter();
                    m_lastCombatTime = null;
                }
            }
        }

        public void Edit(IConfigurable configItem)
        {
            m_configRoot.PushConfig(configItem);
        }

        public void ConfigureMeter(MeterWindow meter)
        {
            if (!m_configRoot.IsOpen)
            {
                this.OpenConfigUi();
                this.Edit(meter);
            }
        }

        private void OpenConfigUi()
        {
            if (!m_configRoot.IsOpen)
            {
                m_configRoot.PushConfig(m_config);
            }
        }

        private void OnLogout(int _, int __)
        {
            ConfigHelpers.SaveConfig();
        }

        private void PluginCommand(string command, string arguments)
        {
            string[] argArray = arguments.Split(" ");
            switch (argArray)
            {
                case { } args when args[0].Equals("end"):
                    Singletons.Get<LogClient>().EndEncounter();
                    break;
                case { } args when args[0].Equals("clear"):
                    this.Clear();
                    break;
                case { } args when args[0].Equals("toggle"):
                    m_config.MeterList.ToggleMeter(args.Length > 1 ? GetIntArg(args[1]) - 1 : null);
                    break;
                case { } args when args[0].Equals("ct"):
                    m_config.MeterList.ToggleClickThrough(args.Length > 1 ? GetIntArg(args[1]) - 1 : null);
                    break;
                default:
                    this.ToggleWindow();
                    break;
            }
        }

        private static int GetIntArg(string argument)
        {
            return !string.IsNullOrEmpty(argument) && int.TryParse(argument, out int num) ? num : 0;
        }

        private void ToggleWindow()
        {
            if (m_configRoot.IsOpen)
            {
                m_configRoot.IsOpen = false;
            }
            else
            {
                m_configRoot.PushConfig(m_config);
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Don't modify order
                m_pluginInterface.UiBuilder.Draw -= Draw;
                m_pluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUi;
                m_clientState.Logout -= OnLogout;
                m_commandManager.RemoveHandler("/lm");
                m_windowSystem.RemoveAllWindows();
            }
        }
    }
}
