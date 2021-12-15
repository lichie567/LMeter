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
        private readonly Vector2 _origin = ImGui.GetMainViewport().Size / 2f;
        private readonly Vector2 _configSize = new Vector2(550, 550);

        private ClientState _clientState;
        private DalamudPluginInterface _pluginInterface;
        private CommandManager _commandManager;
        private WindowSystem _windowSystem;
        private ConfigWindow _configRoot;
        private LMeterConfig _config;

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
            this._clientState = clientState;
            this._commandManager = commandManager;
            this._pluginInterface = pluginInterface;
            this._config = config;

            this._configRoot = new ConfigWindow("ConfigRoot", _origin, _configSize);
            this._windowSystem = new WindowSystem("LMeter");
            this._windowSystem.AddWindow(this._configRoot);

            this._commandManager.AddHandler(
                "/lm",
                new CommandInfo(PluginCommand)
                {
                    HelpMessage = "Opens the LMeter configuration window.\n"
                                + "/lm end → Ends current ACT Encounter.\n"
                                + "/lm clear → Clears all ACT encounter log data.\n"
                                + "/lm ct <number> → Toggles clickthrough status for the given profile.\n"
                                + "/lm toggle <number> → Toggles visibility for the given profile.",
                    ShowInHelp = true
                }
            );

            this._clientState.Logout += OnLogout;
            this._pluginInterface.UiBuilder.OpenConfigUi += OpenConfigUi;
            this._pluginInterface.UiBuilder.Draw += Draw;
        }

        private void Draw()
        {
            if (this._clientState.LocalPlayer == null || CharacterState.IsCharacterBusy())
            {
                return;
            }

            this._windowSystem.Draw();

            this._config.ACTConfig.TryReconnect();
            this._config.ACTConfig.TryEndEncounter();

            ImGuiHelpers.ForceNextWindowMainViewport();
            ImGui.SetNextWindowPos(Vector2.Zero);
            ImGui.SetNextWindowSize(ImGui.GetMainViewport().Size);
            if (ImGui.Begin("LMeter_Root", this._mainWindowFlags))
            {
                foreach (var meter in this._config.MeterList.Meters)
                {
                    meter.Draw(_origin);
                }
            }

            ImGui.End();
        }

        public void Clear(bool clearAct = false)
        {
            ACTClient.Clear(clearAct);
            foreach (var meter in this._config.MeterList.Meters)
            {
                meter.Clear();
            }
        }

        public void Edit(IConfigurable configItem)
        {
            this._configRoot.PushConfig(configItem);
        }

        private void OpenConfigUi()
        {
            if (!this._configRoot.IsOpen)
            {
                this._configRoot.PushConfig(this._config);
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
                    this.Clear(this._config.ACTConfig.ClearACT);
                    break;
                case { } argument when argument.StartsWith("toggle"):
                    this._config.MeterList.ToggleMeter(GetIntArg(argument) - 1);
                    break;
                case { } argument when argument.StartsWith("ct"):
                    this._config.MeterList.ToggleClickThrough(GetIntArg(argument) - 1);
                    break;
                default:
                    this.ToggleWindow();
                    break;
            }
        }

        private static int GetIntArg(string argument)
        {
            string[] args1 = argument.Split(" ");
            return args1.Length > 1 && int.TryParse(args1[1], out int num) ? num : 0;
        }

        private void ToggleWindow()
        {
            if (this._configRoot.IsOpen)
            {
                this._configRoot.IsOpen = false;
            }
            else
            {
                this._configRoot.PushConfig(this._config);
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
                this._pluginInterface.UiBuilder.Draw -= Draw;
                this._pluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUi;
                this._clientState.Logout -= OnLogout;
                this._commandManager.RemoveHandler("/lm");
                this._windowSystem.RemoveAllWindows();
            }
        }
    }
}
