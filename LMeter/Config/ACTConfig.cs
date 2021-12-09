using System.Numerics;
using Dalamud.Interface;
using ImGuiNET;
using LMeter.ACT;
using LMeter.Helpers;
using Newtonsoft.Json;

namespace LMeter.Config
{
    public class ACTConfig : IConfigPage
    {
        [JsonIgnore]
        private string _defaultSocketAddress = "ws://127.0.0.1:10501/ws";

        public string Name => "ACT";

        public string ACTSocketAddress;
        public bool AutoEnd = true;
        public int AutoEndDelay = 3;

        public ACTConfig()
        {
            this.ACTSocketAddress = _defaultSocketAddress;
        }

        public void DrawConfig(Vector2 size, float padX, float padY)
        {
            if (ImGui.BeginChild($"##{this.Name}", new Vector2(size.X, size.Y), true))
            {
                Vector2 buttonSize = new Vector2(40, 0);
                ACTClient client = Singletons.Get<ACTClient>();
                ImGui.Text($"ACT Status: {client.Status}");
                ImGui.InputTextWithHint("ACT Websocket Address", $"Default: '{_defaultSocketAddress}'", ref this.ACTSocketAddress, 64);
                DrawHelpers.DrawButton(string.Empty, FontAwesomeIcon.Sync, () => RetryACTConnection(), "Reconnect", buttonSize);

                ImGui.SameLine();
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 1f);
                ImGui.Text("Retry ACT Connection");

                ImGui.NewLine();
                ImGui.Checkbox("Automatically end ACT encounter after combat", ref this.AutoEnd);
                if (this.AutoEnd)
                {
                    DrawHelpers.DrawNestIndicator(1);
                    ImGui.PushItemWidth(30);
                    ImGui.InputInt("Seconds delay after combat", ref this.AutoEndDelay, 0, 0);
                    ImGui.PopItemWidth();
                }

                ImGui.NewLine();
                DrawHelpers.DrawButton(string.Empty, FontAwesomeIcon.Stop, () => ACTClient.EndEncounter(), null, buttonSize);
                ImGui.SameLine();
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 1f);
                ImGui.Text("Force End Combat");

                DrawHelpers.DrawButton(string.Empty, FontAwesomeIcon.Trash, () => ACTClient.ClearAct(), null, buttonSize);
                ImGui.SameLine();
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 1f);
                ImGui.Text("Clear ACT Encounters");
            }
            
            ImGui.EndChild();
        }

        public void RetryACTConnection()
        {
            ACTClient client = Singletons.Get<ACTClient>();
            client.Reset();
            client.Start(this.ACTSocketAddress);
        }
    }
}
