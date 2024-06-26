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
    public enum VisibilityOperator
    {
        And,
        Or,
        Xor
    }

    public class VisibilityOption
    {
        [JsonIgnore] private string _customJobInput = string.Empty;

        public VisibilityOperator Operator = VisibilityOperator.And;
        public bool Inverted = false;

        public bool AlwaysHide = false;
        public bool HideOutsideCombat = false;
        public bool HideOutsideDuty = false;
        public bool HideWhilePerforming = false;
        public bool HideInGoldenSaucer = false;
        public bool HideIfNotConnected = false;

        public JobType ShowForJobTypes = JobType.All;
        public string CustomJobString = string.Empty;
        public List<Job> CustomJobList = [];

        public bool IsActive()
        {
            if (this.AlwaysHide)
            {
                return false;
            }

            if (this.HideOutsideCombat && !CharacterState.IsInCombat())
            {
                return false;
            }

            if (this.HideOutsideDuty && !CharacterState.IsInDuty())
            {
                return false;
            }

            if (this.HideWhilePerforming && CharacterState.IsPerforming())
            {
                return false;
            }

            if (this.HideInGoldenSaucer && CharacterState.IsInGoldenSaucer())
            {
                return false;
            }

            if (this.HideIfNotConnected && Singletons.Get<LogClient>().Status != ConnectionStatus.Connected)
            {
                return false;
            }

            return CharacterState.IsJobType(CharacterState.GetCharacterJob(), this.ShowForJobTypes, this.CustomJobList);
        }

        public void DrawConfig(Vector2 size, float padX, float padY)
        {
            ImGui.Checkbox("Always Hide", ref this.AlwaysHide);
            ImGui.Checkbox("Hide Outside Combat", ref this.HideOutsideCombat);
            ImGui.Checkbox("Hide Outside Duty", ref this.HideOutsideDuty);
            ImGui.Checkbox("Hide While Performing", ref this.HideWhilePerforming);
            ImGui.Checkbox("Hide In Golden Saucer", ref this.HideInGoldenSaucer);
            ImGui.Checkbox("Hide While Not Connected to ACT", ref this.HideIfNotConnected);

            DrawHelpers.DrawSpacing(1);
            string[] jobTypeOptions = Enum.GetNames(typeof(JobType));
            ImGui.Combo("Show for Jobs", ref Unsafe.As<JobType, int>(ref this.ShowForJobTypes), jobTypeOptions, jobTypeOptions.Length);

            if (this.ShowForJobTypes == JobType.Custom)
            {
                if (string.IsNullOrEmpty(_customJobInput))
                {
                    _customJobInput = this.CustomJobString.ToUpper();
                }

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

    public class VisibilityConfig2 : IConfigPage
    {
        [JsonIgnore] public static readonly string[] OperatorOptions = ["AND", "OR", "XOR"];
        [JsonIgnore] private int _swapX = -1;
        [JsonIgnore] private int _swapY = -1;
        [JsonIgnore] private int _selectedIndex = 0;

        public string Name => "Visibility";
        public IConfigPage GetDefault() => new VisibilityConfig2();

        public bool Initialized = false;
        public List<VisibilityOption> VisibilityOptions = [];
        public bool ShouldClip = true;

        public void SetOldConfig(VisibilityConfig oldConfig)
        {
            VisibilityOption newOption = new();
            this.Initialized = true;
            this.ShouldClip = oldConfig.ShouldClip;
            newOption.HideIfNotConnected = oldConfig.HideIfNotConnected;

            newOption.AlwaysHide = oldConfig.AlwaysHide;
            newOption.HideOutsideCombat = oldConfig.HideOutsideCombat;
            newOption.HideWhilePerforming = oldConfig.HideWhilePerforming;
            newOption.HideOutsideDuty = oldConfig.HideOutsideDuty;
            newOption.HideInGoldenSaucer = oldConfig.HideInGoldenSaucer;
            
            newOption.ShowForJobTypes = oldConfig.ShowForJobTypes;
            newOption.CustomJobList = oldConfig.CustomJobList;
            newOption.CustomJobString = oldConfig.CustomJobString;
            this.AddOption(newOption);
        }

        public bool IsVisible()
        {
            if (this.VisibilityOptions.Count == 0)
            {
                return false;
            }

            bool active = this.VisibilityOptions[0].IsActive();
            for (int i = 1; i < this.VisibilityOptions.Count; i++)
            {
                VisibilityOption option = this.VisibilityOptions[i];
                bool currentActive = option.IsActive();

                if (option.Inverted)
                {
                    currentActive = !currentActive;
                }

                active = option.Operator switch
                {
                    VisibilityOperator.And => active && currentActive,
                    VisibilityOperator.Or => active || currentActive,
                    VisibilityOperator.Xor => active ^ currentActive,
                    _ => false
                };
            }

            return active;
        }

        public void DrawConfig(Vector2 size, float padX, float padY)
        {
            if (this.VisibilityOptions.Count == 0)
            {
                this.AddOption(new());
            }

            float posY = ImGui.GetCursorPosY();
            ImGui.Checkbox("Hide When Covered by Game UI Window", ref this.ShouldClip);
            size = new(size.X, size.Y - (ImGui.GetCursorPosY() - posY));

            if (ImGui.BeginChild("##VisibilityOptionConfig", new Vector2(size.X, size.Y), true))
            {
                ImGui.Text("Visibility Options");
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
                    int buttonCount = this.VisibilityOptions.Count > 1 ? 5 : 3;
                    float actionsWidth = buttonSize.X * buttonCount + padX * (buttonCount - 1);
                    ImGui.TableSetupColumn("Condition", ImGuiTableColumnFlags.WidthFixed, 60, 0);
                    ImGui.TableSetupColumn("Invert", ImGuiTableColumnFlags.WidthFixed, 35, 1);
                    ImGui.TableSetupColumn("Option Name", ImGuiTableColumnFlags.WidthStretch, 0, 2);
                    ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, actionsWidth, 3);
                    ImGui.TableSetupScrollFreeze(0, 1);
                    ImGui.TableHeadersRow();

                    for (int i = 0; i < this.VisibilityOptions.Count; i++)
                    {
                        ImGui.PushID(i.ToString());
                        ImGui.TableNextRow(ImGuiTableRowFlags.None, 28);

                        this.DrawOptionsRow(i);
                    }

                    ImGui.PushID(this.VisibilityOptions.Count.ToString());
                    ImGui.TableNextRow(ImGuiTableRowFlags.None, 28);
                    ImGui.TableSetColumnIndex(3);
                    DrawHelpers.DrawButton(string.Empty, FontAwesomeIcon.Plus, () => this.AddOption(), "New Option", buttonSize);
                    ImGui.SameLine();
                    DrawHelpers.DrawButton(string.Empty, FontAwesomeIcon.Download, () => this.ImportOption(), "Import Option", buttonSize);

                    ImGui.EndTable();

                    if (_swapX < this.VisibilityOptions.Count && _swapX >= 0 &&
                        _swapY < this.VisibilityOptions.Count && _swapY >= 0)
                    {
                        VisibilityOption temp = this.VisibilityOptions[_swapX];
                        this.VisibilityOptions[_swapX] = this.VisibilityOptions[_swapY];
                        this.VisibilityOptions[_swapY] = temp;

                        _swapX = -1;
                        _swapY = -1;
                    }
                }

                ImGui.Text($"Edit Option {_selectedIndex + 1}");
                if (ImGui.BeginChild("##OptionEdit", new Vector2(size.X - padX * 2, size.Y - ImGui.GetCursorPosY() - padY * 2), true))
                {
                    VisibilityOption selectedOption = this.VisibilityOptions[_selectedIndex];
                    selectedOption.DrawConfig(ImGui.GetWindowSize(), padX, padX);
                    
                    ImGui.EndChild();
                }

                ImGui.EndChild();
            }
        }

        private void DrawOptionsRow(int i)
        {
            if (i >= this.VisibilityOptions.Count)
            {
                return;
            }

            VisibilityOption option = this.VisibilityOptions[i];
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
                    ImGui.Combo("##CondCombo", ref Unsafe.As<VisibilityOperator, int>(ref option.Operator), OperatorOptions, OperatorOptions.Length);
                    ImGui.PopItemWidth();
                }
            }

            if (ImGui.TableSetColumnIndex(1))
            {
                ImGui.SetCursorPos(ImGui.GetCursorPos() + new Vector2(1f, 5f));
                ImGui.Checkbox(string.Empty, ref option.Inverted);
            }

            if (ImGui.TableSetColumnIndex(2))
            {
                ImGui.Text($"Option {i + 1}");
            }

            if (ImGui.TableSetColumnIndex(3))
            {
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 1f);
                Vector2 buttonSize = new(30, 0);
                DrawHelpers.DrawButton(string.Empty, FontAwesomeIcon.Pen, () => this.SelectOption(i), "Edit Option", buttonSize);

                if (this.VisibilityOptions.Count > 1)
                {
                    ImGui.SameLine();
                    DrawHelpers.DrawButton(string.Empty, FontAwesomeIcon.ArrowUp, () => this.Swap(i, i - 1), "Move Up", buttonSize);

                    ImGui.SameLine();
                    DrawHelpers.DrawButton(string.Empty, FontAwesomeIcon.ArrowDown, () => this.Swap(i, i + 1), "Move Down", buttonSize);
                }

                ImGui.SameLine();
                DrawHelpers.DrawButton(string.Empty, FontAwesomeIcon.Upload, () => this.ExportOption(i), "Export Option", buttonSize);
                if (this.VisibilityOptions.Count > 1)
                {
                    ImGui.SameLine();
                    DrawHelpers.DrawButton(string.Empty, FontAwesomeIcon.Trash, () => this.RemoveOption(i), "Remove Option", buttonSize);
                }
            }
        }

        private void SelectOption(int i)
        {
            _selectedIndex = i;
        }

        private void AddOption(VisibilityOption? newOption = null)
        {
            this.VisibilityOptions.Add(newOption ?? new VisibilityOption());
            this.SelectOption(this.VisibilityOptions.Count - 1);
        }

        private void ExportOption(int i)
        {
            if (i < this.VisibilityOptions.Count && i >= 0)
            {
                ConfigHelpers.ExportToClipboard<VisibilityOption>(this.VisibilityOptions[i]);
            }
        }

        private void ImportOption()
        {
            string importString = ImGui.GetClipboardText();
            if (!string.IsNullOrEmpty(importString))
            {
                VisibilityOption? newOption = ConfigHelpers.GetFromImportString<VisibilityOption>(importString);
                if (newOption is not null)
                {
                    this.AddOption(newOption);
                }
            }
        }

        private void RemoveOption(int i)
        {
            if (i < this.VisibilityOptions.Count && i >= 0)
            {
                this.VisibilityOptions.RemoveAt(i);
                _selectedIndex = Math.Clamp(_selectedIndex, 0, this.VisibilityOptions.Count - 1);
            }
        }

        private void Swap(int x, int y)
        {
            _swapX = x;
            _swapY = y;
        }
    }
}