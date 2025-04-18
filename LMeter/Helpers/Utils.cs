﻿using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;

namespace LMeter.Helpers
{
    public static class Utils
    {
        public static readonly string[] AnchorOptions = Enum.GetNames<DrawAnchor>();
        public static readonly string[] RoundingFlags = Enum.GetNames<RoundingFlag>();

        public static Vector2 GetAnchoredPosition(Vector2 position, Vector2 size, DrawAnchor anchor) => anchor switch
        {
            DrawAnchor.Center => position - size / 2f,
            DrawAnchor.Left => position + new Vector2(0, -size.Y / 2f),
            DrawAnchor.Right => position + new Vector2(-size.X, -size.Y / 2f),
            DrawAnchor.Top => position + new Vector2(-size.X / 2f, 0),
            DrawAnchor.TopLeft => position,
            DrawAnchor.TopRight => position + new Vector2(-size.X, 0),
            DrawAnchor.Bottom => position + new Vector2(-size.X / 2f, -size.Y),
            DrawAnchor.BottomLeft => position + new Vector2(0, -size.Y),
            DrawAnchor.BottomRight => position + new Vector2(-size.X, -size.Y),
            _ => position
        };

        public static Vector2 GetTopLeft(Vector2 anchorPoint, Vector2 textBoxSize, DrawAnchor anchor) => anchor switch
        {
            DrawAnchor.Center => anchorPoint + new Vector2(-textBoxSize.X / 2, -textBoxSize.Y / 2),
            DrawAnchor.Left => anchorPoint + new Vector2(-textBoxSize.X, -textBoxSize.Y / 2),
            DrawAnchor.Right => anchorPoint + new Vector2(0, -textBoxSize.Y / 2),
            DrawAnchor.Top => anchorPoint + new Vector2(-textBoxSize.X / 2, -textBoxSize.Y),
            DrawAnchor.TopLeft => anchorPoint + new Vector2(-textBoxSize.X, -textBoxSize.Y),
            DrawAnchor.TopRight => anchorPoint + new Vector2(0, -textBoxSize.Y),
            DrawAnchor.Bottom => anchorPoint + new Vector2(-textBoxSize.X / 2, textBoxSize.Y),
            DrawAnchor.BottomLeft => anchorPoint + new Vector2(-textBoxSize.X, textBoxSize.Y),
            DrawAnchor.BottomRight => anchorPoint + new Vector2(0, textBoxSize.Y),
            _ => anchorPoint
        };

        public static DrawAnchor Opposite(this DrawAnchor anchor) => anchor switch
        {
            DrawAnchor.Center => DrawAnchor.Center,
            DrawAnchor.Left => DrawAnchor.Right,
            DrawAnchor.Right => DrawAnchor.Left,
            DrawAnchor.Top => DrawAnchor.Bottom,
            DrawAnchor.TopLeft => DrawAnchor.BottomRight,
            DrawAnchor.TopRight => DrawAnchor.BottomLeft,
            DrawAnchor.Bottom => DrawAnchor.Top,
            DrawAnchor.BottomLeft => DrawAnchor.TopRight,
            DrawAnchor.BottomRight => DrawAnchor.TopLeft,
            _ => anchor
        };

        public static Vector2 GetTextPos(Vector2 parentPos, Vector2 parentSize, Vector2 textSize, DrawAnchor textAlignment)
        {
            return GetAnchoredPosition(GetAnchoredPosition(parentPos, -parentSize, textAlignment), textSize, textAlignment);
        }

        public static IGameObject? FindTargetOfTarget(IGameObject? player, IGameObject? target)
        {
            if (target == null)
            {
                return null;
            }

            if (target.TargetObjectId == 0 && player != null && player.TargetObjectId == 0)
            {
                return player;
            }

            // only the first 200 elements in the array are relevant due to the order in which SE packs data into the array
            // we do a step of 2 because its always an actor followed by its companion
            IObjectTable objectTable = Singletons.Get<IObjectTable>();
            for (int i = 0; i < 200; i += 2)
            {
                IGameObject? actor = objectTable[i];
                if (actor?.GameObjectId == target.TargetObjectId)
                {
                    return actor;
                }
            }

            return null;
        }

        public static void OpenUrl(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
                try
                {
                    // hack because of this: https://github.com/dotnet/corefx/issues/10361
                    if (RuntimeInformation.IsOSPlatform(osPlatform: OSPlatform.Windows))
                    {
                        url = url.Replace("&", "^&");
                        Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        Process.Start("xdg-open", url);
                    }
                }
                catch (Exception e)
                {
                    Singletons.Get<IPluginLog>().Error("Error trying to open url: " + e.Message);
                }
            }
        }

        public static string GetTagsTooltip()
        {
            return $"Append the characters ':k' to a numeric tag to kilo-format it.\n" +
                    "Append a '.' and a number to limit the number of characters,\n" +
                    "or the number of decimals when used with numeric values.\n\nExamples:\n" +
                    "[damagetotal]          =>    123,456\n" +
                    "[damagetotal:k]      =>           123k\n" +
                    "[damagetotal:k.1]  =>       123.4k\n\n" +
                    "[name]                   =>    Firstname Lastname\n" +
                    "[name_first.5]    =>    First\n" +
                    "[name_last.1]     =>    L";
        }
    }
}
