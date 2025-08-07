using System;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Services;

namespace LMeter.Helpers
{
    public class TextureCache : IPluginDisposable
    {
        public static IDalamudTextureWrap? GetTextureById(uint iconId, uint stackCount = 0, bool hdIcon = true)
        {
            string path = Singletons
                .Get<ITextureProvider>()
                .GetIconPath(new GameIconLookup(iconId: iconId + stackCount, hiRes: hdIcon));
            return Singletons.Get<ITextureProvider>().GetFromGame(path).GetWrapOrDefault();
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) { }
    }
}
