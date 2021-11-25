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
            this.ACTSocketAddress = "localhost:10501";
        }

        public void DrawConfig(Vector2 size, float padX, float padY)
        {
            if (ImGui.BeginChild("##ACT", new Vector2(size.X, size.Y), true))
            {
                Vector2 buttonSize = new Vector2(40, 0);
                ImGui.Text("ACT Status: Not Connected");
                ImGui.InputTextWithHint("ACT Websocket Address", "Default: 'localhost:10501'", ref this.ACTSocketAddress, 64);
                DrawHelpers.DrawButton(string.Empty, FontAwesomeIcon.Sync, () => RetryACTConnection(), "Reconnect", buttonSize);

                ImGui.SameLine();
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 1f);
                ImGui.Text("Retry ACT Connection");

                ImGui.EndChild();
            }
        }

        public static void RetryACTConnection()
        {

        }
    }
}
