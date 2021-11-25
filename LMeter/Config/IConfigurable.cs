using System.Collections.Generic;

namespace LMeter.Config
{
    public interface IConfigurable
    {
        string Name { get; }

        IEnumerable<IConfigPage> GetConfigPages();
    }
}
