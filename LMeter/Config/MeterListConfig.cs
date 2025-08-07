using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiNotification;
using LMeter.Helpers;
using LMeter.Meter;
using Newtonsoft.Json;

namespace LMeter.Config
{
    public class MeterListConfig : IConfigPage
    {
        [JsonIgnore]
        private string _input = string.Empty;

        [JsonIgnore]
        private MeterDataType _meterDataType = MeterDataType.Damage;

        private const float MenuBarHeight = 40;

        [JsonIgnore]
        public bool Active { get; set; }

        public string Name => "Profiles";

        public List<MeterWindow> Meters { get; set; }

        public MeterListConfig()
        {
            this.Meters = [];
        }

        public IConfigPage GetDefault() => new MeterListConfig();

        public void DrawConfig(Vector2 size, float padX, float padY, bool border = true)
        {
            this.DrawCreateMenu(size, padX);
            this.DrawMeterTable(size.AddY(-padY), padX);
        }

        public void ToggleMeter(int? meterIndex)
        {
            if (meterIndex is null)
            {
                foreach (MeterWindow meter in this.Meters)
                {
                    meter.VisibilityConfig.AlwaysHide ^= true;
                }
            }
            else if (meterIndex >= 0 && meterIndex < this.Meters.Count)
            {
                this.Meters[meterIndex.Value].VisibilityConfig.AlwaysHide ^= true;
            }
        }

        public void ToggleClickThrough(int? meterIndex)
        {
            if (meterIndex is null)
            {
                foreach (MeterWindow meter in this.Meters)
                {
                    meter.GeneralConfig.ClickThrough ^= true;
                }
            }
            else if (meterIndex >= 0 && meterIndex < this.Meters.Count)
            {
                this.Meters[meterIndex.Value].GeneralConfig.ClickThrough ^= true;
            }
        }

        private void DrawCreateMenu(Vector2 size, float padX)
        {
            Vector2 buttonSize = new(40, 0);
            float meterTypeWidth = 100f;
            float textInputWidth = size.X - meterTypeWidth - buttonSize.X * 2 - padX * 5;

            if (ImGui.BeginChild("##Buttons", new Vector2(size.X, MenuBarHeight), true))
            {
                ImGui.PushItemWidth(textInputWidth);
                ImGui.InputTextWithHint("##Input", "Profile Name/Import String", ref _input, 10000);
                ImGui.PopItemWidth();

                ImGui.PushItemWidth(meterTypeWidth);
                ImGui.SameLine();
                ImGui.Combo("", ref Unsafe.As<MeterDataType, int>(ref _meterDataType), ["Damage", "Healing"], 2);
                ImGui.PopItemWidth();

                ImGui.SameLine();
                DrawHelpers.DrawButton(
                    string.Empty,
                    FontAwesomeIcon.Plus,
                    () => CreateMeter(_input, _meterDataType),
                    "Create new Meter",
                    buttonSize
                );

                ImGui.SameLine();
                DrawHelpers.DrawButton(
                    string.Empty,
                    FontAwesomeIcon.Download,
                    () => ImportMeter(_input),
                    "Import new Meter",
                    buttonSize
                );
                ImGui.PopItemWidth();
            }

            ImGui.EndChild();
        }

        private void DrawMeterTable(Vector2 size, float padX)
        {
            ImGuiTableFlags flags =
                ImGuiTableFlags.RowBg
                | ImGuiTableFlags.Borders
                | ImGuiTableFlags.BordersOuter
                | ImGuiTableFlags.BordersInner
                | ImGuiTableFlags.ScrollY
                | ImGuiTableFlags.NoSavedSettings;

            if (ImGui.BeginTable("##Meter_Table", 3, flags, new Vector2(size.X, size.Y - MenuBarHeight)))
            {
                Vector2 buttonsize = new(30, 0);
                float actionsWidth = buttonsize.X * 3 + padX * 2;

                ImGui.TableSetupColumn("Enabled", ImGuiTableColumnFlags.WidthFixed, 46, 0);
                ImGui.TableSetupColumn("Profile Name", ImGuiTableColumnFlags.WidthStretch, 0, 1);
                ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, actionsWidth, 2);

                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableHeadersRow();

                for (int i = 0; i < this.Meters.Count; i++)
                {
                    MeterWindow meter = this.Meters[i];

                    if (
                        !string.IsNullOrEmpty(_input)
                        && !meter.Name.Contains(_input, StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        continue;
                    }

                    ImGui.PushID(i.ToString());
                    ImGui.TableNextRow(ImGuiTableRowFlags.None, 28);

                    if (ImGui.TableSetColumnIndex(0))
                    {
                        ImGui.SetCursorPos(ImGui.GetCursorPos() + new Vector2(11f, 1f));
                        ImGui.Checkbox($"##Text_{i}_EnabledCheckbox", ref meter.Enabled);
                    }

                    if (ImGui.TableSetColumnIndex(1))
                    {
                        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3f);
                        ImGui.Text(meter.Name);
                    }

                    if (ImGui.TableSetColumnIndex(2))
                    {
                        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 1f);
                        DrawHelpers.DrawButton(
                            string.Empty,
                            FontAwesomeIcon.Pen,
                            () => EditMeter(meter),
                            "Edit",
                            buttonsize
                        );

                        ImGui.SameLine();
                        DrawHelpers.DrawButton(
                            string.Empty,
                            FontAwesomeIcon.Upload,
                            () => ExportMeter(meter),
                            "Export",
                            buttonsize
                        );

                        ImGui.SameLine();
                        DrawHelpers.DrawButton(
                            string.Empty,
                            FontAwesomeIcon.Trash,
                            () => DeleteMeter(meter),
                            "Delete",
                            buttonsize
                        );
                    }
                }

                ImGui.EndTable();
            }
        }

        private void CreateMeter(string name, MeterDataType dataType)
        {
            if (!string.IsNullOrEmpty(name))
            {
                this.Meters.Add(MeterWindow.GetDefaultMeter(dataType, name));
            }

            _input = string.Empty;
        }

        private void EditMeter(MeterWindow meter)
        {
            Singletons.Get<PluginManager>().Edit(meter);
        }

        private void DeleteMeter(MeterWindow meter)
        {
            this.Meters.Remove(meter);
        }

        private void ImportMeter(string input)
        {
            string importString = input;
            if (string.IsNullOrWhiteSpace(importString))
            {
                importString = ImGui.GetClipboardText();
            }

            MeterWindow? newMeter = ConfigHelpers.GetFromImportString<MeterWindow>(importString);
            if (newMeter is not null)
            {
                this.Meters.Add(newMeter);
            }
            else
            {
                DrawHelpers.DrawNotification("Failed to Import Meter!", NotificationType.Error);
            }

            _input = string.Empty;
        }

        private void ExportMeter(MeterWindow meter)
        {
            ConfigHelpers.ExportToClipboard(meter);
        }
    }
}
