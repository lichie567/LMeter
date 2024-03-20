using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Dalamud.Interface;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Services;
using ImGuiNET;

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

    public class FontScope : IDisposable
    {
        private readonly IFontHandle? _handle;

        public FontScope(IFontHandle? handle)
        {
            _handle = handle;
            _handle?.Push();
        }

        public void Dispose()
        {
            _handle?.Pop();
            GC.SuppressFinalize(this);
        }
    }

    public class FontsManager : IDisposable
    {
        private readonly Dictionary<string, IFontHandle> _imGuiFonts = [];
        private string[] _fontList;
        private readonly UiBuilder _uiBuilder;
        
        public const string DalamudFontKey = "Dalamud Font";
        public static readonly List<string> DefaultFontKeys = ["Expressway_24", "Expressway_20", "Expressway_16"];
        public static string DefaultBigFontKey => DefaultFontKeys[0];
        public static string DefaultMediumFontKey => DefaultFontKeys[1];
        public static string DefaultSmallFontKey => DefaultFontKeys[2];

        public FontsManager(UiBuilder uiBuilder, IEnumerable<FontData> fonts)
        {
            _uiBuilder = uiBuilder;
            _fontList = [DalamudFontKey];
            this.BuildFonts(fonts);
        }

        private void BuildFonts(IEnumerable<FontData> fontData)
        {
            var fontDir = GetUserFontPath();
            if (string.IsNullOrEmpty(fontDir))
            {
                return;
            }

            this.DisposeFontHandles();
            var io = ImGui.GetIO();

            foreach (var font in fontData)
            {
                var fontPath = $"{fontDir}{font.Name}.ttf";
                if (!File.Exists(fontPath))
                {
                    continue;
                }

                try
                {
                    var imFont = this._uiBuilder.FontAtlas.NewDelegateFontHandle
                    (
                        e => e.OnPreBuild
                        (
                            tk => tk.AddFontFromFile
                            (
                                fontPath,
                                new SafeFontConfig
                                {
                                    SizePx = font.Size,
                                    GlyphRanges = this.GetCharacterRanges(io, font.Chinese, font.Korean),
                                }
                            )
                        )
                    );

                    _imGuiFonts.Add(GetFontKey(font), imFont);
                }
                catch (Exception ex)
                {
                    Singletons.Get<IPluginLog>().Error($"Failed to load font from path [{fontPath}]!");
                    Singletons.Get<IPluginLog>().Error(ex.ToString());
                }
            }

            _fontList = [DalamudFontKey, .. _imGuiFonts.Keys];
        }

        public void UpdateFonts(IEnumerable<FontData> fonts)
        {
            this.BuildFonts(fonts);
        }

        private void DisposeFontHandles()
        {
            foreach ((string _, IFontHandle value) in _imGuiFonts)
            {
                value.Dispose();
            }
            
            _imGuiFonts.Clear();
        }

        private unsafe ushort[]? GetCharacterRanges(ImGuiIOPtr io, bool chinese, bool korean)
        {
            if (!chinese && !korean)
            {
                return null;
            }

            using (ImGuiHelpers.NewFontGlyphRangeBuilderPtrScoped(out var builder))
            {
                if (chinese)
                {
                    // GetGlyphRangesChineseFull() includes Default + Hiragana, Katakana, Half-Width, Selection of 1946 Ideographs
                    // https://skia.googlesource.com/external/github.com/ocornut/imgui/+/v1.53/extra_fonts/README.txt
                    builder.AddRanges(io.Fonts.GetGlyphRangesChineseFull());
                }

                if (korean)
                {
                    builder.AddRanges(io.Fonts.GetGlyphRangesKorean());
                }

                return builder.BuildRangesToArray();
            }
        }

        public static int GetFontIndex(string fontKey)
        {
            var manager = Singletons.Get<FontsManager>();
            for (var i = 0; i < manager._fontList.Length; i++)
            {
                if (manager._fontList[i].Equals(fontKey))
                {
                    return i;
                }
            }

            return 0;
        }

        public static bool ValidateFont(string[] fontOptions, int fontId, string fontKey)
        {
            return fontId < fontOptions.Length && fontOptions[fontId].Equals(fontKey);
        }

        public static FontScope PushFont(string fontKey)
        {
            if (!string.IsNullOrEmpty(fontKey))
            {
                if (Singletons.Get<FontsManager>()._imGuiFonts.TryGetValue(fontKey, out var fontHandle))
                {
                    return new FontScope(fontHandle);
                }
            }

            return new FontScope(null);
        }

        public static string[] GetFontList()
        {
            return Singletons.Get<FontsManager>()._fontList;
        }

        public static string GetFontKey(FontData font)
        {
            string key = $"{font.Name}_{font.Size}";
            key += font.Chinese ? "_cnjp" : string.Empty;
            key += font.Korean ? "_kr" : string.Empty;
            return key;
        }

        public static void CopyPluginFontsToUserPath()
        {
            var pluginFontPath = GetPluginFontPath();
            var userFontPath = GetUserFontPath();

            if (string.IsNullOrEmpty(pluginFontPath) || string.IsNullOrEmpty(userFontPath))
            {
                return;
            }

            try
            {
                Directory.CreateDirectory(userFontPath);
            }
            catch (Exception ex)
            {
                Singletons.Get<IPluginLog>().Warning($"Failed to create User Font Directory {ex}");
            }

            if (!Directory.Exists(userFontPath)) return;

            string[] pluginFonts;
            try
            {
                pluginFonts = Directory.GetFiles(pluginFontPath, "*.ttf");
            }
            catch
            {
                pluginFonts = [];
            }

            foreach (var font in pluginFonts)
            {
                try
                {
                    if (!string.IsNullOrEmpty(font))
                    {
                        var fileName = font.Replace(pluginFontPath, string.Empty);
                        var copyPath = Path.Combine(userFontPath, fileName);
                        if (!File.Exists(copyPath))
                        {
                            File.Copy(font, copyPath, false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Singletons.Get<IPluginLog>().Warning($"Failed to copy font {font} to User Font Directory: {ex}");
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
            if (string.IsNullOrEmpty(path)) return [];

            string[] fonts;
            try
            {
                fonts = Directory.GetFiles(path, "*.ttf");
            }
            catch
            {
                fonts = [];
            }

            for (var i = 0; i < fonts.Length; i++)
            {
                fonts[i] = fonts[i]
                    .Replace(path, string.Empty)
                    .Replace(".ttf", string.Empty, StringComparison.OrdinalIgnoreCase);
            }

            return fonts;
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
                this.DisposeFontHandles();
            }
        }
    }
}
