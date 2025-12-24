using System;
using System.IO;
using System.Reflection;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using LMeter.Act;
using LMeter.Config;
using LMeter.Helpers;
using LMeter.Meter;

namespace LMeter
{
    public class Plugin : IDalamudPlugin
    {
        public const string ConfigFileName = "LMeter.json";

        public static string Version { get; private set; } = "0.4.3.3";
        public static string ConfigFileDir { get; private set; } = "";
        public static string ConfigFilePath { get; private set; } = "";
        public static string AssemblyFileDir { get; private set; } = "";
        public static IDalamudTextureWrap? IconTexture { get; private set; } = null;
        public static string Changelog { get; private set; } = string.Empty;
        public static string Name => "LMeter";

        public Plugin(
            IClientState clientState,
            IPlayerState playerState,
            ICommandManager commandManager,
            ICondition condition,
            IDalamudPluginInterface pluginInterface,
            IDataManager dataManager,
            IFramework framework,
            IGameGui gameGui,
            IJobGauges jobGauges,
            IObjectTable objectTable,
            IPartyList partyList,
            ITargetManager targetManager,
            IChatGui chatGui,
            IPluginLog logger,
            ITextureProvider textureProvider,
            ITextureSubstitutionProvider textureSubstitutionProvider,
            INotificationManager notificationManager
        )
        {
            Plugin.Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? Plugin.Version;
            Plugin.ConfigFileDir = pluginInterface.GetPluginConfigDirectory();
            Plugin.ConfigFilePath = Path.Combine(pluginInterface.GetPluginConfigDirectory(), Plugin.ConfigFileName);
            Plugin.AssemblyFileDir = pluginInterface.AssemblyLocation.DirectoryName ?? "";

            // Register Dalamud APIs
            Singletons.Register(clientState);
            Singletons.Register(playerState);
            Singletons.Register(commandManager);
            Singletons.Register(condition);
            Singletons.Register(pluginInterface);
            Singletons.Register(dataManager);
            Singletons.Register(framework);
            Singletons.Register(gameGui);
            Singletons.Register(jobGauges);
            Singletons.Register(objectTable);
            Singletons.Register(partyList);
            Singletons.Register(targetManager);
            Singletons.Register(chatGui);
            Singletons.Register(pluginInterface.UiBuilder);
            Singletons.Register(logger);
            Singletons.Register(textureProvider);
            Singletons.Register(textureSubstitutionProvider);
            Singletons.Register(notificationManager);

            // Add ClipRect helper
            Singletons.Register(new ClipRectsHelper());

            // Init TexturesCache
            Singletons.Register(new TextureCache());

            // Load changelog
            Plugin.Changelog = LoadChangelog();

            // Load config
            FontsManager.CopyPluginFontsToUserPath();
            LMeterConfig config = ConfigHelpers.LoadConfig(Plugin.ConfigFilePath);

            // Refresh fonts
            config.FontConfig.RefreshFontList();

            // Register config
            Singletons.Register(config);

            // Initialize Fonts
            Singletons.Register(new FontsManager(pluginInterface.UiBuilder, config.FontConfig.Fonts.Values));

            // Initialize ACT Client
            LogClient actClient = config.ActConfig.ClientType switch
            {
                1 => new IpcClient(config.ActConfig),
                _ => new WebSocketClient(config.ActConfig),
            };

            actClient.Start();
            Singletons.Register(actClient);

            // Create profile on first load
            if (config.FirstLoad && config.MeterList.Meters.Count == 0)
            {
                config.MeterList.Meters.Add(MeterWindow.GetDefaultMeter(MeterDataType.Damage, "Dps Meter"));

                MeterWindow hps = MeterWindow.GetDefaultMeter(MeterDataType.Healing, "Hps Meter");
                hps.Enabled = false;
                config.MeterList.Meters.Add(hps);
            }

            config.FirstLoad = false;

            // Start the plugin
            Singletons.Register(new PluginManager(clientState, commandManager, pluginInterface, config));
        }

        private static IDalamudTextureWrap? LoadIconTexture(ITextureProvider textureProvider)
        {
            if (string.IsNullOrEmpty(AssemblyFileDir))
            {
                return null;
            }

            string iconPath = Path.Combine(AssemblyFileDir, "Media", "Images", "icon_small.png");
            if (!File.Exists(iconPath))
            {
                return null;
            }

            IDalamudTextureWrap? texture = null;
            try
            {
                texture = textureProvider.GetFromFile(iconPath).GetWrapOrDefault();
            }
            catch (Exception ex)
            {
                Singletons.Get<IPluginLog>().Warning($"Failed to load LMeter Icon {ex.ToString()}");
            }

            return texture;
        }

        private static string LoadChangelog()
        {
            if (string.IsNullOrEmpty(AssemblyFileDir))
            {
                return string.Empty;
            }

            string changelogPath = Path.Combine(AssemblyFileDir, "changelog.md");

            if (File.Exists(changelogPath))
            {
                try
                {
                    string changelog = File.ReadAllText(changelogPath);
                    return changelog.Replace("# ", string.Empty);
                }
                catch (Exception ex)
                {
                    Singletons.Get<IPluginLog>().Warning($"Error loading changelog: {ex}");
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
