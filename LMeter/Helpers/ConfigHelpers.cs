using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin.Services;
using ImGuiNET;
using LMeter.Config;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace LMeter.Helpers
{
    public static class ConfigHelpers
    {
        private static readonly ISerializationBinder _serializationBinder = new LMeterSerializationBinder();
        
        private static readonly JsonSerializerSettings _serializerSettings = new()
        {
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            TypeNameHandling = TypeNameHandling.Objects,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            SerializationBinder = _serializationBinder
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

                using MemoryStream outputStream = new();
                using (DeflateStream compressionStream = new(outputStream, CompressionLevel.Optimal))
                using (StreamWriter writer = new(compressionStream, Encoding.UTF8))

                writer.Write(jsonString);

                byte[] compressedBytes = outputStream.ToArray();
                string base64 = Convert.ToBase64String(compressedBytes);
                return base64;
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

                using MemoryStream inputStream = new(bytes);
                using DeflateStream compressionStream = new(inputStream, CompressionMode.Decompress);
                using StreamReader reader = new(compressionStream, Encoding.UTF8);
                
                decodedJsonString = reader.ReadToEnd();
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
                    using FileStream fs = File.OpenRead(path);
                    using StreamReader sr = new(fs);
                    using JsonTextReader reader = new(sr);
                    JsonSerializer serializer = JsonSerializer.Create(_serializerSettings);
                    config = serializer.Deserialize<LMeterConfig>(reader);
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
