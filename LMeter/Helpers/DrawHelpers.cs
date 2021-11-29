using System;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Internal.Notifications;
using ImGuiNET;
using ImGuiScene;

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
            NotificationType type = NotificationType.Success,
            uint durationInMs = 3000,
            string title = "LMeter")
        {
            Singletons.Get<UiBuilder>().AddNotification(message, title, type, durationInMs);
        }

        public static void DrawNestIndicator(int depth)
        {
            // This draws the L shaped symbols and padding to the left of config items collapsible under a checkbox.
            // Shift cursor to the right to pad for children with depth more than 1.
            // 26 is an arbitrary value I found to be around half the width of a checkbox
            Vector2 oldCursor = ImGui.GetCursorPos();
            Vector2 offset = new Vector2(26 * Math.Max((depth - 1), 0), 2);
            ImGui.SetCursorPos(oldCursor + offset);
            ImGui.TextColored(new Vector4(229f / 255f, 57f / 255f, 57f / 255f, 1f), "\u2002\u2514");
            ImGui.SameLine();
            ImGui.SetCursorPosY(oldCursor.Y);
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
            TextureWrap? tex = Singletons.Get<TexturesCache>().GetTextureFromIconId(iconId, 0, true);

            if (tex is null)
            {
                return;
            }

            drawList.AddImage(tex.ImGuiHandle, position, position + size, Vector2.Zero, Vector2.One);
        }

        public static void DrawIcon(
            uint iconId,
            Vector2 position,
            Vector2 size,
            bool cropIcon,
            int stackCount,
            bool desaturate,
            float opacity,
            ImDrawListPtr drawList)
        {
            TextureWrap? tex = Singletons.Get<TexturesCache>().GetTextureFromIconId(iconId, (uint)stackCount, true, desaturate, opacity);

            if (tex is null)
            {
                return;
            }

            (Vector2 uv0, Vector2 uv1) = GetTexCoordinates(tex, size, cropIcon);

            drawList.AddImage(tex.ImGuiHandle, position, position + size, uv0, uv1);
        }

        public static (Vector2, Vector2) GetTexCoordinates(TextureWrap texture, Vector2 size, bool cropIcon = true)
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
            // outline
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

            // text
            drawList.AddText(new Vector2(pos.X, pos.Y), color, text);
        }
    }
}
