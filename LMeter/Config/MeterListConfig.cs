using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Internal.Notifications;
using ImGuiNET;
using Newtonsoft.Json;
using LMeter.Meter;
using LMeter.Helpers;

namespace LMeter.Config
{
    public class MeterListConfig : IConfigPage
    {
        private const float MenuBarHeight = 40;

        [JsonIgnore] private string _input = string.Empty;

        public string Name => "Meters";

        public List<MeterWindow> Meters { get; init; }

        public MeterListConfig()
        {
            this.Meters = new List<MeterWindow>();
        }

        public void DrawConfig(Vector2 size, float padX, float padY)
        {
            this.DrawCreateMenu(size, padX);
            this.DrawMeterTable(size.AddY(-padY), padX);
        }

        private void DrawCreateMenu(Vector2 size, float padX)
        {
            Vector2 buttonSize = new Vector2(40, 0);
            float textInputWidth = size.X - buttonSize.X * 2 - padX * 4;

            if (ImGui.BeginChild("##Buttons", new Vector2(size.X, MenuBarHeight), true))
            {
                ImGui.PushItemWidth(textInputWidth);
                ImGui.InputTextWithHint("##Input", "Meter Name/Import String", ref _input, 10000);
                ImGui.PopItemWidth();

                ImGui.SameLine();
                DrawHelpers.DrawButton(string.Empty, FontAwesomeIcon.Plus, () => CreateMeter(_input), "Create new Meter", buttonSize);

                ImGui.SameLine();
                DrawHelpers.DrawButton(string.Empty, FontAwesomeIcon.Download, () => ImportMeter(_input), "Import new Meter", buttonSize);
                ImGui.PopItemWidth();

                ImGui.EndChild();
            }
        }

        private void DrawMeterTable(Vector2 size, float padX)
        {
            ImGuiTableFlags flags =
                ImGuiTableFlags.RowBg |
                ImGuiTableFlags.Borders |
                ImGuiTableFlags.BordersOuter |
                ImGuiTableFlags.BordersInner |
                ImGuiTableFlags.ScrollY |
                ImGuiTableFlags.NoSavedSettings;

            if (ImGui.BeginTable("##Meter_Table", 2, flags, new Vector2(size.X, size.Y - MenuBarHeight)))
            {
                Vector2 buttonsize = new Vector2(30, 0);
                float actionsWidth = buttonsize.X * 3 + padX * 2;

                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch, 0, 0);
                ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, actionsWidth, 1);

                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableHeadersRow();

                for (int i = 0; i < this.Meters.Count; i++)
                {
                    MeterWindow meter = this.Meters[i];

                    if (!string.IsNullOrEmpty(this._input) &&
                        !meter.Name.Contains(this._input, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    ImGui.PushID(i.ToString());
                    ImGui.TableNextRow(ImGuiTableRowFlags.None, 28);

                    if (ImGui.TableSetColumnIndex(0))
                    {
                        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3f);
                        ImGui.Text(meter.Name);
                    }

                    if (ImGui.TableSetColumnIndex(1))
                    {
                        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 1f);
                        DrawHelpers.DrawButton(string.Empty, FontAwesomeIcon.Pen, () => EditMeter(meter), "Edit", buttonsize);

                        ImGui.SameLine();
                        DrawHelpers.DrawButton(string.Empty, FontAwesomeIcon.Upload, () => ExportMeter(meter), "Export", buttonsize);

                        ImGui.SameLine();
                        DrawHelpers.DrawButton(string.Empty, FontAwesomeIcon.Trash, () => DeleteMeter(meter), "Delete", buttonsize);
                    }
                }

                ImGui.EndTable();
            }
        }

        private void CreateMeter(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                this.Meters.Add(new MeterWindow(name));
            }

            this._input = string.Empty;
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
            if (string.IsNullOrEmpty(importString))
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

            this._input = string.Empty;
        }

        private void ExportMeter(MeterWindow meter)
        {
            ConfigHelpers.ExportToClipboard(meter);
        }
    }
}
