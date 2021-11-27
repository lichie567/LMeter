using System.Numerics;
using Dalamud.Interface;
using ImGuiNET;
using LMeter.ACT;
using LMeter.Helpers;

namespace LMeter.Config
{
    public class ACTConfig : IConfigPage
    {
        public string Name => "ACT";

        public string ACTSocketAddress;

        public ACTConfig()
        {
            this.ACTSocketAddress = "ws://localhost:10501/ws";
        }

        public void DrawConfig(Vector2 size, float padX, float padY)
        {
            if (ImGui.BeginChild($"##{this.Name}", new Vector2(size.X, size.Y), true))
            {
                Vector2 buttonSize = new Vector2(40, 0);
                ACTClient client = Singletons.Get<ACTClient>();
                ImGui.Text($"ACT Status: {client.Status}");
                ImGui.InputTextWithHint("ACT Websocket Address", "Default: 'ws://localhost:10501/ws'", ref this.ACTSocketAddress, 64);
                DrawHelpers.DrawButton(string.Empty, FontAwesomeIcon.Sync, () => RetryACTConnection(), "Reconnect", buttonSize);

                ImGui.SameLine();
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 1f);
                ImGui.Text("Retry ACT Connection");

                ImGui.EndChild();
            }
        }

        public void RetryACTConnection()
        {
            ACTClient client = Singletons.Get<ACTClient>();
            client.Reset();
            client.Start(this.ACTSocketAddress);
        }
    }
}
