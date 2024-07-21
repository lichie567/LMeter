using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Numerics;
using System.Text;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin.Services;
using ImGuiNET;
using LMeter.Act.DataStructures;
using LMeter.Config;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace LMeter.Helpers
{
    public static class ConfigHelpers
    {
        private static readonly JsonSerializerSettings _serializerSettings = new()
        {
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            TypeNameHandling = TypeNameHandling.Objects,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            SerializationBinder = new LMeterSerializationBinder()
        };

        public static void ExportToClipboard<T>(T toExport)
        {
            string? exportString = GetExportString(toExport);

            if (exportString is not null)
            {
                ImGui.SetClipboardText(exportString);
                DrawHelpers.DrawNotification("Export string copied to clipboard.");
            }
            else
            {
                DrawHelpers.DrawNotification("Failed to Export!", NotificationType.Error);
            }
        }

        public static string? GetExportString<T>(T toExport)
        {
            try
            {
                string jsonString = JsonConvert.SerializeObject(toExport, Formatting.None, _serializerSettings);
                using (MemoryStream outputStream = new())
                {
                    using (DeflateStream compressionStream = new(outputStream, CompressionLevel.Optimal))
                    {
                        using (StreamWriter writer = new(compressionStream, Encoding.UTF8))
                        {
                            writer.Write(jsonString);
                        }
                    }

                    return Convert.ToBase64String(outputStream.ToArray());
                }
            }
            catch (Exception ex)
            {
                Singletons.Get<IPluginLog>().Error(ex.ToString());
            }

            return null;
        }

        public static T? GetFromImportString<T>(string importString)
        {
            if (string.IsNullOrEmpty(importString)) return default;

            try
            {
                byte[] bytes = Convert.FromBase64String(importString);

                string decodedJsonString;
                using (MemoryStream inputStream = new(bytes))
                {
                    using (DeflateStream compressionStream = new(inputStream, CompressionMode.Decompress))
                    {
                        using (StreamReader reader = new(compressionStream, Encoding.UTF8))
                        {
                            decodedJsonString = reader.ReadToEnd();
                        }
                    }
                }

                T? importedObj = JsonConvert.DeserializeObject<T>(decodedJsonString, _serializerSettings);
                return importedObj;
            }
            catch (Exception ex)
            {
                Singletons.Get<IPluginLog>().Error(ex.ToString());
            }

            return default;
        }

        public static LMeterConfig LoadConfig(string path)
        {
            LMeterConfig? config = null;

            try
            {
                if (File.Exists(path))
                {
                    string jsonString = File.ReadAllText(path);
                    config = JsonConvert.DeserializeObject<LMeterConfig>(jsonString, _serializerSettings);
                }
            }
            catch (Exception ex)
            {
                Singletons.Get<IPluginLog>().Error(ex.ToString());

                string backupPath = $"{path}.bak";
                if (File.Exists(path))
                {
                    try
                    {
                        File.Copy(path, backupPath);
                        Singletons.Get<IPluginLog>().Information($"Backed up LMeter config to '{backupPath}'.");
                    }
                    catch
                    {
                        Singletons.Get<IPluginLog>().Warning($"Unable to back up LMeter config.");
                    }
                }
            }

            return config ?? new LMeterConfig();
        }

        public static void SaveConfig()
        {
            ConfigHelpers.SaveConfig(Singletons.Get<LMeterConfig>());
        }

        public static void SaveConfig(LMeterConfig config)
        {
            try
            {
                string jsonString = JsonConvert.SerializeObject(config, Formatting.Indented, _serializerSettings);
                File.WriteAllText(Plugin.ConfigFilePath, jsonString);
            }
            catch (Exception ex)
            {
                Singletons.Get<IPluginLog>().Error(ex.ToString());
            }
        }

        public static void ConvertOldConfig(LMeterConfig config)
        {
            foreach (var meter in config.MeterList.Meters)
            {   
                Vector2 size = meter.GeneralConfig.Size;
                size -= Vector2.One * meter.GeneralConfig.BorderThickness * 2;
                size.AddY(-meter.HeaderConfig.HeaderHeight);
                float barHeight = (size.Y - (meter.BarConfig.BarCount - 1) * meter.BarConfig.BarGaps) / meter.BarConfig.BarCount;
                float rankTextOffset = meter.BarConfig.ShowRankText ? ImGui.CalcTextSize("00.").X : 0;
                BarConfig barConfig = meter.BarConfig;
                TextListConfig<Combatant> barTextConfig = meter.BarTextConfig;

                if (!barTextConfig.Initialized &&
                    barTextConfig.Texts.Count == 0)
                {
                    barTextConfig.AddText(new Text("Name")
                    {
                        Enabled = true,
                        TextFormat = barConfig.LeftTextFormat.Replace("[encdps", "[dps"),
                        TextOffset = barConfig.LeftTextOffset.AddX(rankTextOffset + barHeight - 5f),
                        TextAlignment = DrawAnchor.Left,
                        AnchorPoint = DrawAnchor.Left,
                        TextJobColor = barConfig.LeftTextJobColor,
                        TextColor = barConfig.BarNameColor,
                        ShowOutline = barConfig.BarNameShowOutline,
                        OutlineColor = barConfig.BarNameOutlineColor,
                        FontKey = barConfig.BarNameFontKey,
                        FontId = barConfig.BarNameFontId,
                        ThousandsSeparators = barConfig.ThousandsSeparators
                    });

                    barTextConfig.AddText(new Text("Data")
                    {
                        Enabled = true,
                        TextFormat = barConfig.RightTextFormat.Replace("[encdps", "[dps"),
                        TextOffset = barConfig.RightTextOffset,
                        TextAlignment = DrawAnchor.Right,
                        AnchorPoint = DrawAnchor.Right,
                        TextJobColor = barConfig.RightTextJobColor,
                        TextColor = barConfig.BarDataColor,
                        ShowOutline = barConfig.BarDataShowOutline,
                        OutlineColor = barConfig.BarDataOutlineColor,
                        FontKey = barConfig.BarDataFontKey,
                        FontId = barConfig.BarDataFontId,
                        ThousandsSeparators = barConfig.ThousandsSeparators
                    });
                }

                
                HeaderConfig headerConfig = meter.HeaderConfig;
                TextListConfig<Encounter> headerTextConfig = meter.HeaderTextConfig;
                if (!headerTextConfig.Initialized &&
                    headerTextConfig.Texts.Count == 0)
                {
                    headerTextConfig.AddText(new Text("Encounter Duration")
                    {
                        Enabled = true,
                        TextFormat = "[duration]",
                        AnchorParent = 0,
                        TextOffset = headerConfig.DurationOffset,
                        TextAlignment = DrawAnchor.Left,
                        AnchorPoint = DrawAnchor.Left,
                        TextJobColor = false,
                        TextColor = headerConfig.DurationColor,
                        ShowOutline = headerConfig.DurationShowOutline,
                        OutlineColor = headerConfig.DurationOutlineColor,
                        FontKey = headerConfig.DurationFontKey,
                        FontId = headerConfig.DurationFontId,
                        ThousandsSeparators = false
                    });

                    headerTextConfig.AddText(new Text("Encounter Name")
                    {
                        Enabled = true,
                        TextFormat = "[title]",
                        AnchorParent = 1,
                        TextOffset = headerConfig.DurationOffset - headerConfig.NameOffset,
                        TextAlignment = DrawAnchor.Left,
                        AnchorPoint = DrawAnchor.Right,
                        TextJobColor = false,
                        TextColor = headerConfig.NameColor,
                        ShowOutline = headerConfig.NameShowOutline,
                        OutlineColor = headerConfig.NameOutlineColor,
                        FontKey = headerConfig.NameFontKey,
                        FontId = headerConfig.NameFontId,
                        ThousandsSeparators = false
                    });

                    headerTextConfig.AddText(new Text("Raid Stats")
                    {
                        Enabled = true,
                        TextFormat = headerConfig.RaidStatsFormat,
                        AnchorParent = 0,
                        TextOffset = headerConfig.StatsOffset,
                        TextAlignment = DrawAnchor.Right,
                        AnchorPoint = DrawAnchor.Right,
                        TextJobColor = false,
                        TextColor = headerConfig.RaidStatsColor,
                        ShowOutline = headerConfig.StatsShowOutline,
                        OutlineColor = headerConfig.StatsOutlineColor,
                        FontKey = headerConfig.StatsFontKey,
                        FontId = headerConfig.StatsFontId,
                        ThousandsSeparators = false
                    });
                }
            }
        }
    }

    /// <summary>
    /// Because the game blocks the json serializer from loading assemblies at runtime, we define
    /// a custom SerializationBinder to ignore the assembly name for the types defined by this plugin.
    /// </summary>
    public class LMeterSerializationBinder : ISerializationBinder
    {
        private static readonly List<Type> _configTypes =
        [
            typeof(ActConfig)
        ];

        private static readonly Dictionary<string, string> _typeNameConversions = new()
        {
            { "VisibilityConfig2", "VisibilityConfig" }
        };

        private readonly Dictionary<Type, string> _typeToName = [];
        private readonly Dictionary<string, Type> _nameToType = [];

        public LMeterSerializationBinder()
        {
            foreach (Type type in _configTypes)
            {
                if (type.FullName is not null)
                {
                    _typeToName.Add(type, type.FullName.ToLower());
                    _nameToType.Add(type.FullName.ToLower(), type);
                }
            }
        }

        public void BindToName(Type serializedType, out string? assemblyName, out string? typeName)
        {
            if (_typeToName.TryGetValue(serializedType, out string? name))
            {
                assemblyName = null;
                typeName = name;
            }
            else
            {
                assemblyName = serializedType.Assembly.FullName;
                typeName = serializedType.FullName;
            }
        }

        public Type BindToType(string? assemblyName, string? typeName)
        {
            if (typeName is null)
            {
                throw new TypeLoadException("Type name was null.");
            }

            if (_nameToType.TryGetValue(typeName.ToLower(), out Type? type))
            {
                return type;
            }

            Type? loadedType = Type.GetType($"{typeName}, {assemblyName}", false);
            if (loadedType is null)
            {
                foreach (var entry in _typeNameConversions)
                {
                    if (typeName.Contains(entry.Key))
                    {
                        typeName = typeName.Replace(entry.Key, entry.Value);
                    }
                }
            }

            return Type.GetType($"{typeName}, {assemblyName}", true) ??
                throw new TypeLoadException($"Unable to load type '{typeName}' from assembly '{assemblyName}'");
        }
    }
}
