using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using LMeter.Helpers;

namespace LMeter.Config
{
    [JsonObject]
    public class LMeterConfig : IConfigurable, IPluginDisposable
    {
        public string Name
        { 
            get => "LMeter";
            set {}
        }

        public string Version => Plugin.Version;

        public bool FirstLoad = true;

        public MeterListConfig MeterList { get; init; }

        public ACTConfig ACTConfig { get; init; }

        public FontConfig FontConfig { get; init; }

        [JsonIgnore]
        private AboutPage AboutPage { get; } = new AboutPage();

        public LMeterConfig()
        {
            this.MeterList = new MeterListConfig();
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
            yield return this.MeterList;
            yield return this.ACTConfig;
            yield return this.FontConfig;
            yield return this.AboutPage;
        }

        public void ImportPage(IConfigPage page)
        {
        }
    }
}