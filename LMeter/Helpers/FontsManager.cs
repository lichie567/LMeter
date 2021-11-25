using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using Dalamud.Logging;
using System.Linq;
using System.Reflection;
using Dalamud.Interface;

namespace LMeter.Helpers
{
    public struct FontData
    {
        public string Name;
        public int Size;
        public bool Chinese;
        public bool Korean;

        public FontData(string name, int size, bool chinese, bool korean)
        {
            Name = name;
            Size = size;
            Chinese = chinese;
            Korean = korean;
        }
    }

    public class FontsManager : ILMeterDisposable
    {
        private IEnumerable<FontData> FontData { get; set; }
        private Dictionary<string, ImFontPtr> ImGuiFonts { get; init; }
        private string[] FontList { get; set; }
        private UiBuilder UiBuilder { get; init; }

        public const string DalamudFontKey = "Dalamud Font";

        public static readonly List<string> DefaultFontKeys = new List<string>() { "big-noodle-too_24", "big-noodle-too_20", "big-noodle-too_16" };
        public static string DefaultBigFontKey => DefaultFontKeys[0];
        public static string DefaultMediumFontKey => DefaultFontKeys[1];
        public static string DefaultSmallFontKey => DefaultFontKeys[2];

        public FontsManager(UiBuilder uiBuilder, IEnumerable<FontData> fonts)
        {
            this.FontData = fonts;
            this.FontList = new string[0];
            this.ImGuiFonts = new Dictionary<string, ImFontPtr>();

            this.UiBuilder = uiBuilder;
            this.UiBuilder.BuildFonts += BuildFonts;
            this.UiBuilder.RebuildFonts();
        }

        public void BuildFonts()
        {
            string fontDir = GetUserFontPath();

            if (string.IsNullOrEmpty(fontDir))
            {
                return;
            }

            this.ImGuiFonts.Clear();
            ImGuiIOPtr io = ImGui.GetIO();

            foreach (FontData font in this.FontData)
            {
                string fontPath = $"{fontDir}{font.Name}.ttf";
                if (!File.Exists(fontPath))
                {
                    continue;
                }

                try
                {
                    ImVector? ranges = this.GetCharacterRanges(font, io);

                    ImFontPtr imFont = !ranges.HasValue
                        ? io.Fonts.AddFontFromFileTTF(fontPath, font.Size)
                        : io.Fonts.AddFontFromFileTTF(fontPath, font.Size, null, ranges.Value.Data);

                    this.ImGuiFonts.Add(GetFontKey(font), imFont);
                }
                catch (Exception ex)
                {
                    PluginLog.Error($"Failed to load font from path [{fontPath}]!");
                    PluginLog.Error(ex.ToString());
                }
            }

            List<string> fontList = new List<string>() { DalamudFontKey };
            fontList.AddRange(this.ImGuiFonts.Keys);
            this.FontList = fontList.ToArray();
        }

        public bool PushFont(string fontKey)
        {
            if (string.IsNullOrEmpty(fontKey) ||
                fontKey.Equals(DalamudFontKey) ||
                !this.ImGuiFonts.Keys.Contains(fontKey))
            {
                return false;
            }

            ImGui.PushFont(this.ImGuiFonts[fontKey]);
            return true;
        }

        public void UpdateFonts(IEnumerable<FontData> fonts)
        {
            this.FontData = fonts;
            Singletons.Get<UiBuilder>().RebuildFonts();
        }

        public string[] GetFontList()
        {
            return this.FontList;
        }

        public int GetFontIndex(string fontKey)
        {
            for (int i = 0; i < this.FontList.Length; i++)
            {
                if (this.FontList[i].Equals(fontKey))
                {
                    return i;
                }
            }

            return 0;
        }

        private unsafe ImVector? GetCharacterRanges(FontData font, ImGuiIOPtr io)
        {
            if (!font.Chinese && !font.Korean)
            {
                return null;
            }

            var builder = new ImFontGlyphRangesBuilderPtr(ImGuiNative.ImFontGlyphRangesBuilder_ImFontGlyphRangesBuilder());

            if (font.Chinese)
            {
                // GetGlyphRangesChineseFull() includes Default + Hiragana, Katakana, Half-Width, Selection of 1946 Ideographs
                // https://skia.googlesource.com/external/github.com/ocornut/imgui/+/v1.53/extra_fonts/README.txt
                builder.AddRanges(io.Fonts.GetGlyphRangesChineseFull());
            }

            if (font.Korean)
            {
                builder.AddRanges(io.Fonts.GetGlyphRangesKorean());
            }

            builder.BuildRanges(out ImVector ranges);

            return ranges;
        }

        public static string GetFontKey(FontData font)
        {
            string key = $"{font.Name}_{font.Size}";
            key += (font.Chinese ? "_cnjp" : string.Empty);
            key += (font.Korean ? "_kr" : string.Empty);
            return key;
        }

        public static void CopyPluginFontsToUserPath()
        {
            string? pluginFontPath = GetPluginFontPath();
            string? userFontPath = GetUserFontPath();

            if (string.IsNullOrEmpty(pluginFontPath) || string.IsNullOrEmpty(userFontPath))
            {
                return;
            }

            if (!Directory.Exists(userFontPath))
            {
                try
                {
                    Directory.CreateDirectory(userFontPath);
                }
                catch (Exception ex)
                {
                    PluginLog.Warning($"Failed to create User Font Directory {ex.ToString()}");
                }
            }

            if (!Directory.Exists(userFontPath))
            {
                return;
            }
            
            string[] pluginFonts;
            try
            {
                pluginFonts = Directory.GetFiles(pluginFontPath, "*.ttf");
            }
            catch
            {
                pluginFonts = new string[0];
            }

            foreach (string font in pluginFonts)
            {
                try
                {
                    if (!string.IsNullOrEmpty(font))
                    {
                        string fileName = font.Replace(pluginFontPath, string.Empty);
                        string copyPath = Path.Combine(userFontPath, fileName);
                        if (!File.Exists(copyPath))
                        {
                            File.Copy(font, copyPath, false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    PluginLog.Warning($"Failed to copy font {font} to User Font Directory: {ex.ToString()}");
                }
            }
        }

        public static string GetPluginFontPath()
        {
            string? path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            if (path is not null)
            {
                return $"{path}\\Media\\Fonts\\";
            }

            return string.Empty;
        }
        
        public static string GetUserFontPath()
        {
            return $"{Plugin.ConfigFileDir}\\Fonts\\";
        }

        public static string[] GetFontNamesFromPath(string? path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return new string[0];
            }

            string[] fonts;
            try
            {
                fonts = Directory.GetFiles(path, "*.ttf");
            }
            catch
            {
                fonts = new string[0];
            }

            return fonts
                .Select(f => f
                    .Replace(path, string.Empty)
                    .Replace(".ttf", string.Empty, StringComparison.OrdinalIgnoreCase))
                .ToArray();
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
                this.ImGuiFonts.Clear();
                this.UiBuilder.BuildFonts -= BuildFonts;
                this.UiBuilder.RebuildFonts();
            }
        }
    }
}
