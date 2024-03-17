using System;
using System.Numerics;
using Dalamud.Game.Command;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ImGuiNET;
using LMeter.Act;
using LMeter.Config;
using LMeter.Helpers;
using LMeter.Meter;
using LMeter.Windows;

namespace LMeter
{
    public class PluginManager : IPluginDisposable
    {
        private readonly Vector2 _configSize = new Vector2(550, 550);

        private IClientState _clientState;
        private DalamudPluginInterface _pluginInterface;
        private ICommandManager _commandManager;
        private WindowSystem _windowSystem;
        private ConfigWindow _configRoot;
        private LMeterConfig _config;
        private Vector2 _origin;

        private readonly ImGuiWindowFlags _mainWindowFlags =
            ImGuiWindowFlags.NoTitleBar |
            ImGuiWindowFlags.NoScrollbar |
            ImGuiWindowFlags.AlwaysAutoResize |
            ImGuiWindowFlags.NoBackground |
            ImGuiWindowFlags.NoInputs |
            ImGuiWindowFlags.NoBringToFrontOnFocus |
            ImGuiWindowFlags.NoSavedSettings;

        public PluginManager(
            IClientState clientState,
            ICommandManager commandManager,
            DalamudPluginInterface pluginInterface,
            LMeterConfig config)
        {
            _clientState = clientState;
            _commandManager = commandManager;
            _pluginInterface = pluginInterface;
            _config = config;

            _configRoot = new ConfigWindow("ConfigRoot", _origin, _configSize);
            _windowSystem = new WindowSystem("LMeter");
            _windowSystem.AddWindow(_configRoot);

            _commandManager.AddHandler(
                "/lm",
                new CommandInfo(PluginCommand)
                {
                    HelpMessage = "Opens the LMeter configuration window.\n"
                                + "/lm end → Ends current Act Encounter.\n"
                                + "/lm clear → Clears all Act encounter log data.\n"
                                + "/lm ct <number> → Toggles clickthrough status for the given profile.\n"
                                + "/lm toggle <number> [on|off] → Toggles visibility for the given profile.",
                    ShowInHelp = true
                }
            );

            _clientState.Logout += OnLogout;
            _pluginInterface.UiBuilder.OpenConfigUi += OpenConfigUi;
            _pluginInterface.UiBuilder.Draw += Draw;
        }

        private void Draw()
        {
            if (_clientState.LocalPlayer == null || CharacterState.IsCharacterBusy())
            {
                return;
            }
            
            _origin = ImGui.GetMainViewport().Size / 2f;
            _windowSystem.Draw();
            _config.ActConfig.TryReconnect();
            _config.ActConfig.TryEndEncounter();

            ImGuiHelpers.ForceNextWindowMainViewport();
            ImGui.SetNextWindowPos(Vector2.Zero);
            ImGui.SetNextWindowSize(ImGui.GetMainViewport().Size);
            if (ImGui.Begin("LMeter_Root", _mainWindowFlags))
            {
                Singletons.Get<ClipRectsHelper>().Update();
                foreach (var meter in _config.MeterList.Meters)
                {
                    meter.Draw(_origin);
                }
            }

            ImGui.End();
        }

        public void Clear()
        {
            Singletons.Get<LogClient>().Clear();
            foreach (var meter in _config.MeterList.Meters)
            {
                meter.Clear();
            }
        }

        public void Edit(IConfigurable configItem)
        {
            _configRoot.PushConfig(configItem);
        }

        public void ConfigureMeter(MeterWindow meter)
        {
            if (!_configRoot.IsOpen)
            {
                this.OpenConfigUi();
                this.Edit(meter);
            }
        }

        private void OpenConfigUi()
        {
            if (!_configRoot.IsOpen)
            {
                _configRoot.PushConfig(_config);
            }
        }

        private void OnLogout()
        {
            ConfigHelpers.SaveConfig();
        }

        private void PluginCommand(string command, string arguments)
        {
            switch (arguments)
            {
                case "end":
                    LogClient.EndEncounter();
                    break;
                case "clear":
                    this.Clear();
                    break;
                case { } argument when argument.StartsWith("toggle"):
                    _config.MeterList.ToggleMeter(GetIntArg(argument) - 1, GetBoolArg(argument, 2));
                    break;
                case { } argument when argument.StartsWith("ct"):
                    _config.MeterList.ToggleClickThrough(GetIntArg(argument) - 1);
                    break;
                default:
                    this.ToggleWindow();
                    break;
            }
        }

        private static int GetIntArg(string argument)
        {
            string[] args = argument.Split(" ");
            return args.Length > 1 && int.TryParse(args[1], out int num) ? num : 0;
        }

        private static bool? GetBoolArg(string argument, int index = 1)
        {
            string[] args = argument.Split(" ");
            if (args.Length > index)
            {
                string arg = args[index].ToLower();
                return arg.Equals("on") ? true : (arg.Equals("off") ? false : null);
            }

            return null;
        }

        private void ToggleWindow()
        {
            if (_configRoot.IsOpen)
            {
                _configRoot.IsOpen = false;
            }
            else
            {
                _configRoot.PushConfig(_config);
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
                _pluginInterface.UiBuilder.Draw -= Draw;
                _pluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUi;
                _clientState.Logout -= OnLogout;
                _commandManager.RemoveHandler("/lm");
                _windowSystem.RemoveAllWindows();
            }
        }
    }
}
