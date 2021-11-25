using System.Numerics;
using Dalamud.Interface;
using ImGuiNET;
using LMeter.Helpers;

namespace LMeter.Config
{
    public class ACTConfig : IConfigPage
    {
        public string Name => "ACT";

        public string ACTSocketAddress;

        public ACTConfig()
        {
            this.ACTSocketAddress = "127.0.0.1:10501";
        }

        public void DrawConfig(Vector2 size, float padX, float padY)
        {
            if (ImGui.BeginChild("##ACT", new Vector2(size.X, size.Y), true))
            {
                ImGui.
                ImGui.EndChild();
            }
        }
    }
}
