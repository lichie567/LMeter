using System;
using System.Collections.Generic;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Lumina.Data.Files;

namespace LMeter.Helpers
{
    public class TextureCache : IPluginDisposable
    {
        private readonly Dictionary<string, IDalamudTextureWrap> _cache;
        private readonly Dictionary<string, IDalamudTextureWrap> _desaturatedCache;

        public TextureCache()
        {
            _cache = [];
            _desaturatedCache = [];
        }

        public IDalamudTextureWrap? GetTextureById(
            uint iconId,
            uint stackCount = 0,
            bool hdIcon = true,
            bool desaturate = false)
        {
            string? path = Singletons.Get<ITextureProvider>().GetIconPath(new GameIconLookup(iconId: iconId + stackCount, hiRes: hdIcon));
            path = Singletons.Get<ITextureSubstitutionProvider>().GetSubstitutedPath(path);

            if (path is null)
            {
                return null;
            }

            if (desaturate && _desaturatedCache.TryGetValue(path, out IDalamudTextureWrap? dtex))
            {
                return dtex;
            }
            else if (_cache.TryGetValue(path, out IDalamudTextureWrap? tex))
            {
                return tex;
            }

            return this.GetTextureFromPenumbraOrGame(path, desaturate);
        }

        private IDalamudTextureWrap? GetTextureFromPenumbraOrGame(string path, bool desaturate)
        {
            TexFile? texFile = Singletons.Get<IDataManager>().GameData.GetFile<TexFile>(path);
            if (texFile is null)
            {
                return null;
            }

            byte[] bytes = texFile.GetRgbaImageData();
            if (desaturate)
            {
                DesaturateBytes(ref bytes);
            }

            IDalamudTextureWrap texWrap = Singletons.Get<ITextureProvider>()
                .CreateFromRaw(RawImageSpecification.Rgba32(texFile.Header.Width, texFile.Header.Width), bytes);

            if (desaturate)
            {
                _desaturatedCache.Add(path, texWrap);
            }
            else
            {
                _cache.Add(path, texWrap);
            }

            return texWrap;
        }

        private static void DesaturateBytes(ref byte[] bytes)
        {
            if (bytes.Length % 4 != 0)
            {
                return;
            }

            for (int i = 0; i < bytes.Length; i += 4)
            {
                int r = bytes[i] >> 2;
                int g = bytes[i + 1] >> 1;
                int b = bytes[i + 2] >> 3;
                byte lum = (byte)(r + g + b);

                bytes[i] = lum;
                bytes[i + 1] = lum;
                bytes[i + 2] = lum;
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
                foreach (IDalamudTextureWrap tex in _desaturatedCache.Values)
                {
                    tex.Dispose();
                }

                foreach (IDalamudTextureWrap tex in _cache.Values)
                {
                    tex.Dispose();
                }

                _desaturatedCache.Clear();
                _cache.Clear();
            }
        }
    }
}