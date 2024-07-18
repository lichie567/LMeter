using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiNotification;
using ImGuiNET;
using LMeter.Act.DataStructures;
using LMeter.Helpers;
using Newtonsoft.Json;

namespace LMeter.Config
{
    public class TextListConfig<T>(string name = "Texts") : IConfigPage where T : IActData<T>
    {
        [JsonIgnore] private string _textInput = string.Empty;
        [JsonIgnore] private int _selectedIndex;

        private string _name = name;
        public string Name => _name;

        public bool Initialized = false;
        public List<Text> Texts { get; init; } = [];

        public IConfigPage GetDefault()
        {
            return new TextListConfig<T>();
        }

        public void DrawConfig(Vector2 size, float padX, float padY, bool border = true)
        {
            if (this.Texts.Count == 0)
            {
                return;
            }

            if (ImGui.BeginChild($"##TextListConfig", size, border))
            {
                ImGui.Text(this.Name);
                ImGuiTableFlags tableFlags =
                    ImGuiTableFlags.RowBg |
                    ImGuiTableFlags.Borders |
                    ImGuiTableFlags.BordersOuter |
                    ImGuiTableFlags.BordersInner |
                    ImGuiTableFlags.ScrollY |
                    ImGuiTableFlags.NoSavedSettings;

                if (ImGui.BeginTable($"##TextList_Table", 5, tableFlags, new Vector2(size.X - padX * 2, (size.Y - ImGui.GetCursorPosY() - padY * 2) / 3)))
                {
                    Vector2 buttonSize = new(30, 0);
                    float actionsWidth = buttonSize.X * 3 + ImGui.GetStyle().ItemSpacing.X * 2;
                    float anchorComboWidth = 100f;

                    ImGui.TableSetupColumn("Enable", ImGuiTableColumnFlags.WidthFixed, 39, 0);
                    ImGui.TableSetupColumn("Text Name", ImGuiTableColumnFlags.WidthStretch, 0, 1);
                    ImGui.TableSetupColumn("Anchored To", ImGuiTableColumnFlags.WidthFixed, anchorComboWidth, 2);
                    ImGui.TableSetupColumn("Anchor Point", ImGuiTableColumnFlags.WidthFixed, anchorComboWidth, 3);
                    ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, actionsWidth, 4);

                    ImGui.TableSetupScrollFreeze(0, 1);
                    ImGui.TableHeadersRow();

                    int i = 0;
                    for (; i < Texts.Count; i++)
                    {
                        ImGui.PushID(i.ToString());
                        ImGui.TableNextRow(ImGuiTableRowFlags.None, 28);

                        Text text = this.Texts[i];
                        if (ImGui.TableSetColumnIndex(0))
                        {
                            ImGui.SetCursorPos(ImGui.GetCursorPos() + new Vector2(8f, 1f));
                            ImGui.Checkbox($"##Text_{i}_EnabledCheckbox", ref text.Enabled);
                        }

                        if (ImGui.TableSetColumnIndex(1))
                        {
                            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 1f);
                            ImGui.Text(text.Name);
                        }

                        if (ImGui.TableSetColumnIndex(2))
                        {
                            string[] anchorOptions = ["Bar", .. this.Texts.Select(x => x.Name)];
                            ImGui.PushItemWidth(anchorComboWidth);
                            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 1f);
                            if (ImGui.Combo($"##Text_{i}_AnchorToCombo", ref text.AnchorParent, anchorOptions, anchorOptions.Length))
                            {
                                // Check for circular dependency
                                int parent = text.AnchorParent;
                                Text t = this.Texts[Math.Clamp(parent - 1, 0, this.Texts.Count - 1)];
                                for (int j = 0; j < this.Texts.Count; j++)
                                {
                                    parent = t.AnchorParent;
                                    if (parent == 0)
                                    {
                                        break;
                                    }

                                    t = this.Texts[Math.Clamp(parent - 1, 0, this.Texts.Count - 1)];
                                }

                                if (parent != 0)
                                {
                                    text.AnchorParent = 0;
                                    DrawHelpers.DrawNotification(
                                        $"Cannot Anchor to {this.Texts[parent - 1].Name}, anchor chain must eventually anchor to Bar.",
                                        NotificationType.Error);
                                }
                            }

                            ImGui.PopItemWidth();
                        }

                        if (ImGui.TableSetColumnIndex(3))
                        {
                            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 1f);
                            ImGui.PushItemWidth(anchorComboWidth);
                            ImGui.Combo($"##Text_{i}_AnchorPointCombo", ref Unsafe.As<DrawAnchor, int>(ref text.AnchorPoint), Utils.AnchorOptions, Utils.AnchorOptions.Length);

                            ImGui.PopItemWidth();
                        }

                        if (ImGui.TableSetColumnIndex(4))
                        {
                            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 1f);
                            DrawHelpers.DrawButton(string.Empty, FontAwesomeIcon.Pen, () => SelectText(i), "Edit", buttonSize);

                            ImGui.SameLine();
                            DrawHelpers.DrawButton(string.Empty, FontAwesomeIcon.Upload, () => ExportText(text), "Export", buttonSize);

                            ImGui.SameLine();
                            DrawHelpers.DrawButton(string.Empty, FontAwesomeIcon.Trash, () => DeleteText(i), "Delete", buttonSize);
                        }
                    }

                    ImGui.PushID((i + 1).ToString());
                    ImGui.TableNextRow(ImGuiTableRowFlags.None, 28);
                    if (ImGui.TableSetColumnIndex(1))
                    {
                        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 1f);
                        ImGui.PushItemWidth(ImGui.GetColumnWidth());
                        ImGui.InputTextWithHint($"##NewTextInput", "New Text Name", ref _textInput, 10000);
                        ImGui.PopItemWidth();
                    }

                    if (ImGui.TableSetColumnIndex(4))
                    {
                        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 1f);
                        DrawHelpers.DrawButton(string.Empty, FontAwesomeIcon.Plus, () => AddText(_textInput), "Create Text", buttonSize);

                        ImGui.SameLine();
                        DrawHelpers.DrawButton(string.Empty, FontAwesomeIcon.Download, () => ImportText(), "Import Text", buttonSize);
                    }

                    ImGui.EndTable();
                }

                
                ImGui.Text($"Edit {this.Texts[_selectedIndex].Name}");
                if (ImGui.BeginChild($"##SelectedText_Edit", new(size.X - padX * 2, size.Y - ImGui.GetCursorPosY() - padY * 2), true))
                {
                    this.Texts[_selectedIndex].DrawConfig<T>();
                    ImGui.EndChild();
                }

                ImGui.EndChild();
            }
        }

        public void AddText(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                this.Texts.Add(new Text(name));
            }

            _textInput = string.Empty;
        }

        public void AddText(Text text)
        {
            this.Texts.Add(text);
            _selectedIndex = this.Texts.Count - 1;
        }

        private void SelectText(int i)
        {
            _selectedIndex = i;
        }

        private void ImportText()
        {
            string importString;
            try
            {
                importString = ImGui.GetClipboardText();
            }
            catch
            {
                DrawHelpers.DrawNotification("Failed to read from clipboard!", NotificationType.Error);
                return;
            }

            Text? newElement = ConfigHelpers.GetFromImportString<Text>(importString);

            if (newElement is Text text)
            {
                this.AddText(text);
            }
            else
            {
                DrawHelpers.DrawNotification("Failed to Import Element!", NotificationType.Error);
            }

            _textInput = string.Empty;
        }

        private void ExportText(Text text)
        {
            ConfigHelpers.ExportToClipboard(text);
        }

        private void DeleteText(int index)
        {
            foreach (Text text in this.Texts)
            {
                if (text.AnchorParent - 1 == index)
                {
                    DrawHelpers.DrawNotification(
                        $"Cannot delete {this.Texts[index].Name} while other texts are anchored to it.",
                        NotificationType.Error);
                    return;
                }
            }

            for (int i = 0; i < this.Texts.Count; i++)
            {
                if (this.Texts[i].AnchorParent > index)
                {
                    this.Texts[i].AnchorParent -= 1;
                }
            }

            this.Texts.RemoveAt(index);
            _selectedIndex = Math.Clamp(_selectedIndex, 0, this.Texts.Count - 1);
        }
    }

    public class Text(string name = "Text")
    {
        public string Name = name;
        public bool Enabled = true;
        public string TextFormat = "";
        public Vector2 TextOffset = new();
        public int AnchorParent = 0;
        public DrawAnchor AnchorPoint = DrawAnchor.Left;
        public DrawAnchor TextAlignment = DrawAnchor.Left;
        public bool ThousandsSeparators = true;
        public bool TextJobColor = false;
        public ConfigColor TextColor = new(1, 1, 1, 1);
        public bool ShowOutline = true;
        public ConfigColor OutlineColor = new(0, 0, 0, 0.5f);
        public string FontKey = FontsManager.DefaultSmallFontKey;
        public int FontId = 0;
        public bool FixedTextWidth;
        public float TextWidth = 60;
        public bool UseEllipsis = false;
        public bool ShowSeparator = false;
        public float SeparatorWidth = 2f;
        public float SeparatorHeight = .75f;
        public Vector2 SeparatorOffset = new();
        public ConfigColor SeparatorColor = new(.0f, .0f, .0f, .5f);
        
        public void DrawConfig<T>() where T : IActData<T>
        {
            Vector4 vector = Vector4.Zero;
            string[] fontOptions = FontsManager.GetFontList();
            if (fontOptions.Length == 0)
            {
                return;
            }

            ImGui.InputText("Text Name", ref this.Name, 512);
            ImGui.InputText("Text Format", ref this.TextFormat, 512);

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(Utils.GetTagsTooltip(Combatant.TextTags));
            }

            ImGui.SameLine();
            string? selectedTag = DrawHelpers.DrawTextTagsList(T.TextTags);
            if (selectedTag is not null)
            {
                this.TextFormat += selectedTag;
            }
            
            if (!FontsManager.ValidateFont(fontOptions, this.FontId, this.FontKey))
            {
                this.FontId = 0;
                for (int i = 0; i < fontOptions.Length; i++)
                {
                    if (this.FontKey.Equals(fontOptions[i]))
                    {
                        this.FontId = i;
                    }
                }
            }

            ImGui.Combo("Font##Name", ref this.FontId, fontOptions, fontOptions.Length);
            this.FontKey = fontOptions[this.FontId];

            ImGui.DragFloat2("Text Offset", ref this.TextOffset);
            ImGui.Checkbox("Fixed Text Width", ref this.FixedTextWidth);
            if (this.FixedTextWidth)
            {
                DrawHelpers.DrawNestIndicator(1);
                ImGui.DragFloat("Text Width", ref this.TextWidth, .1f, 0f, 10000f);
                DrawHelpers.DrawNestIndicator(1);
                ImGui.Combo("Text Alignment", ref Unsafe.As<DrawAnchor, int>(ref this.TextAlignment), Utils.AnchorOptions, Utils.AnchorOptions.Length);
                DrawHelpers.DrawNestIndicator(1);
                ImGui.Checkbox("Add ellipsis (...) to truncated text", ref this.UseEllipsis);
            }

            if (this.AnchorParent != 0)
            {
                ImGui.NewLine();
                ImGui.Checkbox("Show Separator", ref this.ShowSeparator);
                if (this.ShowSeparator)
                {
                    DrawHelpers.DrawNestIndicator(1);
                    ImGui.DragFloat("Height (% of Bar height)", ref this.SeparatorHeight, .1f, 0f, 1f);
                    DrawHelpers.DrawNestIndicator(1);
                    ImGui.DragFloat("Width", ref this.SeparatorWidth, .1f, 0f, 100f);
                    DrawHelpers.DrawNestIndicator(1);
                    ImGui.DragFloat2("Offset", ref this.SeparatorOffset);
                    DrawHelpers.DrawNestIndicator(1);
                    vector = this.SeparatorColor.Vector;
                    ImGui.ColorEdit4("Color", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
                    this.SeparatorColor.Vector = vector;
                }
            }

            ImGui.NewLine();
            ImGui.Checkbox("Use Job Color", ref this.TextJobColor);
            if (!this.TextJobColor)
            {
                DrawHelpers.DrawNestIndicator(1);
                vector = this.TextColor.Vector;
                ImGui.ColorEdit4("Text Color", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
                this.TextColor.Vector = vector;
            }

            ImGui.Checkbox("Show Outline", ref this.ShowOutline);
            if (this.ShowOutline)
            {
                DrawHelpers.DrawNestIndicator(1);
                vector = this.OutlineColor.Vector;
                ImGui.ColorEdit4("Outline Color", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
                this.OutlineColor.Vector = vector;
            }

            ImGui.Checkbox("Use Thousands Separator for Numbers", ref this.ThousandsSeparators);
        }
    }
}
