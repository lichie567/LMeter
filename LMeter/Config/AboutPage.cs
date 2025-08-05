﻿using System.Numerics;
using System.Text.Json.Serialization;
using Dalamud.Bindings.ImGui;
using LMeter.Helpers;

namespace LMeter.Config
{
    public class AboutPage : IConfigPage
    {
        [JsonIgnore]
        public bool Active { get; set; }

        public string Name => "Changelog";

        public IConfigPage GetDefault() => new AboutPage();

        public void DrawConfig(Vector2 size, float padX, float padY, bool border = true)
        {
            if (ImGui.BeginChild("##AboutPage", new Vector2(size.X, size.Y), border))
            {
                Vector2 headerSize = Vector2.Zero;
                // if (Plugin.IconTexture is not null)
                // {
                //     Vector2 iconSize = new(Plugin.IconTexture.Width, Plugin.IconTexture.Height);
                //     string versionText = $"LMeter v{Plugin.Version}";
                //     Vector2 textSize = ImGui.CalcTextSize(versionText);
                //     headerSize = new Vector2(size.X, iconSize.Y + textSize.Y);

                //     if (ImGui.BeginChild("##Icon", headerSize, false))
                //     {
                //         ImDrawListPtr drawList = ImGui.GetWindowDrawList();
                //         Vector2 pos = ImGui.GetWindowPos().AddX(size.X / 2 - iconSize.X / 2);
                //         drawList.AddImage(Plugin.IconTexture.Handle, pos, pos + iconSize);
                //         Vector2 textPos = ImGui.GetWindowPos().AddX(size.X / 2 - textSize.X / 2).AddY(iconSize.Y);
                //         drawList.AddText(textPos, 0xFFFFFFFF, versionText);
                //         ImGui.End();
                //     }
                // }

                // ImGui.SetCursorPosY(ImGui.GetCursorPosY() + headerSize.Y);
                // DrawHelpers.DrawSpacing(1);
                
                ImGui.Text("Changelog");
                Vector2 changeLogSize = new(size.X - padX * 2, size.Y - ImGui.GetCursorPosY() - padY - 30);

                if (ImGui.BeginChild("##Changelog", changeLogSize, true))
                {
                    ImGui.Text(Plugin.Changelog);
                    ImGui.EndChild();
                }

                ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0);
                Vector2 buttonSize = new((size.X - padX * 2 - padX * 2) / 3, 30 - padY * 2);
                if (ImGui.Button("Github", buttonSize))
                {
                    Utils.OpenUrl("https://github.com/lichie567/LMeter");
                }

                ImGui.SameLine();
                if (ImGui.Button("Help", buttonSize))
                {
                    Utils.OpenUrl("https://github.com/lichie567/LMeter/wiki/FAQ");
                }

                ImGui.SameLine();
                if (ImGui.Button("Ko-fi", buttonSize))
                {
                    Utils.OpenUrl("https://ko-fi.com/lichie");
                }

                ImGui.PopStyleVar();
            }

            ImGui.EndChild();
        }
    }
}
