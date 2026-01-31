using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.FontIdentifier;
using Dalamud.Interface.ImGuiFontChooserDialog;
using LMeter.Helpers;
using Newtonsoft.Json;

namespace LMeter.Config
{
    public class FontConfig : IConfigPage
    {
        [JsonIgnore]
        private const int MIN_SIZE = 6;

        [JsonIgnore]
        public bool Active { get; set; }

        public string Name => "Fonts";

        [JsonIgnore]
        private static readonly string? m_fontPath = FontsManager.GetUserFontPath();

        [JsonIgnore]
        private int m_selectedFont = 0;

        [JsonIgnore]
        private int m_selectedSize = 10;

        [JsonIgnore]
        private string[] m_fontPaths = FontsManager.GetFontPaths(FontsManager.GetUserFontPath());

        [JsonIgnore]
        private readonly string[] m_sizes = Enumerable.Range(MIN_SIZE, 48 - MIN_SIZE + 1).Select(i => i.ToString()).ToArray();

        [JsonIgnore]
        private bool m_chinese = false;

        [JsonIgnore]
        private bool m_korean = false;

        public Dictionary<string, FontData> Fonts { get; set; }

        public FontConfig()
        {
            RefreshFontList();
            this.Fonts = [];

            foreach (FontData font in FontsManager.GetDefaultFontData())
            {
                this.Fonts.Add(FontsManager.GetFontKey(font), font);
            }
        }

        public IConfigPage GetDefault() => new FontConfig();

        public void DrawConfig(Vector2 size, float padX, float padY, bool border = true)
        {
            if (m_fontPaths.Length == 0)
            {
                RefreshFontList();
            }

            if (ImGui.BeginChild("##FontConfig", new Vector2(size.X, size.Y), border))
            {
                if (m_fontPath is not null)
                {
                    float cursorY = ImGui.GetCursorPosY();
                    ImGui.SetCursorPosY(cursorY + 2f);
                    ImGui.Text("Copy Font Folder Path to Clipboard: ");
                    ImGui.SameLine();

                    Vector2 buttonSize = new(40, 0);
                    ImGui.SetCursorPosY(cursorY);
                    DrawHelpers.DrawButton(
                        string.Empty,
                        FontAwesomeIcon.Copy,
                        () => ImGui.SetClipboardText(m_fontPath),
                        null,
                        buttonSize
                    );

                    string[] fontNames = m_fontPaths.Select(x => FontsManager.GetFontName(m_fontPath, x)).ToArray();
                    ImGui.Combo("Font", ref m_selectedFont, fontNames, fontNames.Length);
                    ImGui.SameLine();
                    DrawHelpers.DrawButton(
                        string.Empty,
                        FontAwesomeIcon.Sync,
                        () => RefreshFontList(),
                        "Reload Font List",
                        buttonSize
                    );

                    ImGui.Combo("Size", ref m_selectedSize, m_sizes, m_sizes.Length);
                    ImGui.SameLine();
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 3f);
                    DrawHelpers.DrawButton(
                        string.Empty,
                        FontAwesomeIcon.Plus,
                        () => AddFont(m_selectedFont, m_selectedSize),
                        "Add Font",
                        buttonSize
                    );

                    ImGui.Checkbox("Chinese/Japanese", ref m_chinese);
                    ImGui.SameLine();
                    ImGui.Checkbox("Korean", ref m_korean);
                    ImGui.SameLine();
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 81f);
                    ImGui.Text("Add System or Game Font");
                    ImGui.SameLine();

                    DrawHelpers.DrawButton(
                        string.Empty,
                        FontAwesomeIcon.Font,
                        () => DisplayFontSelector(),
                        string.Empty,
                        buttonSize
                    );

                    ImGui.Text("Font List");

                    ImGuiTableFlags tableFlags =
                        ImGuiTableFlags.RowBg
                        | ImGuiTableFlags.Borders
                        | ImGuiTableFlags.BordersOuter
                        | ImGuiTableFlags.BordersInner
                        | ImGuiTableFlags.ScrollY
                        | ImGuiTableFlags.NoSavedSettings;

                    if (
                        ImGui.BeginTable(
                            "##Font_Table",
                            5,
                            tableFlags,
                            new Vector2(size.X - padX * 2, size.Y - ImGui.GetCursorPosY() - padY * 2)
                        )
                    )
                    {
                        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch, 0, 0);
                        ImGui.TableSetupColumn("Size", ImGuiTableColumnFlags.WidthFixed, 40, 1);
                        ImGui.TableSetupColumn("CN/JP", ImGuiTableColumnFlags.WidthFixed, 40, 2);
                        ImGui.TableSetupColumn("KR", ImGuiTableColumnFlags.WidthFixed, 40, 3);
                        ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, 45, 4);

                        ImGui.TableSetupScrollFreeze(0, 1);
                        ImGui.TableHeadersRow();

                        for (int i = 0; i < this.Fonts.Keys.Count; i++)
                        {
                            ImGui.PushID(i.ToString());
                            ImGui.TableNextRow(ImGuiTableRowFlags.None, 28);

                            string key = this.Fonts.Keys.ElementAt(i);
                            FontData font = this.Fonts[key];

                            if (ImGui.TableSetColumnIndex(0))
                            {
                                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3f);
                                ImGui.Text(key);
                            }

                            if (ImGui.TableSetColumnIndex(1))
                            {
                                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3f);
                                ImGui.Text(font.Size.ToString());
                            }

                            if (ImGui.TableSetColumnIndex(2))
                            {
                                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3f);
                                ImGui.Text(font.Chinese ? "Yes" : "No");
                            }

                            if (ImGui.TableSetColumnIndex(3))
                            {
                                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3f);
                                ImGui.Text(font.Korean ? "Yes" : "No");
                            }

                            if (ImGui.TableSetColumnIndex(4))
                            {
                                if (!FontsManager.DefaultFontKeys.Contains(key))
                                {
                                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 1f);
                                    DrawHelpers.DrawButton(
                                        string.Empty,
                                        FontAwesomeIcon.Trash,
                                        () => RemoveFont(key),
                                        "Remove Font",
                                        new Vector2(45, 0)
                                    );
                                }
                            }
                        }

                        ImGui.EndTable();
                    }
                }
            }

            ImGui.EndChild();
        }

        public void RefreshFontList()
        {
            m_fontPaths = FontsManager.GetFontPaths(FontsManager.GetUserFontPath());
        }

        private void AddFont(int fontIndex, int size)
        {
            FontData newFont = new(
                FontsManager.GetFontName(m_fontPath, m_fontPaths[fontIndex]),
                m_fontPaths[fontIndex],
                size + MIN_SIZE,
                m_chinese,
                m_korean,
                null
            );
            string key = FontsManager.GetFontKey(newFont);

            if (this.Fonts.TryAdd(key, newFont))
            {
                Singletons.Get<FontsManager>().UpdateFonts(this.Fonts.Values);
            }
        }

        public void DisplayFontSelector()
        {
            if (Singletons.Get<IUiBuilder>() is UiBuilder uiBuilder)
            {
                SingleFontChooserDialog fcd = new(uiBuilder);
                uiBuilder.Draw += fcd.Draw;
                fcd.ResultTask.ContinueWith(r =>
                {
                    _ = r.Exception;
                    uiBuilder.Draw -= fcd.Draw;
                    SelectFontCallback(fcd.SelectedFont);
                    fcd.Dispose();
                });
            }
        }

        private void SelectFontCallback(SingleFontSpec font)
        {
            FontData newFont = new(font.ToString(), string.Empty, font.SizePt, false, false, font);
            string key = FontsManager.GetFontKey(newFont);

            if (this.Fonts.TryAdd(key, newFont))
            {
                Singletons.Get<FontsManager>().UpdateFonts(this.Fonts.Values);
            }
        }

        private void RemoveFont(string key)
        {
            this.Fonts.Remove(key);
            Singletons.Get<FontsManager>().UpdateFonts(this.Fonts.Values);
        }
    }
}
