using System;
using System.Collections.Generic;
using Dalamud.Data;
using Dalamud.Interface;
using Dalamud.Logging;
using Dalamud.Utility;
using ImGuiScene;
using Lumina.Data.Files;

namespace LMeter.Helpers
{
    public class TexturesCache : ILMeterDisposable
    {
        private Dictionary<string, Tuple<TextureWrap, float>> TextureCache { get; init; }

        public TexturesCache()
        {
            this.TextureCache = new Dictionary<string, Tuple<TextureWrap, float>>();
        }

        public TextureWrap? GetTextureFromIconId(
            uint iconId,
            uint stackCount = 0,
            bool hdIcon = true,
            bool greyScale = false,
            float opacity = 1f)
        {
            string key = $"{iconId}{(greyScale ? "_g" : string.Empty)}{(opacity != 1f ? "_t" : string.Empty)}";
            if (this.TextureCache.TryGetValue(key, out var tuple))
            {
                TextureWrap texture = tuple.Item1;
                float cachedOpacity = tuple.Item2;
                if (cachedOpacity == opacity)
                {
                    return texture;
                }

                this.TextureCache.Remove(key);
            }

            TextureWrap? newTexture = this.LoadTexture(iconId + stackCount, hdIcon, greyScale, opacity);
            if (newTexture == null)
            {
                return null;
            }

            this.TextureCache.Add(key, new Tuple<TextureWrap, float>(newTexture, opacity));
            return newTexture;
        }

        private TextureWrap? LoadTexture(uint id, bool hdIcon, bool greyScale, float opacity = 1f)
        {
            string hdString = hdIcon ? "_hr1" : "";
            string path = $"ui/icon/{id / 1000 * 1000:000000}/{id:000000}{hdString}.tex";
            return this.LoadTexture(path, greyScale, opacity);
        }

        private TextureWrap? LoadTexture(string path, bool greyScale, float opacity = 1f)
        {
            try
            {
                TexFile? iconFile = Singletons.Get<DataManager>().GetFile<TexFile>(path);
                if (iconFile is null)
                {
                    return null;
                }

                IconData newIcon = new IconData(iconFile);
                return newIcon.GetTextureWrap(greyScale, opacity);
            }
            catch (Exception ex)
            {
                PluginLog.Warning(ex.ToString());
            }

            return null;
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
                foreach (var tuple in this.TextureCache.Values)
                {
                    tuple.Item1.Dispose();
                }

                this.TextureCache.Clear();
            }
        }
    }

    public class IconData
    {
        public byte[] Bytes { get; init; }
        public ushort Width { get; init; }
        public ushort Height { get; init; }

        public IconData(byte[] bytes, ushort width, ushort height)
        {
            this.Bytes = bytes;
            this.Width = width;
            this.Height = height;
        }

        public IconData(TexFile tex)
        {
            this.Bytes = tex.GetRgbaImageData();
            this.Width = tex.Header.Width;
            this.Height = tex.Header.Height;
        }

        public TextureWrap GetTextureWrap(bool greyScale, float opacity)
        {
            UiBuilder uiBuilder = Singletons.Get<UiBuilder>();
            byte[] bytes = this.Bytes;
            if (greyScale || opacity < 1f)
            {
                bytes = this.ConvertBytes(bytes, greyScale, opacity);
            }

            return uiBuilder.LoadImageRaw(bytes, this.Width, this.Height, 4);
        }

        public byte[] ConvertBytes(byte[] bytes, bool greyScale, float opacity)
        {
            if (bytes.Length % 4 != 0 || opacity > 1f || opacity < 0)
            {
                return bytes;
            }
            
            byte[] newBytes = new byte[bytes.Length];
            for (int i = 0; i < bytes.Length; i += 4)
            {
                if (greyScale)
                {
                    int r = bytes[i] >> 2;
                    int g = bytes[i + 1] >> 1;
                    int b = bytes[i + 2] >> 3;
                    byte lum = (byte)(r + g + b / 3);
                    
                    newBytes[i] = lum;
                    newBytes[i + 1] = lum;
                    newBytes[i + 2] = lum;
                }
                else
                {
                    newBytes[i] = bytes[i];
                    newBytes[i + 1] = bytes[i + 1];
                    newBytes[i + 2] = bytes[i + 2];
                }

                newBytes[i + 3] = (byte)(bytes[i + 3] * opacity);
            }

            return newBytes;
        }
    }
}