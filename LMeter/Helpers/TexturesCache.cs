using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Dalamud.Data;
using Dalamud.Interface;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Utility;
using ImGuiScene;
using Lumina.Data.Files;
using Lumina.Data.Parsing.Tex;
using static Lumina.Data.Files.TexFile;

namespace LMeter.Helpers
{
    public class TexturesCache : ILMeterDisposable
    {
        private Dictionary<string, Tuple<TextureWrap, float>> TextureCache { get; init; }
        private ICallGateSubscriber<string, string> PenumbraPathResolver { get; init; }
        private UiBuilder UiBuilder { get; init; }

        public TexturesCache(DalamudPluginInterface pluginInterface)
        {
            this.TextureCache = new Dictionary<string, Tuple<TextureWrap, float>>();
            this.PenumbraPathResolver = pluginInterface.GetIpcSubscriber<string, string>("Penumbra.ResolveDefaultPath");
            this.UiBuilder = pluginInterface.UiBuilder;
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
            TextureWrap? textureWrap = null;
            try
            {
                string resolvedPath = this.PenumbraPathResolver.InvokeFunc(path);

                if (!string.IsNullOrEmpty(resolvedPath) && !resolvedPath.Equals(path))
                {
                    textureWrap = this.LoadPenumbraTexture(resolvedPath);
                }
                
                if (textureWrap is null)
                {
                    TexFile? iconFile = Singletons.Get<DataManager>().GetFile<TexFile>(path);
                    if (iconFile is null)
                    {
                        return null;
                    }

                    IconData newIcon = new IconData(iconFile);
                    textureWrap = newIcon.GetTextureWrap(greyScale, opacity);
                }
            }
            catch (Exception ex)
            {
                PluginLog.Warning(ex.ToString());
            }

            return textureWrap;
        }

        private TextureWrap? LoadPenumbraTexture(string path)
        {            
            try
            {
                var fileStream = new FileStream(path, FileMode.Open);
                var reader = new BinaryReader(fileStream);

                // read header
                int headerSize = Unsafe.SizeOf<TexHeader>();
                ReadOnlySpan<byte> headerData = reader.ReadBytes(headerSize);
                TexHeader Header = MemoryMarshal.Read<TexHeader>(headerData);

                // read image data
                byte[] rawImageData = reader.ReadBytes((int)fileStream.Length - headerSize);
                byte[] imageData = new byte[Header.Width * Header.Height * 4];

                if (!ProcessTexture(Header.Format, rawImageData, imageData, Header.Width, Header.Height))
                {
                    return null;
                }

                return this.UiBuilder.LoadImageRaw(GetRgbaImageData(imageData), Header.Width, Header.Height, 4);
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Error loading texture: {path} {ex.ToString()}");
            }
            
            return null;
        }
        
        private static byte[] GetRgbaImageData(byte[] imageData)
        {
            var dst = new byte[imageData.Length];

            for (var i = 0; i < dst.Length; i += 4)
            {
                dst[i] = imageData[i + 2];
                dst[i + 1] = imageData[i + 1];
                dst[i + 2] = imageData[i];
                dst[i + 3] = imageData[i + 3];
            }

            return dst;
        }

        private static bool ProcessTexture(TextureFormat format, byte[] src, byte[] dst, int width, int height)
        {
            switch (format)
            {
                case TextureFormat.DXT1: Decompress(SquishOptions.DXT1, src, dst, width, height); return true;
                case TextureFormat.DXT3: Decompress(SquishOptions.DXT3, src, dst, width, height); return true;
                case TextureFormat.DXT5: Decompress(SquishOptions.DXT5, src, dst, width, height); return true;
                case TextureFormat.R5G5B5A1: ProcessA1R5G5B5(src, dst, width, height); return true;
                case TextureFormat.R4G4B4A4: ProcessA4R4G4B4(src, dst, width, height); return true;
                case TextureFormat.L8: ProcessR3G3B2(src, dst, width, height); return true;
                case TextureFormat.A8R8G8B8: Array.Copy(src, dst, dst.Length); return true;
            }

            return false;
        }
        
        private static void Decompress(SquishOptions squishOptions, byte[] src, byte[] dst, int width, int height)
        {
            var decompressed = Squish.DecompressImage(src, width, height, squishOptions);
            Array.Copy(decompressed, dst, dst.Length);
        }

        private static void ProcessA1R5G5B5(Span<byte> src, byte[] dst, int width, int height)
        {
            for (var i = 0; (i + 2) <= 2 * width * height; i += 2)
            {
                var v = BitConverter.ToUInt16(src.Slice(i, sizeof(UInt16)).ToArray(), 0);

                var a = (uint)(v & 0x8000);
                var r = (uint)(v & 0x7C00);
                var g = (uint)(v & 0x03E0);
                var b = (uint)(v & 0x001F);

                var rgb = ((r << 9) | (g << 6) | (b << 3));
                var argbValue = (a * 0x1FE00 | rgb | ((rgb >> 5) & 0x070707));

                for (var j = 0; j < 4; ++j)
                {
                    dst[i * 2 + j] = (byte)(argbValue >> (8 * j));
                }
            }
        }

        private static void ProcessA4R4G4B4(Span<byte> src, byte[] dst, int width, int height)
        {
            for (var i = 0; (i + 2) <= 2 * width * height; i += 2)
            {
                var v = BitConverter.ToUInt16(src.Slice(i, sizeof(UInt16)).ToArray(), 0);

                for (var j = 0; j < 4; ++j)
                {
                    dst[i * 2 + j] = (byte)(((v >> (4 * j)) & 0x0F) << 4);
                }
            }
        }

        private static void ProcessR3G3B2(Span<byte> src, byte[] dst, int width, int height)
        {
            for (var i = 0; i < width * height; ++i)
            {
                var r = (uint)(src[i] & 0xE0);
                var g = (uint)(src[i] & 0x1C);
                var b = (uint)(src[i] & 0x03);

                dst[i * 4 + 0] = (byte)(b | (b << 2) | (b << 4) | (b << 6));
                dst[i * 4 + 1] = (byte)(g | (g << 3) | (g << 6));
                dst[i * 4 + 2] = (byte)(r | (r << 3) | (r << 6));
                dst[i * 4 + 3] = 0xFF;
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