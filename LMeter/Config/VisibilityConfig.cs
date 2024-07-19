using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Dalamud.Interface;
using ImGuiNET;
using LMeter.Act;
using LMeter.Helpers;
using Newtonsoft.Json;

namespace LMeter.Config
{
    public class VisibilityConfig : IConfigPage
    {
        [JsonIgnore] public static readonly string[] OperatorOptions = [ "AND", "OR", "XOR" ];
        [JsonIgnore] public static readonly string[] ResultOptions = [ "Show", "Hide" ];
        [JsonIgnore] private int _swapX = -1;
        [JsonIgnore] private int _swapY = -1;
        [JsonIgnore] private int _selectedIndex = 0;
        
        [JsonIgnore]
        public bool Active { get; set; }

        public string Name => "Visibility";

        public List<VisibilityCondition> VisibilityConditions = [];
        public bool Initialized = false;
        public bool AlwaysHide = false;
        public bool ShouldClip = true;
        public bool ShowOnMouseover = false;
        public bool HideIfNotConnected = false;
        public int ResultOption = 0;

        public VisibilityConfig()
        {
            this.VisibilityConditions = [ new() ];
        }

        public IConfigPage GetDefault() => new VisibilityConfig();

        public bool IsVisible()
        {
            if (this.VisibilityConditions.Count == 0)
            {
                return false;
            }

            if (this.AlwaysHide)
            {
                return false;
            }

            if (this.HideIfNotConnected && Singletons.Get<LogClient>().Status != ConnectionStatus.Connected)
            {
                return false;
            }

            bool active = this.VisibilityConditions[0].IsActive();
            for (int i = 1; i < this.VisibilityConditions.Count; i++)
            {
                VisibilityCondition option = this.VisibilityConditions[i];
                bool currentActive = option.IsActive();

                active = option.Operator switch
                {
                    BooleanOperator.And => active && currentActive,
                    BooleanOperator.Or => active || currentActive,
                    BooleanOperator.Xor => active ^ currentActive,
                    _ => false
                };
            }

            return active ^ this.ResultOption == 1;
        }

        public void DrawConfig(Vector2 size, float padX, float padY, bool border = true)
        {
            if (this.VisibilityConditions.Count == 0)
            {
                this.AddOption(new());
            }

            float posY = ImGui.GetCursorPosY();
            ImGui.Checkbox("Always Hide", ref this.AlwaysHide);
            ImGui.Checkbox("Hide When Covered by Game UI Elements", ref this.ShouldClip);
            ImGui.Checkbox("Always Show When Hovered by Mouse", ref this.ShowOnMouseover);
            ImGui.Checkbox("Always Hide When Not Connected to ACT", ref this.HideIfNotConnected);
            size = new(size.X, size.Y - (ImGui.GetCursorPosY() - posY));

            if (ImGui.BeginChild("##VisibilityOptionConfig", new Vector2(size.X, size.Y), border))
            {
                ImGui.Text("Visibility Conditions");

                ImGuiTableFlags tableFlags =
                    ImGuiTableFlags.RowBg |
                    ImGuiTableFlags.Borders |
                    ImGuiTableFlags.BordersOuter |
                    ImGuiTableFlags.BordersInner |
                    ImGuiTableFlags.ScrollY |
                    ImGuiTableFlags.NoSavedSettings;

                if (ImGui.BeginTable("##VisibilityOptions_Table", 4, tableFlags, new Vector2(size.X - padX * 2, (size.Y - ImGui.GetCursorPosY() - padY * 2) / 4)))
                {
                    Vector2 buttonSize = new(30, 0);
                    int buttonCount = this.VisibilityConditions.Count > 1 ? 5 : 3;
                    float actionsWidth = buttonSize.X * buttonCount + padX * (buttonCount - 1);
                    ImGui.TableSetupColumn("Operator", ImGuiTableColumnFlags.WidthFixed, 60, 0);
                    ImGui.TableSetupColumn("Invert", ImGuiTableColumnFlags.WidthFixed, 35, 1);
                    ImGui.TableSetupColumn("Condition Type", ImGuiTableColumnFlags.WidthStretch, 0, 2);
                    ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, actionsWidth, 3);
                    ImGui.TableSetupScrollFreeze(0, 1);
                    ImGui.TableHeadersRow();

                    for (int i = 0; i < this.VisibilityConditions.Count; i++)
                    {
                        ImGui.PushID(i.ToString());
                        ImGui.TableNextRow(ImGuiTableRowFlags.None, 28);
                        this.DrawOptionsRow(i);
                    }

                    ImGui.PushID(this.VisibilityConditions.Count.ToString());
                    ImGui.TableNextRow(ImGuiTableRowFlags.None, 28);
                    ImGui.TableSetColumnIndex(3);
                    DrawHelpers.DrawButton(string.Empty, FontAwesomeIcon.Plus, () => this.AddOption(), "New Condition", buttonSize);
                    ImGui.SameLine();
                    DrawHelpers.DrawButton(string.Empty, FontAwesomeIcon.Download, () => this.ImportOption(), "Import Condition", buttonSize);

                    ImGui.EndTable();

                    if (_swapX < this.VisibilityConditions.Count && _swapX >= 0 &&
                        _swapY < this.VisibilityConditions.Count && _swapY >= 0)
                    {
                        VisibilityCondition temp = this.VisibilityConditions[_swapX];
                        this.VisibilityConditions[_swapX] = this.VisibilityConditions[_swapY];
                        this.VisibilityConditions[_swapY] = temp;

                        _swapX = -1;
                        _swapY = -1;
                    }
                }

                ImGui.Text("Action if result is true:");
                ImGui.SameLine();
                ImGui.PushItemWidth(100);
                ImGui.SetCursorPos(ImGui.GetCursorPos() + new Vector2(0f, -2f));
                ImGui.Combo("##ResultCombo", ref this.ResultOption, ResultOptions, ResultOptions.Length);
                ImGui.PopItemWidth();

                ImGui.Text($"Edit Condition {_selectedIndex + 1}");
                if (ImGui.BeginChild("##ConditionEdit", new Vector2(size.X - padX * 2, size.Y - ImGui.GetCursorPosY() - padY * 2), true))
                {
                    VisibilityCondition selectedOption = this.VisibilityConditions[_selectedIndex];
                    selectedOption.DrawConfig(ImGui.GetWindowSize(), padX, padX);
                    
                    ImGui.EndChild();
                }

                ImGui.EndChild();
            }
        }

        private void DrawOptionsRow(int i)
        {
            if (i >= this.VisibilityConditions.Count)
            {
                return;
            }

            VisibilityCondition condition = this.VisibilityConditions[i];
            if (ImGui.TableSetColumnIndex(0))
            {
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (i == 0 ? 3f : 1f));
                if (i == 0)
                {
                    ImGui.Text("IF");
                }
                else
                {
                    ImGui.PushItemWidth(ImGui.GetColumnWidth());
                    ImGui.Combo("##CondCombo", ref Unsafe.As<BooleanOperator, int>(ref condition.Operator), OperatorOptions, OperatorOptions.Length);
                    ImGui.PopItemWidth();
                }
            }

            if (ImGui.TableSetColumnIndex(1))
            {
                ImGui.Checkbox(string.Empty, ref condition.Inverted);
            }

            if (ImGui.TableSetColumnIndex(2))
            {
                ImGui.Text(condition.ConditionType == VisibilityConditionType.Zone ? $"Zone: {condition.Zone}" : $"{condition.ConditionType}");
            }

            if (ImGui.TableSetColumnIndex(3))
            {
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 1f);
                Vector2 buttonSize = new(30, 0);
                DrawHelpers.DrawButton(string.Empty, FontAwesomeIcon.Pen, () => this.SelectOption(i), "Edit Condition", buttonSize);

                if (this.VisibilityConditions.Count > 1)
                {
                    ImGui.SameLine();
                    DrawHelpers.DrawButton(string.Empty, FontAwesomeIcon.ArrowUp, () => this.Swap(i, i - 1), "Move Up", buttonSize);

                    ImGui.SameLine();
                    DrawHelpers.DrawButton(string.Empty, FontAwesomeIcon.ArrowDown, () => this.Swap(i, i + 1), "Move Down", buttonSize);
                }

                ImGui.SameLine();
                DrawHelpers.DrawButton(string.Empty, FontAwesomeIcon.Upload, () => this.ExportOption(i), "Export Condition", buttonSize);
                if (this.VisibilityConditions.Count > 1)
                {
                    ImGui.SameLine();
                    DrawHelpers.DrawButton(string.Empty, FontAwesomeIcon.Trash, () => this.RemoveOption(i), "Remove Condition", buttonSize);
                }
            }
        }

        private void SelectOption(int i)
        {
            _selectedIndex = i;
        }

        private void AddOption(VisibilityCondition? newOption = null)
        {
            this.VisibilityConditions.Add(newOption ?? new VisibilityCondition());
            this.SelectOption(this.VisibilityConditions.Count - 1);
        }

        private void ExportOption(int i)
        {
            if (i < this.VisibilityConditions.Count && i >= 0)
            {
                ConfigHelpers.ExportToClipboard(this.VisibilityConditions[i]);
            }
        }

        private void ImportOption()
        {
            string importString = ImGui.GetClipboardText();
            if (!string.IsNullOrEmpty(importString))
            {
                VisibilityCondition? newOption = ConfigHelpers.GetFromImportString<VisibilityCondition>(importString);
                if (newOption is not null)
                {
                    this.AddOption(newOption);
                }
            }
        }

        private void RemoveOption(int i)
        {
            if (i < this.VisibilityConditions.Count && i >= 0)
            {
                this.VisibilityConditions.RemoveAt(i);
                _selectedIndex = Math.Clamp(_selectedIndex, 0, this.VisibilityConditions.Count - 1);
            }
        }

        private void Swap(int x, int y)
        {
            _swapX = x;
            _swapY = y;
        }
    }
    
    public class VisibilityCondition
    {
        [JsonIgnore] private string _customJobInput = string.Empty;

        public bool Inverted = false;
        public BooleanOperator Operator = BooleanOperator.And;
        public VisibilityConditionType ConditionType = VisibilityConditionType.AlwaysTrue;

        public JobType ShowForJobTypes = JobType.All;
        public string CustomJobString = string.Empty;
        public List<Job> CustomJobList = [];

        public ZoneType Zone;

        public bool IsActive() => this.Inverted ^ this.ConditionType switch
        {
            VisibilityConditionType.AlwaysTrue => true,
            VisibilityConditionType.InCombat => CharacterState.IsInCombat(),
            VisibilityConditionType.InDuty => CharacterState.IsInDuty(),
            VisibilityConditionType.Performing => CharacterState.IsPerforming(),
            VisibilityConditionType.Zone => CharacterState.InZone(this.Zone),
            VisibilityConditionType.Job => CharacterState.IsJobType(CharacterState.GetCharacterJob(), this.ShowForJobTypes, this.CustomJobList),
            _ => true
        };

        public void DrawConfig(Vector2 size, float padX, float padY)
        {
            ImGui.RadioButton("Always True", ref Unsafe.As<VisibilityConditionType, int>(ref this.ConditionType), (int)VisibilityConditionType.AlwaysTrue);
            ImGui.RadioButton("In Combat", ref Unsafe.As<VisibilityConditionType, int>(ref this.ConditionType), (int)VisibilityConditionType.InCombat);
            ImGui.RadioButton("In Duty", ref Unsafe.As<VisibilityConditionType, int>(ref this.ConditionType), (int)VisibilityConditionType.InDuty);
            ImGui.RadioButton("Performing", ref Unsafe.As<VisibilityConditionType, int>(ref this.ConditionType), (int)VisibilityConditionType.Performing);

            ImGui.RadioButton("In Zone", ref Unsafe.As<VisibilityConditionType, int>(ref this.ConditionType), (int)VisibilityConditionType.Zone);
            if (this.ConditionType == VisibilityConditionType.Zone)
            {
                DrawHelpers.DrawNestIndicator(1);
                ImGui.RadioButton("Gold Saucer", ref Unsafe.As<ZoneType, int>(ref this.Zone), (int)ZoneType.GoldSaucer);
                DrawHelpers.DrawNestIndicator(1);
                ImGui.RadioButton("Player House", ref Unsafe.As<ZoneType, int>(ref this.Zone), (int)ZoneType.PlayerHouse);
            }

            ImGui.RadioButton("Job", ref Unsafe.As<VisibilityConditionType, int>(ref this.ConditionType), (int)VisibilityConditionType.Job);
            if (this.ConditionType == VisibilityConditionType.Job)
            {
                DrawHelpers.DrawNestIndicator(1);
                string[] jobTypeOptions = Enum.GetNames(typeof(JobType));
                ImGui.Combo("Job Select", ref Unsafe.As<JobType, int>(ref this.ShowForJobTypes), jobTypeOptions, jobTypeOptions.Length);

                if (this.ShowForJobTypes == JobType.Custom)
                {
                    if (string.IsNullOrEmpty(_customJobInput))
                    {
                        _customJobInput = this.CustomJobString.ToUpper();
                    }

                    DrawHelpers.DrawNestIndicator(1);
                    if (ImGui.InputTextWithHint("Custom Job List", "Comma Separated List (ex: WAR, SAM, BLM)", ref _customJobInput, 100, ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        IEnumerable<string> jobStrings = _customJobInput.Split(',').Select(j => j.Trim());
                        List<Job> jobList = [];
                        foreach (string j in jobStrings)
                        {
                            if (Enum.TryParse(j, true, out Job parsed))
                            {
                                jobList.Add(parsed);
                            }
                            else
                            {
                                jobList.Clear();
                                _customJobInput = string.Empty;
                                break;
                            }
                        }

                        _customJobInput = _customJobInput.ToUpper();
                        this.CustomJobString = _customJobInput;
                        this.CustomJobList = jobList;
                    }
                }
            }
        }
    }
}