using System.Collections.Generic;

namespace LMeter.Config
{
    public interface IConfigurable
    {
        string Name { get; set; }

        IEnumerable<IConfigPage> GetConfigPages();
        void ImportPage(IConfigPage page);
    }
}
