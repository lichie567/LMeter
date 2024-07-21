using System.Numerics;

namespace LMeter.Config
{
    public interface IConfigPage
    {
        string Name { get; }
        bool Active { get; set; }
        
        IConfigPage GetDefault();
        void DrawConfig(Vector2 size, float padX, float padY, bool border = true);
    }
}
