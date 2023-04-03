using System;
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
        private const string _defaultSocketAddress = "ws://127.0.0.1:10501/ws";

        [JsonIgnore]
        private DateTime? LastCombatTime { get; set; } = null;

        [JsonIgnore]
        private DateTime? LastReconnectAttempt { get; set; } = null;

        public string Name => "ACT";
        
        public IConfigPage GetDefault() => new ACTConfig();

        public bool IINACTMode = true;
        public string ACTSocketAddress;

        public int EncounterHistorySize = 15;

        public bool AutoReconnect = false;
        public int ReconnectDelay = 30;

        public bool ClearACT = false;
        public bool AutoEnd = false;
        public int AutoEndDelay = 3;

        public ACTConfig()
        {
            this.ACTSocketAddress = _defaultSocketAddress;
        }

        public void DrawConfig(Vector2 size, float padX, float padY)
        {
            if (ImGui.BeginChild($"##{this.Name}", new Vector2(size.X, size.Y), true))
            {
                ImGui.Text("ACT Connection Mode:");

                var newClientRequested = false;
                var iinactModeNum = IINACTMode ? 1 : 0;

                newClientRequested |= ImGui.RadioButton("ACT WebSocket", ref iinactModeNum, 0);
                ImGui.SameLine();
                newClientRequested |= ImGui.RadioButton("IINACT", ref iinactModeNum, 1);

                IINACTMode = iinactModeNum == 1;
                if (newClientRequested)
                {
                    IACTClient client = IACTClient.GetNewClient(); // Singleton Registry is done internally here
                    client.Start();
                }

                Vector2 buttonSize = new Vector2(40, 0);

                if (IINACTMode)
                {
                    ImGui.Text($"IINACT Status: " + IACTClient.Current.Status);
                }
                else
                {
                    ImGui.Text($"ACT Status: {IACTClient.Current.Status}");
                    ImGui.InputTextWithHint("ACT Websocket Address", $"Default: '{_defaultSocketAddress}'", ref this.ACTSocketAddress, 64);
                }

                DrawHelpers.DrawButton(string.Empty, FontAwesomeIcon.Sync, IACTClient.Current.RetryConnection, "Reconnect", buttonSize);
                ImGui.SameLine();
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 1f);
                ImGui.Text("Retry Connection");
                ImGui.NewLine();
                ImGui.Checkbox("Automatically attempt to reconnect if connection fails", ref this.AutoReconnect);
                if (this.AutoReconnect)
                {
                    DrawHelpers.DrawNestIndicator(1);
                    ImGui.PushItemWidth(30);
                    ImGui.InputInt("Seconds between reconnect attempts", ref this.ReconnectDelay, 0, 0);
                    ImGui.PopItemWidth();
                }

                ImGui.NewLine();
                ImGui.PushItemWidth(30);
                ImGui.InputInt("Number of Encounters to save", ref this.EncounterHistorySize, 0, 0);
                ImGui.PopItemWidth();

                ImGui.NewLine();
                ImGui.Checkbox("Clear ACT when clearing LMeter", ref this.ClearACT);
                ImGui.Checkbox("Force ACT to end encounter after combat", ref this.AutoEnd);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("It is recommended to disable ACT Command Sounds if you use this feature.\n" +
                                     "The option can be found in ACT under Options -> Sound Settings.");
                }
                
                if (this.AutoEnd)
                {
                    DrawHelpers.DrawNestIndicator(1);
                    ImGui.PushItemWidth(30);
                    ImGui.InputInt("Seconds delay after combat", ref this.AutoEndDelay, 0, 0);
                    ImGui.PopItemWidth();
                }

                ImGui.NewLine();
                DrawHelpers.DrawButton(string.Empty, FontAwesomeIcon.Stop, () => IACTClient.Current.EndEncounter(), null, buttonSize);
                ImGui.SameLine();
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 1f);
                ImGui.Text("Force End Combat");

                DrawHelpers.DrawButton(string.Empty, FontAwesomeIcon.Trash, () => Singletons.Get<PluginManager>().Clear(), null, buttonSize);
                ImGui.SameLine();
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 1f);
                ImGui.Text("Clear LMeter");
            }
            
            ImGui.EndChild();
        }

        public void TryReconnect()
        {
            if (this.LastReconnectAttempt.HasValue &&
                (IACTClient.Current.Status == ConnectionStatus.NotConnected ||
                IACTClient.Current.Status == ConnectionStatus.ConnectionFailed))
            {
                if (this.AutoReconnect &&
                    this.LastReconnectAttempt < DateTime.UtcNow - TimeSpan.FromSeconds(this.ReconnectDelay))
                {
                    IACTClient.Current.RetryConnection();
                    this.LastReconnectAttempt = DateTime.UtcNow;
                }
            }
            else
            {
                this.LastReconnectAttempt = DateTime.UtcNow;
            }
        }

        public void TryEndEncounter()
        {
            if (IACTClient.Current.Status == ConnectionStatus.Connected)
            {
                if (this.AutoEnd &&
                    CharacterState.IsInCombat())
                {
                    this.LastCombatTime = DateTime.UtcNow;
                }
                else if (this.LastCombatTime is not null && 
                         this.LastCombatTime < DateTime.UtcNow - TimeSpan.FromSeconds(this.AutoEndDelay))
                {
                    IACTClient.Current.EndEncounter();
                    this.LastCombatTime = null;
                }
            }
        }
    }
}
