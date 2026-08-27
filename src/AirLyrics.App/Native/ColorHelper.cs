using System;
using System.Windows.Media;

namespace AirLyrics.App.Native
{
    public static class ColorHelper
    {
        public static Color FromHsl(double hue, double saturation, double lightness)
        {
            saturation = Math.Clamp(saturation, 0.0, 1.0);
            lightness = Math.Clamp(lightness, 0.0, 1.0);
            hue = (hue % 360 + 360) % 360;

            if (saturation == 0)
            {
                byte val = (byte)(lightness * 255);
                return Color.FromRgb(val, val, val);
            }

            double q = lightness < 0.5 
                ? lightness * (1 + saturation) 
                : lightness + saturation - (lightness * saturation);
            double p = 2 * lightness - q;

            double hk = hue / 360.0;
            double r = HueToRgb(p, q, hk + (1.0 / 3.0));
            double g = HueToRgb(p, q, hk);
            double b = HueToRgb(p, q, hk - (1.0 / 3.0));

            return Color.FromRgb(
                (byte)Math.Round(r * 255), 
                (byte)Math.Round(g * 255), 
                (byte)Math.Round(b * 255));
        }

        private static double HueToRgb(double p, double q, double t)
        {
            if (t < 0) t += 1;
            if (t > 1) t -= 1;
            if (t < 1.0 / 6.0) return p + (q - p) * 6 * t;
            if (t < 1.0 / 2.0) return q;
            if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6;
            return p;
        }

        public static string ColorToHex(Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }
    }
}
