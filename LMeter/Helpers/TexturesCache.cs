using System;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Services;

namespace LMeter.Helpers
{
    public class TextureCache : IPluginDisposable
    {
        public IDalamudTextureWrap? GetTextureById(
            uint iconId,
            uint stackCount = 0,
            bool hdIcon = true)
        {
            string path = Singletons.Get<ITextureProvider>().GetIconPath(new GameIconLookup(iconId: iconId + stackCount, hiRes: hdIcon));
            path = Singletons.Get<ITextureSubstitutionProvider>().GetSubstitutedPath(path);

            if (path is null)
            {
                return null;
            }

            return Singletons.Get<ITextureProvider>().GetFromGame(path).GetWrapOrDefault();
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}