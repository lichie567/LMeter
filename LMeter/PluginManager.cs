using System;
using System.Numerics;
using Dalamud.Game.ClientState;
using Dalamud.Game.Command;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using ImGuiNET;
using LMeter.Config;
using LMeter.Helpers;
using LMeter.Windows;
using LMeter.ACT;

namespace LMeter
{
    public class PluginManager : ILMeterDisposable
    {
        private ClientState ClientState { get; init; }

        private DalamudPluginInterface PluginInterface { get; init; }

        private CommandManager CommandManager { get; init; }

        private WindowSystem WindowSystem { get; init; }

        private ConfigWindow ConfigRoot { get; init; }

        private LMeterConfig Config { get; init; }

        private readonly Vector2 _origin = ImGui.GetMainViewport().Size / 2f;

        private readonly Vector2 _configSize = new Vector2(550, 550);

        private readonly ImGuiWindowFlags _mainWindowFlags = 
            ImGuiWindowFlags.NoTitleBar |
            ImGuiWindowFlags.NoScrollbar |
            ImGuiWindowFlags.AlwaysAutoResize |
            ImGuiWindowFlags.NoBackground |
            ImGuiWindowFlags.NoInputs |
            ImGuiWindowFlags.NoBringToFrontOnFocus |
            ImGuiWindowFlags.NoSavedSettings;

        public PluginManager(
            ClientState clientState,
            CommandManager commandManager,
            DalamudPluginInterface pluginInterface,
            LMeterConfig config)
        {
            this.ClientState = clientState;
            this.CommandManager = commandManager;
            this.PluginInterface = pluginInterface;
            this.Config = config;

            this.ConfigRoot = new ConfigWindow("ConfigRoot", _origin, _configSize);

            this.WindowSystem = new WindowSystem("LMeter");
            this.WindowSystem.AddWindow(this.ConfigRoot);

            this.CommandManager.AddHandler(
                "/lm",
                new CommandInfo(PluginCommand)
                {
                    HelpMessage = "Opens the LMeter configuration window.\n"
                                + "/lm end → Ends current ACT Encounter.\n"
                                + "/lm clear → Clears all ACT encounter log data.\n"
                                + "/lm toggle <number> → Toggles visibility for the given profile.",
                    ShowInHelp = true
                }
            );

            this.ClientState.Logout += OnLogout;
            this.PluginInterface.UiBuilder.OpenConfigUi += OpenConfigUi;
            this.PluginInterface.UiBuilder.Draw += Draw;
        }

        private void Draw()
        {
            if (this.ClientState.LocalPlayer == null || CharacterState.IsCharacterBusy())
            {
                return;
            }

            this.WindowSystem.Draw();

            this.Config.ACTConfig.TryReconnect();
            this.Config.ACTConfig.TryEndEncounter();

            ImGuiHelpers.ForceNextWindowMainViewport();
            ImGui.SetNextWindowPos(Vector2.Zero);
            ImGui.SetNextWindowSize(ImGui.GetMainViewport().Size);
            if (ImGui.Begin("LMeter_Root", this._mainWindowFlags))
            {
                foreach (var meter in this.Config.MeterList.Meters)
                {
                    meter.Draw(_origin);
                }
            }

            ImGui.End();
        }

        public void Clear(bool clearAct = false)
        {
            ACTClient.Clear(clearAct);
            foreach (var meter in this.Config.MeterList.Meters)
            {
                meter.Clear();
            }
        }

        public void Edit(IConfigurable configItem)
        {
            this.ConfigRoot.PushConfig(configItem);
        }

        private void OpenConfigUi()
        {
            if (!this.ConfigRoot.IsOpen)
            {
                this.ConfigRoot.PushConfig(this.Config);
            }
        }

        private void OnLogout(object? sender, EventArgs? args)
        {
            ConfigHelpers.SaveConfig();
        }

        private void PluginCommand(string command, string arguments)
        {
            switch (arguments)
            {
                case "end":
                    ACTClient.EndEncounter();
                    break;
                case "clear":
                    this.Clear(this.Config.ACTConfig.ClearACT);
                    break;
                case { } argument when argument.StartsWith("toggle"):
                    string[] args = argument.Split(" ");
                    if (args.Length > 1 && int.TryParse(args[1], out int num))
                        this.ToggleMeter(num - 1);
                    break;
                default:
                    this.ToggleWindow();
                    break;
            }
        }

        private void ToggleMeter(int meterIndex)
        {
            this.Config.MeterList.ToggleMeter(meterIndex);
        }

        private void ToggleWindow()
        {
            if (this.ConfigRoot.IsOpen)
            {
                this.ConfigRoot.IsOpen = false;
            }
            else
            {
                this.ConfigRoot.PushConfig(this.Config);
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
                this.PluginInterface.UiBuilder.Draw -= Draw;
                this.PluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUi;
                this.ClientState.Logout -= OnLogout;
                this.CommandManager.RemoveHandler("/lm");
                this.WindowSystem.RemoveAllWindows();
            }
        }
    }
}
