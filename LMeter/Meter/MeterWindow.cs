using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Plugin.Services;
using Dalamud.Bindings.ImGui;
using LMeter.Act;
using LMeter.Act.DataStructures;
using LMeter.Config;
using LMeter.Helpers;
using Newtonsoft.Json;

namespace LMeter.Meter
{
    public class MeterWindow(string name) : IConfigurable
    {        
        [JsonIgnore] private bool _lastFrameWasUnlocked = false;
        [JsonIgnore] private bool _lastFrameWasDragging = false;
        [JsonIgnore] private bool _lastFrameWasPreview = false;
        [JsonIgnore] private bool _lastFrameWasCombat = false;
        [JsonIgnore] private bool _contextMenuWasOpen = false;
        [JsonIgnore] private bool _unlocked = false;
        [JsonIgnore] private bool _hovered = false;
        [JsonIgnore] private bool _dragging = false;
        [JsonIgnore] private bool _locked = false;
        [JsonIgnore] private int _eventIndex = -1;
        [JsonIgnore] private ActEvent? _previewEvent = null;
        [JsonIgnore] private int _scrollPosition = 0;
        [JsonIgnore] private float _scrollShift = 0;
        [JsonIgnore] private DateTime? _lastSortedTimestamp = null;
        [JsonIgnore] private List<Combatant> _lastSortedCombatants = [];
        [JsonIgnore] public string Id { get; init; } = $"LMeter_MeterWindow_{Guid.NewGuid()}";

        public string Name { get; set; } = name;

        public bool Enabled = true;
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

        public static MeterWindow GetDefaultMeter(MeterDataType type, string name = "Default")
        {
            MeterWindow? newMeter = type switch
            {
                MeterDataType.Damage => ConfigHelpers.GetFromImportString<MeterWindow>(DefaultProfiles.DefaultDpsMeter),
                MeterDataType.Healing => ConfigHelpers.GetFromImportString<MeterWindow>(DefaultProfiles.DefaultHpsMeter),
                _ => ConfigHelpers.GetFromImportString<MeterWindow>(DefaultProfiles.DefaultDpsMeter)
            };

            if (newMeter is not null)
            {
                newMeter.Name = name;
            }

            return newMeter ?? new MeterWindow(name);
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
            if (!this.Enabled)
            {
                return;
            }

            Vector2 localPos = pos + this.GeneralConfig.Position;
            Vector2 size = this.GeneralConfig.Size;
            if (!this.ShouldDraw(localPos, size) && !_contextMenuWasOpen)
            {
                return;
            }

            if (ImGui.IsMouseHoveringRect(localPos, localPos + size))
            {
                _scrollPosition -= (int)ImGui.GetIO().MouseWheel;

                if (ImGui.IsMouseClicked(ImGuiMouseButton.Right) && !this.GeneralConfig.Preview)
                {
                    ImGui.OpenPopup($"{this.Id}_ContextMenu", ImGuiPopupFlags.MouseButtonRight);
                }
            }

            _contextMenuWasOpen = this.DrawContextMenu($"{this.Id}_ContextMenu", out bool selected, out int index);
            if (_contextMenuWasOpen && selected)
            {
                _eventIndex = index;
                _lastSortedTimestamp = null;
                _lastSortedCombatants = [];
                _scrollPosition = 0;
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
                        DrawHelpers.DrawRect(
                            drawList,
                            borderPos + offset,
                            borderPos + borderSize - offset,
                            this.GeneralConfig.BorderColor,
                            this.GeneralConfig.BorderRounding);
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
                
                Vector2 backgroundSize = this.HeaderConfig.ShowFooter ? size.AddY(-this.HeaderConfig.FooterHeight) : size;
                DrawHelpers.DrawRectFilled(
                    drawList,
                    localPos,
                    localPos + backgroundSize,
                    this.GeneralConfig.BackgroundColor,
                    this.GeneralConfig.Rounding);

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

                ImGui.PushClipRect(localPos, localPos + size, false);
                this.DrawBars(drawList, localPos, size, actEvent);
                ImGui.PopClipRect();

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
            DrawHelpers.DrawRectFilled(
                drawList,
                pos,
                pos + headerSize,
                headerConfig.BackgroundColor,
                headerConfig.Rounding);

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
            {
                DrawBarTexts(drawList, footerTextConfig.Texts, pos, footerSize, jobColor, encounter);
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

                // add rank to the sorted combatants, with this we have the real rank of the player
                int rank = 1;
                foreach (var combatant in sortedCombatants)
                {
                    combatant.Rank = rank++;
                }

                float top = sortedCombatants[0].GetValueForDataType(this.GeneralConfig.DataType);                
                int barCount = this.BarConfig.BarCount;
                float margin = 0;
                if (this.BarConfig.BarHeightType == 1)
                {
                    float total = 0;
                    barCount = 0;
                    do
                    {
                        barCount++;
                        total += this.BarConfig.BarHeight + this.BarConfig.BarGaps;
                    } 
                    while (total <= size.Y);
                    margin = total - size.Y - this.BarConfig.BarGaps;
                }

                int currentIndex = 0;
                string playerName = Singletons.Get<IClientState>().LocalPlayer?.Name.ToString() ?? "YOU";
                if (sortedCombatants.Count > barCount)
                {
                    int unclampedScroll = _scrollPosition;
                    currentIndex = Math.Clamp(_scrollPosition, 0, sortedCombatants.Count - barCount);
                    _scrollPosition = currentIndex;

                    if (margin > 0 && _scrollPosition < unclampedScroll)
                    {
                        _scrollShift = margin;
                    }
                    
                    if (unclampedScroll < 0)
                    {
                        _scrollShift = 0;
                    }

                    if (this.BarConfig.AlwaysShowSelf && this.BarConfig.BarHeightType == 0)
                    {
                        MovePlayerIntoViewableRange(sortedCombatants, _scrollPosition, playerName);
                    }
                }

                localPos = localPos.AddY(-_scrollShift);
                int maxIndex = Math.Min(currentIndex + barCount, sortedCombatants.Count);
                int startIndex = currentIndex;
                for (; currentIndex < maxIndex; currentIndex++)
                {
                    Combatant combatant = sortedCombatants[currentIndex];
                    float current = combatant.GetValueForDataType(this.GeneralConfig.DataType);
                    ConfigColor barColor = this.BarConfig.BarColor;
                    ConfigColor jobColor = this.BarColorsConfig.GetColor(combatant.Job);

                    if (this.BarConfig.UseCustomColorForSelf && combatant.OriginalName.Equals("YOU"))
                    {
                        barColor = this.BarConfig.CustomColorForSelf;
                        jobColor = this.BarConfig.CustomColorForSelf;
                    }

                    combatant.NameOverwrite = this.BarConfig.UseCharacterName switch
                    {
                        true when combatant.Name.Contains("YOU") => combatant.Name.Replace("YOU", playerName),
                        false when combatant.NameOverwrite is not null => null,
                        _ => combatant.NameOverwrite
                    };

                    RoundingOptions rounding = this.BarConfig.MiddleBarRounding;
                    if (currentIndex == startIndex)
                    {
                        rounding = this.BarConfig.TopBarRounding;
                    }
                    else if (currentIndex == maxIndex - 1)
                    {
                        rounding = this.BarConfig.BottomBarRounding;
                    }

                    localPos = this.DrawBar(drawList, localPos, size, combatant, jobColor, barColor, top, current, rounding);
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
            float current,
            RoundingOptions rounding)
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

            DrawHelpers.DrawRectFilled(drawList, barPos, barPos + barFillSize, barConfig.UseJobColor ? jobColor : barColor, rounding);

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
                            string formattedText = actData.GetFormattedString($" {text.TextFormat} ", text.ThousandsSeparators ? "N" : "F", text.EmptyIfZero);
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
            int oldPlayerIndex = sortedCombatants.FindIndex(combatant => combatant.Name.Equals("YOU") || combatant.Name.Equals(playerName));
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
                float xFloat = x.GetValueForDataType(dataType);
                float yFloat = y.GetValueForDataType(dataType);

                return (yFloat - xFloat) switch
                {
                    > 0 => 1,
                    < 0 => -1,
                    _ => 0
                };
            });

            _lastSortedTimestamp = actEvent.Timestamp;
            _lastSortedCombatants = sortedCombatants;
            return sortedCombatants;
        }
    }
}
