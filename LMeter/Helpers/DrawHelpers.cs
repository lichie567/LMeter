﻿using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Services;
using Dalamud.Bindings.ImGui;
using LMeter.Config;

namespace LMeter.Helpers
{
    public class DrawHelpers
    {
        public static void DrawButton(
            string label,
            FontAwesomeIcon icon,
            Action clickAction,
            string? help = null,
            Vector2? size = null)
        {
            if (!string.IsNullOrEmpty(label))
            {
                ImGui.Text(label);
                ImGui.SameLine();
            }

            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button(icon.ToIconString(), size ?? Vector2.Zero))
            {
                clickAction();
            }

            ImGui.PopFont();
            if (!string.IsNullOrEmpty(help) && ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(help);
            }
        }

        public static void DrawNotification(
            string message,
            NotificationType type = NotificationType.Info,
            uint durationInMs = 3000,
            string title = "LMeter")
        {
            Notification notification = new()
            {
                Title = title,
                Content = message,
                Type = type,
                InitialDuration = TimeSpan.FromMilliseconds(durationInMs),
                Minimized = false
            };

            Singletons.Get<INotificationManager>().AddNotification(notification);
        }

        public static void DrawNestIndicator(int depth)
        {
            // This draws the L shaped symbols and padding to the left of config items collapsible under a checkbox.
            // Shift cursor to the right to pad for children with depth more than 1.
            // 26 is an arbitrary value I found to be around half the width of a checkbox
            Vector2 oldCursor = ImGui.GetCursorPos();
            Vector2 offset = new(26 * Math.Max(depth - 1, 0), 0);
            ImGui.SetCursorPos(oldCursor + offset);
            ImGui.TextColored(new Vector4(229f / 255f, 57f / 255f, 57f / 255f, 1f), "\u2002\u2514");
            ImGui.SetCursorPosY(oldCursor.Y);
            ImGui.SameLine();
        }

        public static void DrawSpacing(int spacingSize)
        {
            for (int i = 0; i < spacingSize; i++)
            {
                ImGui.NewLine();
            }
        }

        public static void DrawIcon(
            uint iconId,
            Vector2 position,
            Vector2 size,
            ImDrawListPtr drawList)
        {
            IDalamudTextureWrap? tex = TextureCache.GetTextureById(iconId);

            if (tex is null)
            {
                return;
            }

            drawList.AddImage(tex.Handle, position, position + size, Vector2.Zero, Vector2.One);
        }

        public static void DrawIcon(
            uint iconId,
            Vector2 position,
            Vector2 size,
            bool cropIcon,
            int stackCount,
            float opacity,
            ImDrawListPtr drawList)
        {
            IDalamudTextureWrap? tex = TextureCache.GetTextureById(iconId, (uint)stackCount, true);

            if (tex is null)
            {
                return;
            }

            (Vector2 uv0, Vector2 uv1) = GetTexCoordinates(tex, size, cropIcon);

            uint alpha = (uint)(opacity * 255) << 24 | 0x00FFFFFF;
            drawList.AddImage(tex.Handle, position, position + size, uv0, uv1, alpha);
        }

        public static void DrawFontSelector(string label, ref string fontKey, ref int fontId)
        {
            string[] fontOptions = FontsManager.GetFontList();
            if (fontOptions.Length == 0)
            {
                return;
            }

            if (!FontsManager.ValidateFont(fontOptions, fontId, fontKey))
            {
                fontId = 0;
                for (int i = 0; i < fontOptions.Length; i++)
                {
                    if (fontKey.Equals(fontOptions[i]))
                    {
                        fontId = i;
                    }
                }
            }

            ImGui.Combo(label, ref fontId, fontOptions, fontOptions.Length);
            fontKey = fontOptions[fontId];
        }

        public static void DrawColorSelector(string label, ConfigColor color)
        {
            Vector4 vector = color.Vector;
            ImGui.ColorEdit4(label, ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
            color.Vector = vector;
        }

        public static void DrawRoundingOptions(string label, int depth, RoundingOptions options)
        {
            ImGui.Checkbox(label, ref options.Enabled);
            if (options.Enabled)
            {
                DrawNestIndicator(depth + 1);
                ImGui.DragFloat($"Roundness##{label}", ref options.Rounding, 0.1f, 1, 50);

                DrawNestIndicator(depth + 1);
                ImGui.Combo($"Rounding Type##{label}", ref Unsafe.As<RoundingFlag, int>(ref options.Flag), Utils.RoundingFlags, Utils.RoundingFlags.Length);
            }
        }

        public static void DrawRectFilled(ImDrawListPtr drawList, Vector2 p_min, Vector2 p_max, ConfigColor color, RoundingOptions? options = null)
        {
            if (options is not null && options.Enabled)
            {
                drawList.AddRectFilled(p_min, p_max, color.Base, options.Rounding, options.GetImDrawFlag());
            }
            else
            {
                drawList.AddRectFilled(p_min, p_max, color.Base);
            }
        }
        public static void DrawRect(ImDrawListPtr drawList, Vector2 p_min, Vector2 p_max, ConfigColor color, RoundingOptions? options = null)
        {
            if (options is not null && options.Enabled)
            {
                drawList.AddRect(p_min, p_max, color.Base, options.Rounding, options.GetImDrawFlag());
            }
            else
            {
                drawList.AddRect(p_min, p_max, color.Base);
            }
        }

        public static (Vector2, Vector2) GetTexCoordinates(IDalamudTextureWrap texture, Vector2 size, bool cropIcon = true)
        {
            if (texture == null)
            {
                return (Vector2.Zero, Vector2.Zero);
            }

            // Status = 24x32, show from 2,7 until 22,26
            //show from 0,0 until 24,32 for uncropped status icon

            float uv0x = cropIcon ? 4f : 1f;
            float uv0y = cropIcon ? 14f : 1f;

            float uv1x = cropIcon ? 4f : 1f;
            float uv1y = cropIcon ? 12f : 1f;

            Vector2 uv0 = new(uv0x / texture.Width, uv0y / texture.Height);
            Vector2 uv1 = new(1f - uv1x / texture.Width, 1f - uv1y / texture.Height);

            return (uv0, uv1);
        }

        public static void DrawInWindow(
            string name,
            Vector2 pos,
            Vector2 size,
            bool needsInput,
            bool setPosition,
            Action<ImDrawListPtr> drawAction)
        {
            DrawInWindow(name, pos, size, needsInput, false, setPosition, drawAction);
        }

        public static void DrawInWindow(
            string name,
            Vector2 pos,
            Vector2 size,
            bool needsInput,
            bool needsFocus,
            bool locked,
            Action<ImDrawListPtr> drawAction,
            ImGuiWindowFlags extraFlags = ImGuiWindowFlags.None)
        {
            ImGuiWindowFlags windowFlags =
                ImGuiWindowFlags.NoSavedSettings |
                ImGuiWindowFlags.NoTitleBar |
                ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoBackground |
                extraFlags;

            if (!needsInput)
            {
                windowFlags |= ImGuiWindowFlags.NoInputs;
            }

            if (!needsFocus)
            {
                windowFlags |= ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoBringToFrontOnFocus;
            }

            if (locked)
            {
                windowFlags |= ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize;
                ImGui.SetNextWindowSize(size);
                ImGui.SetNextWindowPos(pos);
            }

            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);

            if (ImGui.Begin(name, windowFlags))
            {
                drawAction(ImGui.GetWindowDrawList());
            }

            ImGui.PopStyleVar(3);
            ImGui.End();
        }

        public static void DrawText(
            ImDrawListPtr drawList,
            string text,
            Vector2 pos,
            uint color,
            bool outline,
            uint outlineColor = 0xFF000000,
            int thickness = 1)
        {
            if (outline)
            {
                for (int i = 1; i < thickness + 1; i++)
                {
                    drawList.AddText(new Vector2(pos.X - i, pos.Y + i), outlineColor, text);
                    drawList.AddText(new Vector2(pos.X, pos.Y + i), outlineColor, text);
                    drawList.AddText(new Vector2(pos.X + i, pos.Y + i), outlineColor, text);
                    drawList.AddText(new Vector2(pos.X - i, pos.Y), outlineColor, text);
                    drawList.AddText(new Vector2(pos.X + i, pos.Y), outlineColor, text);
                    drawList.AddText(new Vector2(pos.X - i, pos.Y - i), outlineColor, text);
                    drawList.AddText(new Vector2(pos.X, pos.Y - i), outlineColor, text);
                    drawList.AddText(new Vector2(pos.X + i, pos.Y - i), outlineColor, text);
                }
            }

            drawList.AddText(new Vector2(pos.X, pos.Y), color, text);
        }

        public static string? DrawTextTagsList(string[] tags)
        {
            string? selectedTag = null;

            if (ImGui.Button("Tags"))
            {
                ImGui.OpenPopup("LMeter_TextTagsPopup");
            }

            ImGui.SetNextWindowSize(new(210, 300));
            if (ImGui.BeginPopup("LMeter_TextTagsPopup", ImGuiWindowFlags.NoMove))
            {
                if (ImGui.BeginChild("##LMeter_TextTags_List", new Vector2(195, 284), true))
                {
                    foreach (string tag in tags)
                    {
                        if (ImGui.Selectable(tag))
                        {
                            selectedTag = tag;
                        }
                    }

                    ImGui.EndChild();
                }

                ImGui.EndPopup();
            }

            return selectedTag;
        }
    }
}
