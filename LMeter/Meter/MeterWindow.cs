using System;
using System.Collections.Generic;
using System.Linq;
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
    public class MeterWindow(string name) : IConfigurable
    {
        [JsonIgnore] private const string _defaultProfile = "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";
        
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
        [JsonIgnore] public string Id { get; init; } = $"LMeter_MeterWindow_{Guid.NewGuid()}";

        public string Name { get; set; } = name;

        public GeneralConfig GeneralConfig { get; set; } = new GeneralConfig();
        public HeaderConfig HeaderConfig { get; set; } = new HeaderConfig();
        public TextListConfig<Encounter> HeaderTextConfig { get; set; } = new("Header Texts");
        public TextListConfig<Encounter> FooterTextConfig { get; set; } = new("Footer Texts");
        public BarConfig BarConfig { get; set; } = new BarConfig();
        public TextListConfig<Combatant> BarTextConfig { get; set; } = new("Bar Texts");
        public BarColorsConfig BarColorsConfig { get; set; } = new BarColorsConfig();
        public VisibilityConfig VisibilityConfig { get; set; } = new VisibilityConfig();

        public IEnumerable<IConfigPage> GetConfigPages()
        {
            yield return this.GeneralConfig;
            yield return this.HeaderConfig;

            if (this.HeaderConfig.ShowHeader)
            {
                yield return this.HeaderTextConfig;
            }

            if (this.HeaderConfig.ShowFooter)
            {
                yield return this.FooterTextConfig;
            }

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
                    if (this.HeaderTextConfig.Active)
                    {
                        newPage.NameInternal = "Header Texts";
                        this.HeaderTextConfig = newPage;
                    }

                    if (this.FooterTextConfig.Active)
                    {
                        newPage.NameInternal = "Footer Texts";
                        this.FooterTextConfig = newPage;
                    }

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

        public static MeterWindow GetDefaultMeter(string name = "Profile 1")
        {
            MeterWindow newMeter = ConfigHelpers.GetFromImportString<MeterWindow>(_defaultProfile) ?? new MeterWindow(name);
            newMeter.Name = name;
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

                if (this.GeneralConfig.Preview && !_lastFrameWasPreview)
                {
                    _previewEvent = ActEvent.GetTestData();
                }

                ActEvent? actEvent = this.GeneralConfig.Preview ? _previewEvent : Singletons.Get<LogClient>().GetEvent(_eventIndex);
                ConfigColor jobColor = this.BarColorsConfig.GetColor(CharacterState.GetCharacterJob());

                ImGui.PushClipRect(localPos, localPos + size, false);
                Vector2 footerPos = localPos.AddY(size.Y - this.HeaderConfig.FooterHeight);

                if (this.HeaderConfig.ShowHeader)
                {
                    (localPos, size) = DrawHeader(
                        drawList,
                        this.HeaderConfig,
                        this.HeaderTextConfig,
                        localPos,
                        size,
                        jobColor,
                        actEvent?.Encounter);
                }
                
                drawList.AddRectFilled(localPos, localPos + size, this.GeneralConfig.BackgroundColor.Base);
                if (this.BarConfig.ShowColumnHeader && actEvent is not null)
                {
                    List<Text> columnHeaderTexts = GetColumnHeaderTexts(this.BarTextConfig.Texts, this.BarConfig);
                    Vector2 columnHeaderSize = new(size.X, this.BarConfig.ColumnHeaderHeight);
                    drawList.AddRectFilled(localPos, localPos + columnHeaderSize, this.BarConfig.ColumnHeaderColor.Base);
                    DrawBarTexts(
                        drawList,
                        columnHeaderTexts,
                        localPos + this.BarConfig.ColumnHeaderOffset,
                        columnHeaderSize,
                        jobColor,
                        actEvent);
                        
                    (localPos, size) = (localPos.AddY(columnHeaderSize.Y), size.AddY(-columnHeaderSize.Y));
                }
            
                if (this.HeaderConfig.ShowFooter)
                {
                    size = size.AddY(-this.HeaderConfig.FooterHeight);
                }

                this.DrawBars(drawList, localPos, size, actEvent);

                if (this.HeaderConfig.ShowFooter)
                {
                    DrawFooter(
                        drawList,
                        this.HeaderConfig,
                        this.FooterTextConfig,
                        footerPos,
                        size,
                        jobColor,
                        actEvent?.Encounter);
                }

                ImGui.PopClipRect();
            });

            _lastFrameWasUnlocked = _unlocked;
            _lastFrameWasPreview = this.GeneralConfig.Preview;
            _lastFrameWasCombat = combat;
        }

        private static List<Text> GetColumnHeaderTexts(List<Text> texts, BarConfig config)
        {
            List<Text> newTexts = [.. texts.Select(x => x.Clone())];
            foreach (Text text in newTexts)
            {
                text.TextFormat = text.Name;
                text.TextColor = config.ColumnHeaderTextColor;
                text.ShowOutline = config.ColumnHeaderShowOutline;
                text.OutlineColor = config.ColumnHeaderOutlineColor;
                text.FontKey = config.UseColumnFont ? text.FontKey : config.ColumnHeaderFontKey;
                text.FontId = config.UseColumnFont ? text.FontId : config.ColumnHeaderFontId;
            }

            return newTexts;
        }

        private static (Vector2, Vector2) DrawHeader(
            ImDrawListPtr drawList,
            HeaderConfig headerConfig,
            TextListConfig<Encounter> headerTextConfig,
            Vector2 pos,
            Vector2 size,
            ConfigColor jobColor,
            Encounter? encounter)
        {
            Vector2 headerSize = new(size.X, headerConfig.HeaderHeight);
            drawList.AddRectFilled(pos, pos + headerSize, headerConfig.BackgroundColor.Base);

            if (encounter is null && headerConfig.ShowVersion)
            {
                using (FontsManager.PushFont(headerConfig.VersionFontKey))
                {
                    string version = $" LMeter v{Plugin.Version} ";
                    Vector2 versionSize = ImGui.CalcTextSize(version);
                    Vector2 versionPos = Utils.GetAnchoredPosition(pos, -headerSize, DrawAnchor.Left);
                    versionPos = Utils.GetAnchoredPosition(versionPos, versionSize, DrawAnchor.Left);
                    DrawHelpers.DrawText(
                        drawList,
                        version,
                        versionPos + headerConfig.VersionOffset,
                        headerConfig.VersionColor.Base,
                        headerConfig.VersionShowOutline,
                        headerConfig.VersionOutlineColor.Base);
                }
            }
            else if (encounter is not null)
            {
                DrawBarTexts(drawList, headerTextConfig.Texts, pos, headerSize, jobColor, encounter);
            }

            return (pos.AddY(headerConfig.HeaderHeight), size.AddY(-headerConfig.HeaderHeight));
        }

        private static (Vector2, Vector2) DrawFooter(
            ImDrawListPtr drawList,
            HeaderConfig headerConfig,
            TextListConfig<Encounter> footerTextConfig,
            Vector2 pos,
            Vector2 size,
            ConfigColor jobColor,
            Encounter? encounter)
        {
            Vector2 footerSize = new(size.X, headerConfig.FooterHeight);
            drawList.AddRectFilled(pos, pos + footerSize, headerConfig.FooterBackgroundColor.Base);

            if (encounter is not null)
                DrawBarTexts(drawList, footerTextConfig.Texts, pos, footerSize, jobColor, encounter);
                
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
            };
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

            Vector2 barPos = localPos;
            Vector2 barSize = new(size.X, barHeight);
            Vector2 barFillSize = new(size.X * (current / top), barHeight * barConfig.BarFillHeight);

            if (barConfig.BarFillHeight != 1f)
            {
                barPos = barConfig.BarFillDirection == 0 ? barPos.AddY(barHeight - barFillSize.Y) : barPos;
                Vector2 barBackgroundSize = new(size.X * (current / top), barHeight);
                drawList.AddRectFilled(localPos, localPos + barBackgroundSize, barConfig.BarBackgroundColor.Base);
            }

            drawList.AddRectFilled(barPos, barPos + barFillSize, barConfig.UseJobColor ? jobColor.Base : barColor.Base);

            if (barConfig.ShowJobIcon && combatant.Job != Job.UKN)
            {
                uint jobIconId = 62000u + (uint)combatant.Job + 100u * (uint)barConfig.JobIconStyle;
                Vector2 jobIconPos = localPos + barConfig.JobIconOffset;
                Vector2 jobIconSize = barConfig.JobIconSizeType == 0 ? Vector2.One * barHeight : barConfig.JobIconSize;
                if (barConfig.JobIconBackgroundColor.Vector.W > 0f)
                {
                    Vector2 jobIconBackgroundPos = new(jobIconPos.X, localPos.Y);
                    Vector2 jobIconBackgroundSize = new(jobIconSize.X, barSize.Y);
                    drawList.AddRectFilled(jobIconBackgroundPos, jobIconBackgroundPos + jobIconBackgroundSize, barConfig.JobIconBackgroundColor.Base);
                }

                DrawHelpers.DrawIcon(jobIconId, jobIconPos, jobIconSize, drawList);
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
            bool[] visited = new bool[texts.Count];
            Dictionary<int, (Vector2, Vector2)> lookup = new() { { 0, (parentPos, parentSize) } };
            for (int anchorLayer = 0; anchorLayer < texts.Count + 1; anchorLayer++)
            {
                for (int textIndex = 0; textIndex < texts.Count; textIndex++)
                {
                    Text text = texts[textIndex];
                    if (!visited[textIndex] && lookup.TryGetValue(text.AnchorParent, out (Vector2, Vector2) parent))
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
                            Vector2 anchorPoint = Utils.GetAnchoredPosition(parent.Item1, -parent.Item2, text.AnchorPoint);
                            Vector2 textBoxPos = Utils.GetTopLeft(anchorPoint, textBoxSize, text.AnchorParent == 0 ? text.AnchorPoint.Opposite() : text.AnchorPoint);
                            
                            textBoxPos += text.TextOffset;
                            DrawAnchor alignment = text.FixedTextWidth ? text.TextAlignment : text.AnchorPoint;
                            Vector2 textPos = Utils.GetTextPos(textBoxPos, textBoxSize, textSize, alignment);
                            lookup.Add(textIndex + 1, (textBoxPos, textBoxSize));
                            visited[textIndex] = true;

                            if (text.UseBackground)
                            {
                                Vector2 backgroundPos = new(textBoxPos.X, parentPos.Y);
                                Vector2 backgroundSize = new(textBoxSize.X, parentSize.Y);
                                drawList.AddRectFilled(backgroundPos, backgroundPos + backgroundSize, text.BackgroundColor.Base);
                            }

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
