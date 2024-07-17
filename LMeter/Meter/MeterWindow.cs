using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Plugin.Services;
using ImGuiNET;
using LMeter.Act;
using LMeter.Act.DataStructures;
using LMeter.Config;
using LMeter.Helpers;
using Newtonsoft.Json;

namespace LMeter.Meter
{
    public class MeterWindow : IConfigurable
    {
        [JsonIgnore] private bool _lastFrameWasUnlocked = false;
        [JsonIgnore] private bool _lastFrameWasDragging = false;
        [JsonIgnore] private bool _lastFrameWasPreview = false;
        [JsonIgnore] private bool _lastFrameWasCombat = false;
        [JsonIgnore] private bool _unlocked = false;
        [JsonIgnore] private bool _hovered = false;
        [JsonIgnore] private bool _dragging = false;
        [JsonIgnore] private bool _locked = false;
        [JsonIgnore] private int _eventIndex = -1;
        [JsonIgnore] private ActEvent? _previewEvent = null;
        [JsonIgnore] private int _scrollPosition = 0;
        [JsonIgnore] private DateTime? _lastSortedTimestamp = null;
        [JsonIgnore] private List<Combatant> _lastSortedCombatants = [];
        [JsonIgnore] public string Id { get; init; }

        public string Name { get; set; }

        public GeneralConfig GeneralConfig { get; set; }
        public HeaderConfig HeaderConfig { get; set; }
        public TextListConfig<Encounter> HeaderTextConfig { get; set; }
        public BarConfig BarConfig { get; set; }
        public TextListConfig<Combatant> BarTextConfig { get; set; }
        public BarColorsConfig BarColorsConfig { get; set; }
        public VisibilityConfig VisibilityConfig { get; set; }

        public MeterWindow(string name)
        {
            this.Name = name;
            this.Id = $"LMeter_MeterWindow_{Guid.NewGuid()}";
            this.GeneralConfig = new GeneralConfig();
            this.HeaderConfig = new HeaderConfig();
            this.HeaderTextConfig = new("Header Texts");
            this.BarConfig = new BarConfig();
            this.BarTextConfig = new("Bar Texts");
            this.BarColorsConfig = new BarColorsConfig();
            this.VisibilityConfig = new VisibilityConfig();
        }

        public IEnumerable<IConfigPage> GetConfigPages()
        {
            yield return this.GeneralConfig;
            yield return this.HeaderConfig;
            yield return this.HeaderTextConfig;
            yield return this.BarConfig;
            yield return this.BarTextConfig;
            yield return this.BarColorsConfig;
            yield return this.VisibilityConfig;
        }

        public void ImportPage(IConfigPage page)
        {
            switch (page)
            {
                case GeneralConfig newPage:
                    this.GeneralConfig = newPage;
                    break;
                case HeaderConfig newPage:
                    this.HeaderConfig = newPage;
                    break;
                case TextListConfig<Encounter> newPage:
                        this.HeaderTextConfig = newPage;
                    break;
                case BarConfig newPage:
                    this.BarConfig = newPage;
                    break;
                case TextListConfig<Combatant> newPage:
                        this.BarTextConfig = newPage;
                    break;
                case BarColorsConfig newPage:
                    this.BarColorsConfig = newPage;
                    break;
                case VisibilityConfig newPage:
                    this.VisibilityConfig = newPage;
                    break;
            }
        }

        public static MeterWindow GetDefaultMeter(string name)
        {
            MeterWindow newMeter = new(name);
            newMeter.ImportPage(newMeter.HeaderConfig.GetDefault());
            newMeter.ImportPage(newMeter.BarConfig.GetDefault());
            return newMeter;
        }

        public void Clear()
        {
            _lastSortedCombatants = [];
            _lastSortedTimestamp = null;
        }

        // Dont ask
        protected void UpdateDragData(Vector2 pos, Vector2 size, bool locked)
        {
            _unlocked = !locked;
            _hovered = ImGui.IsMouseHoveringRect(pos, pos + size);
            _dragging = _lastFrameWasDragging && ImGui.IsMouseDown(ImGuiMouseButton.Left);
            _locked = (_unlocked && !_lastFrameWasUnlocked || !_hovered) && !_dragging;
            _lastFrameWasDragging = _hovered || _dragging;
        }

        private bool ShouldDraw(Vector2 pos, Vector2 size)
        {
            if (_dragging)
            {
                return true;
            }

            if (!this.GeneralConfig.Preview && !this.VisibilityConfig.IsVisible() &&
                !(this.VisibilityConfig.ShowOnMouseover && ImGui.IsMouseHoveringRect(pos, pos + size)))
            {
                return false;
            }

            if (this.VisibilityConfig.ShouldClip &&
                    Singletons.Get<ClipRectsHelper>().GetClipRectForArea(pos, size).HasValue)
            {
                return false;
            }

            return true;
        }

        public void Draw(Vector2 pos)
        {
            Vector2 localPos = pos + this.GeneralConfig.Position;
            Vector2 size = this.GeneralConfig.Size;

            if (ImGui.IsMouseHoveringRect(localPos, localPos + size))
            {
                _scrollPosition -= (int)ImGui.GetIO().MouseWheel;

                if (ImGui.IsMouseClicked(ImGuiMouseButton.Right) && !this.GeneralConfig.Preview)
                {
                    ImGui.OpenPopup($"{this.Id}_ContextMenu", ImGuiPopupFlags.MouseButtonRight);
                }
            }

            bool contextMenuOpen = this.DrawContextMenu($"{this.Id}_ContextMenu", out bool selected, out int index);
            if (contextMenuOpen && selected)
            {
                _eventIndex = index;
                _lastSortedTimestamp = null;
                _lastSortedCombatants = [];
                _scrollPosition = 0;
            }
            
            if (!contextMenuOpen && !this.ShouldDraw(localPos, size))
            {
                return;
            }

            bool combat = CharacterState.IsInCombat();
            if (this.GeneralConfig.ReturnToCurrent && !_lastFrameWasCombat && combat)
            {
                _eventIndex = -1;
            }

            this.UpdateDragData(localPos, size, this.GeneralConfig.Lock);
            bool needsInput = !this.GeneralConfig.ClickThrough;
            DrawHelpers.DrawInWindow($"##{this.Id}", localPos, size, needsInput, _locked || this.GeneralConfig.Lock, (drawList) =>
            {
                if (_unlocked)
                {
                    if (_lastFrameWasDragging)
                    {
                        localPos = ImGui.GetWindowPos();
                        this.GeneralConfig.Position = localPos - pos;

                        size = ImGui.GetWindowSize();
                        this.GeneralConfig.Size = size;
                    }
                }

                if (this.GeneralConfig.ShowBorder)
                {
                    Vector2 borderPos = localPos;
                    Vector2 borderSize = size;
                    if (this.GeneralConfig.BorderAroundBars &&
                        this.HeaderConfig.ShowHeader)
                    {
                        borderPos = borderPos.AddY(this.HeaderConfig.HeaderHeight);
                        borderSize = borderSize.AddY(-this.HeaderConfig.HeaderHeight);
                    }

                    for (int i = 0; i < this.GeneralConfig.BorderThickness; i++)
                    {
                        Vector2 offset = new(i, i);
                        drawList.AddRect(borderPos + offset, borderPos + borderSize - offset, this.GeneralConfig.BorderColor.Base);
                    }

                    localPos += Vector2.One * this.GeneralConfig.BorderThickness;
                    size -= Vector2.One * this.GeneralConfig.BorderThickness * 2;
                }

                ImGui.PushClipRect(localPos, localPos + size, false);
                if (this.GeneralConfig.Preview && !_lastFrameWasPreview)
                {
                    _previewEvent = ActEvent.GetTestData();
                }

                ActEvent? actEvent = this.GeneralConfig.Preview ? _previewEvent : Singletons.Get<LogClient>().GetEvent(_eventIndex);
                (localPos, size) = this.DrawHeader(drawList, localPos, size, actEvent?.Encounter);
                drawList.AddRectFilled(localPos, localPos + size, this.GeneralConfig.BackgroundColor.Base);
                this.DrawBars(drawList, localPos, size, actEvent);
                ImGui.PopClipRect();
            });

            _lastFrameWasUnlocked = _unlocked;
            _lastFrameWasPreview = this.GeneralConfig.Preview;
            _lastFrameWasCombat = combat;
        }

        private (Vector2, Vector2) DrawHeader(
            ImDrawListPtr drawList,
            Vector2 pos,
            Vector2 size,
            Encounter? encounter)
        {
            HeaderConfig headerConfig = this.HeaderConfig;
            TextListConfig<Encounter> headerTextConfig = this.HeaderTextConfig;
            if (!headerConfig.ShowHeader)
            {
                return (pos, size);
            }

            Vector2 headerSize = new(size.X, headerConfig.HeaderHeight);
            drawList.AddRectFilled(pos, pos + headerSize, headerConfig.BackgroundColor.Base);

            if (encounter is null)
            {
                using (FontsManager.PushFont(FontsManager.DefaultSmallFontKey))
                {
                    string version = $" LMeter v{Plugin.Version} ";
                    Vector2 versionSize = ImGui.CalcTextSize(version);
                    Vector2 versionPos = Utils.GetAnchoredPosition(pos, -headerSize, DrawAnchor.Left);
                    versionPos = Utils.GetAnchoredPosition(versionPos, versionSize, DrawAnchor.Left);
                    DrawHelpers.DrawText(drawList, version, versionPos, headerConfig.DurationColor.Base, headerConfig.DurationShowOutline, headerConfig.DurationOutlineColor.Base);
                }
            }
            else
            {
                DrawBarTexts(drawList, headerTextConfig.Texts, pos, headerSize, this.BarColorsConfig.GetColor(CharacterState.GetCharacterJob()), encounter);
            }

            return (pos.AddY(headerConfig.HeaderHeight), size.AddY(-headerConfig.HeaderHeight));
        }

        private void DrawBars(ImDrawListPtr drawList, Vector2 localPos, Vector2 size, ActEvent? actEvent)
        {
            if (actEvent?.Combatants is not null && actEvent.Combatants.Count != 0)
            {
                // We don't want to corrupt the cache. The entire logic past this point mutates the sorted Act combatants instead of using a rendering cache
                // This has the issue that some settings can't behave properly and or don't update till the following combat update/fight
                List<Combatant> sortedCombatants = [.. this.GetSortedCombatants(actEvent, this.GeneralConfig.DataType)];

                float top = this.GeneralConfig.DataType switch
                {
                    MeterDataType.Damage => sortedCombatants[0].DamageTotal?.Value ?? 0,
                    MeterDataType.Healing => sortedCombatants[0].EffectiveHealing?.Value ?? 0,
                    MeterDataType.DamageTaken => sortedCombatants[0].DamageTaken?.Value ?? 0,
                    _ => 0
                };

                int currentIndex = 0;
                string playerName = Singletons.Get<IClientState>().LocalPlayer?.Name.ToString() ?? "YOU";

                if (sortedCombatants.Count > this.BarConfig.BarCount)
                {
                    currentIndex = Math.Clamp(_scrollPosition, 0, sortedCombatants.Count - this.BarConfig.BarCount);
                    _scrollPosition = currentIndex;

                    if (this.BarConfig.AlwaysShowSelf)
                    {
                        MovePlayerIntoViewableRange(sortedCombatants, _scrollPosition, playerName);
                    }
                }

                int maxIndex = Math.Min(currentIndex + this.BarConfig.BarCount, sortedCombatants.Count);
                for (; currentIndex < maxIndex; currentIndex++)
                {
                    Combatant combatant = sortedCombatants[currentIndex];
                    combatant.Rank = (currentIndex + 1).ToString();
                    UpdatePlayerName(combatant, playerName);

                    float current = this.GeneralConfig.DataType switch
                    {
                        MeterDataType.Damage => combatant.DamageTotal?.Value ?? 0,
                        MeterDataType.Healing => combatant.EffectiveHealing?.Value ?? 0,
                        MeterDataType.DamageTaken => combatant.DamageTaken?.Value ?? 0,
                        _ => 0
                    };

                    ConfigColor barColor = this.BarConfig.BarColor;
                    ConfigColor jobColor = this.BarColorsConfig.GetColor(combatant.Job);
                    localPos = this.DrawBar(drawList, localPos, size, combatant, jobColor, barColor, top, current);
                }
            }
        }

        private Vector2 DrawBar(
            ImDrawListPtr drawList,
            Vector2 localPos,
            Vector2 size,
            Combatant combatant,
            ConfigColor jobColor,
            ConfigColor barColor,
            float top,
            float current)
        {
            BarConfig barConfig = this.BarConfig;
            float barHeight = barConfig.BarHeightType == 0
                ? (size.Y - (barConfig.BarCount - 1) * barConfig.BarGaps) / barConfig.BarCount
                : barConfig.BarHeight;

            Vector2 barSize = new(size.X, barHeight);
            Vector2 barFillSize = new(size.X * (current / top), barHeight);
            drawList.AddRectFilled(localPos, localPos + barFillSize, barConfig.UseJobColor ? jobColor.Base : barColor.Base);

            if (barConfig.ShowJobIcon && combatant.Job != Job.UKN)
            {
                uint jobIconId = 62000u + (uint)combatant.Job + 100u * (uint)barConfig.JobIconStyle;
                Vector2 jobIconSize = barConfig.JobIconSizeType == 0 ? Vector2.One * barHeight : barConfig.JobIconSize;
                DrawHelpers.DrawIcon(jobIconId, localPos + barConfig.JobIconOffset, jobIconSize, drawList);
            }

            DrawBarTexts(drawList, this.BarTextConfig.Texts, localPos, barSize, jobColor, combatant);
            return localPos.AddY(barHeight + barConfig.BarGaps);
        }

        private static void DrawBarTexts<T>(
            ImDrawListPtr drawList,
            List<Text> texts,
            Vector2 parentPos,
            Vector2 parentSize,
            ConfigColor jobColor,
            IActData<T> actData)
        {
            Dictionary<int, (Vector2, Vector2)> lookup = new() { { 0, (parentPos, parentSize) } };
            for (int i = 0; i < texts.Count + 1; i++)
            {
                for (int j = 0; j < texts.Count; j++)
                {
                    Text text = texts[j];
                    if (text.AnchorParent == i)
                    {
                        using (FontsManager.PushFont(text.FontKey))
                        {
                            string formattedText = actData.GetFormattedString($" {text.TextFormat} ", text.ThousandsSeparators ? "N" : "F");
                            Vector2 textSize = ImGui.CalcTextSize(formattedText);
                            
                            if (text.FixedTextWidth && textSize.X > text.TextWidth)
                            {
                                float ellipsisWidth = text.UseEllipsis ? ImGui.CalcTextSize("... ").X : ImGui.CalcTextSize(" ").X;
                                do
                                {
                                    formattedText = formattedText.AsSpan(0, formattedText.Length - 1).ToString();
                                    textSize = ImGui.CalcTextSize(formattedText);
                                }
                                while (textSize.X + ellipsisWidth > text.TextWidth && formattedText.Length > 1);
                                formattedText += text.UseEllipsis ? "... " : " ";
                                textSize = ImGui.CalcTextSize(formattedText);
                            }
                            
                            Vector2 textBoxSize = new(text.FixedTextWidth ? text.TextWidth : textSize.X, textSize.Y);
                            Vector2 anchorPoint = Utils.GetAnchoredPosition(lookup[text.AnchorParent].Item1, -lookup[text.AnchorParent].Item2, text.AnchorPoint);
                            Vector2 textBoxPos = Utils.GetTopLeft(anchorPoint, textBoxSize, text.AnchorParent == 0 ? text.AnchorPoint.Opposite() : text.AnchorPoint);
                            
                            textBoxPos += text.TextOffset;
                            DrawAnchor alignment = text.FixedTextWidth ? text.TextAlignment : text.AnchorPoint;
                            Vector2 textPos = Utils.GetTextPos(textBoxPos, textBoxSize, textSize, alignment);
                            lookup.Add(j + 1, (textBoxPos, textBoxSize));

                            if (text.ShowSeparator)
                            {
                                Vector2 separatorSize = new(text.SeparatorWidth, parentSize.Y * text.SeparatorHeight);
                                Vector2 separatorPos = new Vector2(anchorPoint.X, anchorPoint.Y - separatorSize.Y / 2) + text.SeparatorOffset;
                                drawList.AddRectFilled(separatorPos, separatorPos + separatorSize, text.SeparatorColor.Base);
                            }

                            if (text.Enabled)
                            {
                                DrawHelpers.DrawText(
                                    drawList,
                                    formattedText,
                                    textPos,
                                    text.TextJobColor ? jobColor.Base : text.TextColor.Base,
                                    text.ShowOutline,
                                    text.OutlineColor.Base);
                            }
                        }
                    }
                }
            }
        }

        private void MovePlayerIntoViewableRange(List<Combatant> sortedCombatants, int scrollPosition, string playerName)
        {
            int oldPlayerIndex = sortedCombatants.FindIndex(combatant => combatant.Name.Contains("YOU") || combatant.Name.Contains(playerName));
            if (oldPlayerIndex == -1)
            {
                return;
            }

            int newPlayerIndex = Math.Clamp(oldPlayerIndex, scrollPosition, this.BarConfig.BarCount + scrollPosition - 1);

            if (oldPlayerIndex == newPlayerIndex)
            {
                return;
            }

            sortedCombatants.MoveItem(oldPlayerIndex, newPlayerIndex);
        }

        private void UpdatePlayerName(Combatant combatant, string localPlayerName)
        {
            combatant.NameOverwrite = this.BarConfig.UseCharacterName switch
            {
                true when combatant.Name.Contains("YOU") => localPlayerName,
                false when combatant.NameOverwrite is not null => null,
                _ => combatant.NameOverwrite
            };
        }

        private bool DrawContextMenu(string popupId, out bool selected, out int selectedIndex)
        {
            selectedIndex = -1;
            selected = false;

            bool popupDrawn = ImGui.BeginPopup(popupId);
            if (popupDrawn)
            {
                if (!ImGui.IsAnyItemActive() && !ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    ImGui.SetKeyboardFocusHere(0);
                }

                if (ImGui.Selectable("Current Data"))
                {
                    selected = true;
                }

                List<ActEvent> events = Singletons.Get<LogClient>().PastEvents;
                if (events.Count > 0)
                {
                    ImGui.Separator();
                }

                for (int i = events.Count - 1; i >= 0; i--)
                {
                    if (ImGui.Selectable($"{events[i].Encounter?.Duration}\t—\t{events[i].Encounter?.Title}"))
                    {
                        selectedIndex = i;
                        selected = true;
                    }
                }

                ImGui.Separator();
                if (ImGui.Selectable("Clear Data"))
                {
                    Singletons.Get<PluginManager>().Clear();
                    selected = true;
                }

                if (ImGui.Selectable("Configure"))
                {
                    Singletons.Get<PluginManager>().ConfigureMeter(this);
                    selected = true;
                }

                ImGui.EndPopup();
            }

            return popupDrawn;
        }

        private List<Combatant> GetSortedCombatants(ActEvent actEvent, MeterDataType dataType)
        {
            if (actEvent.Combatants is null ||
                _lastSortedTimestamp.HasValue &&
                _lastSortedTimestamp.Value == actEvent.Timestamp &&
                !this.GeneralConfig.Preview)
            {
                return _lastSortedCombatants;
            }

            List<Combatant> sortedCombatants = [.. actEvent.Combatants.Values];

            sortedCombatants.Sort((x, y) =>
            {
                float xFloat = dataType switch
                {
                    MeterDataType.Damage => x.DamageTotal?.Value ?? 0,
                    MeterDataType.Healing => x.EffectiveHealing?.Value ?? 0,
                    MeterDataType.DamageTaken => x.DamageTaken?.Value ?? 0,
                    _ => 0
                };

                float yFloat = dataType switch
                {
                    MeterDataType.Damage => y.DamageTotal?.Value ?? 0,
                    MeterDataType.Healing => y.EffectiveHealing?.Value ?? 0,
                    MeterDataType.DamageTaken => y.DamageTaken?.Value ?? 0,
                    _ => 0
                };

                return (int)(yFloat - xFloat);
            });

            _lastSortedTimestamp = actEvent.Timestamp;
            _lastSortedCombatants = sortedCombatants;
            return sortedCombatants;
        }
    }
}
