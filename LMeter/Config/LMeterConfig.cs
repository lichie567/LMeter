using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using LMeter.Helpers;

namespace LMeter.Config
{
    [JsonObject]
    public class LMeterConfig : IConfigurable, ILMeterDisposable
    {
        public string Name => "LMeter";

        public string Version => Plugin.Version;

        public ACTConfig ACTConfig { get; set; }

        public FontConfig FontConfig { get; set; }

        [JsonIgnore]
        private AboutPage AboutPage { get; } = new AboutPage();

        public LMeterConfig()
        {
            this.ACTConfig = new ACTConfig();
            this.FontConfig = new FontConfig();
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
                ConfigHelpers.SaveConfig(this);
            }
        }

        public IEnumerable<IConfigPage> GetConfigPages()
        {
            yield return this.ACTConfig;
            yield return this.FontConfig;
            yield return this.AboutPage;
        }
    }
}