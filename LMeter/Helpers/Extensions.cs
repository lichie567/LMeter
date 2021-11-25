using System.Numerics;

namespace LMeter.Helpers
{
    public static class Extensions
    {
        public static Vector2 AddX(this Vector2 v, float offset)
        {
            return new Vector2(v.X + offset, v.Y);
        }

        public static Vector2 AddY(this Vector2 v, float offset)
        {
            return new Vector2(v.X, v.Y + offset);
        }

        public static Vector4 AddTransparency(this Vector4 vec, float opacity)
        {
            return new Vector4(vec.X, vec.Y, vec.Z, vec.W * opacity);
        }

        public static Vector4 AdjustColor(this Vector4 vec, float correctionFactor)
        {
            float red = vec.X;
            float green = vec.Y;
            float blue = vec.Z;

            if (correctionFactor < 0)
            {
                correctionFactor = 1 + correctionFactor;
                red *= correctionFactor;
                green *= correctionFactor;
                blue *= correctionFactor;
            }
            else
            {
                red = (1 - red) * correctionFactor + red;
                green = (1 - green) * correctionFactor + green;
                blue = (1 - blue) * correctionFactor + blue;
            }

            return new Vector4(red, green, blue, vec.W);
        }
    }
}
