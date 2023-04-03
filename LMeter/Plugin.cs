using System;
using System.IO;
using System.Reflection;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.JobGauge;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Interface;
using Dalamud.Logging;
using Dalamud.Plugin;
using ImGuiScene;
using LMeter.ACT;
using LMeter.Config;
using LMeter.Helpers;
using LMeter.Meter;
using SigScanner = Dalamud.Game.SigScanner;

namespace LMeter
{
    public class Plugin : IDalamudPlugin
    {
        public const string ConfigFileName = "LMeter.json";

        public static string Version { get; private set; } = "0.1.8.0";

        public static string ConfigFileDir { get; private set; } = "";

        public static string ConfigFilePath { get; private set; } = "";
        
        public static TextureWrap? IconTexture { get; private set; } = null;

        public static string Changelog { get; private set; } = string.Empty;

        public string Name => "LMeter";

        public Plugin(
            ClientState clientState,
            CommandManager commandManager,
            Condition condition,
            DalamudPluginInterface pluginInterface,
            DataManager dataManager,
            Framework framework,
            GameGui gameGui,
            JobGauges jobGauges,
            ObjectTable objectTable,
            PartyList partyList,
            SigScanner sigScanner,
            TargetManager targetManager,
            ChatGui chatGui
        )
        {
            Plugin.Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? Plugin.Version;
            Plugin.ConfigFileDir = pluginInterface.GetPluginConfigDirectory();
            Plugin.ConfigFilePath = Path.Combine(pluginInterface.GetPluginConfigDirectory(), Plugin.ConfigFileName);

            // Register Dalamud APIs
            Singletons.Register(clientState);
            Singletons.Register(commandManager);
            Singletons.Register(condition);
            Singletons.Register(pluginInterface);
            Singletons.Register(dataManager);
            Singletons.Register(framework);
            Singletons.Register(gameGui);
            Singletons.Register(jobGauges);
            Singletons.Register(objectTable);
            Singletons.Register(partyList);
            Singletons.Register(sigScanner);
            Singletons.Register(targetManager);
            Singletons.Register(chatGui);
            Singletons.Register(pluginInterface.UiBuilder);

            // Init TexturesCache
            Singletons.Register(new TexturesCache(pluginInterface));

            // Load Icon Texure
            Plugin.IconTexture = LoadIconTexture(pluginInterface.UiBuilder);

            // Load changelog
            Plugin.Changelog = LoadChangelog();

            // Load config
            FontsManager.CopyPluginFontsToUserPath();
            LMeterConfig config = ConfigHelpers.LoadConfig(Plugin.ConfigFilePath);
            config.FontConfig.RefreshFontList();
            Singletons.Register(config);

            // Initialize Fonts
            Singletons.Register(new FontsManager(pluginInterface.UiBuilder, config.FontConfig.Fonts.Values));

            // Connect to ACT
            IACTClient actClient = IACTClient.GetNewClient(); // Singleton Registry is done internally here
            actClient.Start();

            // Create profile on first load
            if (config.FirstLoad && config.MeterList.Meters.Count == 0)
            {
                config.MeterList.Meters.Add(MeterWindow.GetDefaultMeter("Profile 1"));
            }
            config.FirstLoad = false;

            // Start the plugin
            Singletons.Register(new PluginManager(clientState, commandManager, pluginInterface, config));
        }
        
        private static TextureWrap? LoadIconTexture(UiBuilder uiBuilder)
        {
            string? pluginPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(pluginPath))
            {
                return null;
            }

            string iconPath = Path.Combine(pluginPath, "Media", "Images", "icon_small.png");
            if (!File.Exists(iconPath))
            {
                return null;
            }

            TextureWrap? texture = null;
            try
            {
                texture = uiBuilder.LoadImage(iconPath);
            }
            catch (Exception ex)
            {
                PluginLog.Warning($"Failed to load LMeter Icon {ex.ToString()}");
            }

            return texture;
        }

        private static string LoadChangelog()
        {
            string? pluginPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            if (string.IsNullOrEmpty(pluginPath))
            {
                return string.Empty;
            }

            string changelogPath = Path.Combine(pluginPath, "changelog.md");

            if (File.Exists(changelogPath))
            {
                try
                {
                    string changelog = File.ReadAllText(changelogPath);
                    return changelog.Replace("# ", string.Empty);
                }
                catch (Exception ex)
                {
                    PluginLog.Warning($"Error loading changelog: {ex.ToString()}");
                }
            }

            return string.Empty;
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
                Singletons.Dispose();
            }
        }
    }
}
