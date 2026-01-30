using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using LMeter.Act;
using LMeter.Helpers;
using Newtonsoft.Json;

namespace LMeter.Config
{
    public class ActConfig : IConfigPage
    {
        [JsonIgnore]
        private const string SOCKET_ADDRESS = "ws://127.0.0.1:10501/ws";

        [JsonIgnore]
        public bool Active { get; set; }
        public string Name => "ACT";
        public string ActSocketAddress;
        public int EncounterHistorySize = 15;
        public bool AutoReconnect = false;
        public int ReconnectDelay = 30;
        public bool ClearAct = false;
        public bool AutoEnd = false;
        public int AutoEndDelay = 3;
        public int ClientType = 0;
        public bool UseFFLogs = false;
        public bool DisableFFLogsOutsideDuty = true;
        public bool LogConnectionErrors = true;

        public ActConfig()
        {
            this.ActSocketAddress = SOCKET_ADDRESS;
        }

        public IConfigPage GetDefault() => new ActConfig();

        public void DrawConfig(Vector2 size, float padX, float padY, bool border = true)
        {
            if (ImGui.BeginChild($"##{this.Name}", new Vector2(size.X, size.Y), border))
            {
                int currentClientType = this.ClientType;
                ImGui.Text("ACT Client Type:");
                ImGui.RadioButton("WebSocket", ref this.ClientType, 0);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(
                        "Use this option if you are using the standard standalone Advanced Combat Tracker program."
                    );
                }

                ImGui.SameLine();
                ImGui.RadioButton("IINACT IPC", ref this.ClientType, 1);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Use this option if you are using the IINACT dalamud plugin.");
                }

                if (currentClientType != this.ClientType)
                {
                    Singletons.Get<PluginManager>().ChangeClientType(this.ClientType);
                }

                Vector2 buttonSize = new(40, 0);
                ImGui.Text($"ACT Status: {Singletons.Get<LogClient>().Status}");
                if (this.ClientType == 0)
                {
                    ImGui.InputTextWithHint(
                        "ACT Websocket Address",
                        $"Default: '{SOCKET_ADDRESS}'",
                        ref this.ActSocketAddress,
                        64
                    );
                }

                DrawHelpers.DrawButton(
                    string.Empty,
                    FontAwesomeIcon.Sync,
                    () => Singletons.Get<LogClient>().Reset(),
                    "Reconnect",
                    buttonSize
                );

                ImGui.SameLine();
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 1f);
                ImGui.Text("Retry ACT Connection");

                if (this.UseFFLogs)
                {
                    DrawHelpers.DrawNestIndicator(1);
                    ImGui.Checkbox("Disable FFLogs Calculations Outside Duty", ref this.DisableFFLogsOutsideDuty);
                }

                ImGui.NewLine();
                ImGui.PushItemWidth(30);
                ImGui.InputInt("Number of Encounters to save", ref this.EncounterHistorySize, 0, 0);
                ImGui.PopItemWidth();

                ImGui.NewLine();
                ImGui.Checkbox("Automatically attempt to reconnect if connection fails", ref this.AutoReconnect);
                if (this.AutoReconnect)
                {
                    DrawHelpers.DrawNestIndicator(1);
                    ImGui.PushItemWidth(30);
                    ImGui.InputInt("Seconds between reconnect attempts", ref this.ReconnectDelay, 0, 0);
                    ImGui.PopItemWidth();
                }

                ImGui.Checkbox("Log connection errors", ref this.LogConnectionErrors);

                ImGui.NewLine();
                ImGui.Checkbox("Clear ACT when clearing LMeter", ref this.ClearAct);
                ImGui.Checkbox("Force ACT to end encounter after combat", ref this.AutoEnd);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(
                        "It is recommended to disable ACT Command Sounds if you use this feature.\n"
                            + "The option can be found in ACT under Options -> Sound Settings."
                    );
                }

                if (this.AutoEnd)
                {
                    DrawHelpers.DrawNestIndicator(1);
                    ImGui.PushItemWidth(30);
                    ImGui.InputInt("Seconds delay after combat", ref this.AutoEndDelay, 0, 0);
                    ImGui.PopItemWidth();
                }

                ImGui.NewLine();
                DrawHelpers.DrawButton(
                    string.Empty,
                    FontAwesomeIcon.Stop,
                    () => Singletons.Get<LogClient>().EndEncounter(),
                    null,
                    buttonSize
                );
                ImGui.SameLine();
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 1f);
                ImGui.Text("Force End Combat");

                DrawHelpers.DrawButton(
                    string.Empty,
                    FontAwesomeIcon.Trash,
                    () => Singletons.Get<PluginManager>().Clear(),
                    null,
                    buttonSize
                );
                ImGui.SameLine();
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 1f);
                ImGui.Text("Clear LMeter");
            }

            ImGui.EndChild();
        }
    }
}
