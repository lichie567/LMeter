using System;
using System.Collections.Generic;
using Dalamud.Interface;
using Dalamud.Interface.Internal;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Lumina.Data.Files;
using static Dalamud.Plugin.Services.ITextureProvider;

namespace LMeter.Helpers
{
    public class TexturesCache : IPluginDisposable
    {
        private Dictionary<uint, IDalamudTextureWrap> _desaturatedCache;

        public TexturesCache()
        {
            _desaturatedCache = [];
        }

        public IDalamudTextureWrap? GetTextureFromIconId(
            uint iconId,
            uint stackCount = 0,
            bool hdIcon = true,
            bool greyScale = false)
        {
            IconFlags flags = hdIcon ? IconFlags.HiRes : IconFlags.None;

            if (!greyScale)
            {
                return Singletons.Get<ITextureProvider>().GetIcon(iconId + stackCount, flags);
            }

            if (_desaturatedCache.TryGetValue(iconId + stackCount, out IDalamudTextureWrap? t))
            {
                return t;
            }

            string? path = Singletons.Get<ITextureProvider>().GetIconPath(iconId + stackCount, flags);
            if (path != null)
            {
                path = Singletons.Get<ITextureSubstitutionProvider>().GetSubstitutedPath(path);
                IDalamudTextureWrap? texture = GetDesaturatedTextureWrap(path);
                if (texture != null)
                {
                    _desaturatedCache.Add(iconId + stackCount, texture);
                }

                return texture;
            }

            return null;
        }

        private static IDalamudTextureWrap? GetDesaturatedTextureWrap(string path)
        {
            TexFile? file = Singletons.Get<IDataManager>().GetFile<TexFile>(path);
            if (file == null) { return null; }

            UiBuilder uiBuilder = Singletons.Get<UiBuilder>();
            byte[] bytes = file.GetRgbaImageData();
            ConvertBytes(ref bytes);

            return uiBuilder.LoadImageRaw(bytes, file.Header.Width, file.Header.Height, 4);
        }

        private static void ConvertBytes(ref byte[] bytes)
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
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var text in _desaturatedCache.Values)
                {
                    text.Dispose();
                }

                _desaturatedCache.Clear();
            }
        }
    }
}