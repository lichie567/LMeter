using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;

namespace LMeter.Helpers
{
    public static class Utils
    {

        public static Vector2 GetAnchoredPosition(Vector2 position, Vector2 size, DrawAnchor anchor)
        {
            return anchor switch
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
        }

        public static GameObject? FindTargetOfTarget(GameObject? player, GameObject? target)
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
            ObjectTable objectTable = Singletons.Get<ObjectTable>();
            for (int i = 0; i < 200; i += 2)
            {
                GameObject? actor = objectTable[i];
                if (actor?.ObjectId == target.TargetObjectId)
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
                    PluginLog.Error("Error trying to open url: " + e.Message);
                }
            }
        }
        private static uint FCColor(uint baseoffset, uint job)
        {
            uint totaloffset = job;
            // This looks like a mess, but it's because of two reasons:
            // 1. There are unrelated icons between jobs/classes randomly.
            // 2. Arcanist and Machinist are simply in a different location than their job order.
            if (job >= 6) totaloffset += 1u;
            if (job >= 19) totaloffset += 38u;
            if (job == 26) totaloffset -= 56u;
            if (job >= 27) totaloffset -= 1u;
            if (job >= 29) totaloffset += 33u;
            if (job == 31) totaloffset += 2u;
            if (job >= 32) totaloffset -= 1u;
            if (job >= 34) totaloffset += 2u;
            return baseoffset + totaloffset;
        }

        public static uint StyleToOffset(uint job, int style)
        {
            return style switch
            {
                11 => FCColor(94521u, job), // FC Green
                10 => FCColor(94021u, job), // FC Blue
                9  => FCColor(93521u, job), // FC Purple
                8  => FCColor(93021u, job), // FC Red
                7  => FCColor(92521u, job), // FC Orange
                6  => FCColor(92021u, job), // FC Gold
                5  => FCColor(91521u, job), // FC Black
                4  => FCColor(91021u, job), // FC Silver
                3  => 62800u + job, // Gear Set
                2  => (job >= 19u ? (62401u - 19u) : 62300u) + job, // Glowing
                1  => 62100u + job, // Framed
                0  => 62000u + job, // Filled Gold
                _  => 62000u + job, // Default to Filled Gold
            };
        }
    }
        
    public enum DrawAnchor
    {
        Center = 0,
        Left = 1,
        Right = 2,
        Top = 3,
        TopLeft = 4,
        TopRight = 5,
        Bottom = 6,
        BottomLeft = 7,
        BottomRight = 8
    }
}
