using Newtonsoft.Json;
using System;
using System.Globalization;
using System.Windows.Data;
using DrawingColor = System.Drawing.Color;
using MediaColor = System.Windows.Media.Color;

namespace Aurora.Utils
{
    public static class ColorExt
    {
        public static DrawingColor ToDrawingColor(this MediaColor self)
        {
            return ColorUtils.MediaColorToDrawingColor(self);
        }

        public static MediaColor ToMediaColor(this DrawingColor self)
        {
            return ColorUtils.DrawingColorToMediaColor(self);
        }

        public static MediaColor Clone(this MediaColor self)
        {
            return ColorUtils.CloneMediaColor(self);
        }

        public static DrawingColor Clone(this DrawingColor clr)
        {
            return ColorUtils.CloneDrawingColor(clr);
        }
    }   

    /// <summary>
    /// Various color utilities
    /// </summary>
    public static class ColorUtils
    {
        private static readonly Random Randomizer = new();

        /// <summary>
        /// Converts from System.Windows.Media.Color to System.Drawing.Color
        /// </summary>
        /// <param name="in_color">A Windows Media Color</param>
        /// <returns>A Drawing Color</returns>
        public static DrawingColor MediaColorToDrawingColor(MediaColor in_color)
        {
            return FastColor(in_color.R, in_color.G, in_color.B, in_color.A);
        }

        /// <summary>
        /// Converts from System.Drawing.Color to System.Windows.Media.Color
        /// </summary>
        /// <param name="in_color">A Drawing Color</param>
        /// <returns>A Windows Media Color</returns>
        public static MediaColor DrawingColorToMediaColor(DrawingColor in_color)
        {
            return MediaColor.FromArgb(in_color.A, in_color.R, in_color.G, in_color.B);
        }

        /// <summary>
        /// Multiplies a byte by a specified double balue
        /// </summary>
        /// <param name="color">Part of the color, as a byte</param>
        /// <param name="value">The value to multiply the byte by</param>
        /// <returns>The color byte</returns>
        public static byte ColorByteMultiplication(byte color, double value)
        {
            var val = (int)(color * value);

            if (val > 255)
                return 255;
            if (val < 0)
                return 0;

            return (byte) val;
        }

        /// <summary>
        /// Blends two colors together by a specified amount
        /// </summary>
        /// <param name="background">The background color (When percent is at 0.0D, only this color is shown)</param>
        /// <param name="foreground">The foreground color (When percent is at 1.0D, only this color is shown)</param>
        /// <param name="percent">The blending percent value</param>
        /// <returns>The blended color</returns>
        public static DrawingColor BlendColors(DrawingColor background, DrawingColor foreground, double percent)
        {
            if (percent < 0.0)
                percent = 0.0;
            else if (percent > 1.0)
                percent = 1.0;

            var red = (byte)Math.Min(foreground.R * percent + background.R * (1.0 - percent), 255);
            var green = (byte)Math.Min(foreground.G * percent + background.G * (1.0 - percent), 255);
            var blue = (byte)Math.Min(foreground.B * percent + background.B * (1.0 - percent), 255);
            var alpha = (byte)Math.Min(foreground.A * percent + background.A * (1.0 - percent), 255);

            return FastColor(red, green, blue, alpha);
        }

        /// <summary>
        /// Adds two colors together by using the "SRC over DST" blending algorithm by Porter and Duff
        /// </summary>
        /// <param name="background">The background color</param>
        /// <param name="foreground">The foreground color</param>
        /// <returns>The sum of two colors including combined alpha</returns>
        public static DrawingColor AddColors(DrawingColor background, DrawingColor foreground)
        {
            var backgroundA = 255 - background.A;
            return FastColor(
                (byte)(foreground.R * foreground.A / 255 + background.R * backgroundA / 255),
                (byte)(foreground.G * foreground.A / 255 + background.G * backgroundA / 255), 
                (byte)(foreground.B * foreground.A / 255 + background.B * backgroundA / 255),
                (byte)((int) (1 - backgroundA / 255d * (255 - foreground.A) / 255d) * 255));
        }

        /// <summary>
        /// Multiplies all non-alpha values by alpha/255.
        /// Device integrations don't support alpha values, so we correct them here
        /// </summary>
        /// <param name="color">Color to correct</param>
        /// <returns>Corrected Color</returns>
        public static DrawingColor CorrectWithAlpha(DrawingColor color)
        {
            float scalar = color.A / 255.0f;

            byte red = ColorByteMultiplication(color.R, scalar);
            byte green = ColorByteMultiplication(color.G, scalar);
            byte blue = ColorByteMultiplication(color.B, scalar);

            return FastColor(red, green, blue);
        }

        /// <summary>
        /// Multiplies a Drawing Color instance by a scalar value
        /// </summary>
        /// <param name="color">The color to be multiplied</param>
        /// <param name="scalar">The scalar amount for multiplication</param>
        /// <returns>The multiplied Color</returns>
        public static DrawingColor MultiplyColorByScalar(DrawingColor color, double scalar)
        {
            byte red = ColorByteMultiplication(color.R, scalar);
            byte green = ColorByteMultiplication(color.G, scalar);
            byte blue = ColorByteMultiplication(color.B, scalar);
            byte alpha = ColorByteMultiplication(color.A, scalar);

            return FastColor(red, green, blue, alpha);
        }

        /// <summary>
        /// Multiplies a Drawing Color instance by a scalar value
        /// </summary>
        /// <param name="color">The color to be multiplied</param>
        /// <param name="scalar">The scalar amount for multiplication</param>
        /// <returns>The multiplied Color</returns>
        public static MediaColor MultiplyColorByScalar(MediaColor color, double scalar)
        {
            byte red = ColorByteMultiplication(color.R, scalar);
            byte green = ColorByteMultiplication(color.G, scalar);
            byte blue = ColorByteMultiplication(color.B, scalar);
            byte alpha = ColorByteMultiplication(color.A, scalar);

            return MediaColor.FromArgb(alpha, red, green, blue);
        }

        /// <summary>
        /// Generates a random color
        /// </summary>
        /// <returns>A random color</returns>
        public static DrawingColor GenerateRandomColor()
        {
            return FastColor((byte)Randomizer.Next(255), 
                (byte)Randomizer.Next(255), 
                (byte)Randomizer.Next(255)
                );
        }

        /// <summary>
        /// Returns an average color from a presented Bitmap
        /// </summary>
        /// <param name="bitmap">The bitmap to be evaluated</param>
        /// <returns>An average color from the bitmap</returns>
        public static DrawingColor GetAverageColor(System.Windows.Media.Imaging.BitmapSource bitmap)
        {
            var format = bitmap.Format;

            if (format != System.Windows.Media.PixelFormats.Bgr24 &&
                format != System.Windows.Media.PixelFormats.Bgr32 &&
                format != System.Windows.Media.PixelFormats.Bgra32 &&
                format != System.Windows.Media.PixelFormats.Pbgra32)
            {
                throw new InvalidOperationException("BitmapSource must have Bgr24, Bgr32, Bgra32 or Pbgra32 format");
            }

            var width = bitmap.PixelWidth;
            var height = bitmap.PixelHeight;
            var numPixels = width * height;
            var bytesPerPixel = format.BitsPerPixel / 8;
            var pixelBuffer = new byte[numPixels * bytesPerPixel];

            bitmap.CopyPixels(pixelBuffer, width * bytesPerPixel, 0);

            long blue = 0;
            long green = 0;
            long red = 0;

            for (int i = 0; i < pixelBuffer.Length; i += bytesPerPixel)
            {
                blue += pixelBuffer[i];
                green += pixelBuffer[i + 1];
                red += pixelBuffer[i + 2];
            }

            return FastColor((byte)(red / numPixels), (byte)(green / numPixels), (byte)(blue / numPixels));
        }

        public static DrawingColor GetColorFromInt(int integer)
        {
            integer = integer switch
            {
                < 0 => 0,
                > 16777215 => 16777215,
                _ => integer
            };

            int r = integer >> 16;
            int g = (integer >> 8) & 255;
            int b = integer & 255;

            return FastColor((byte)r, (byte)g, (byte)b);
        }

        public static int GetIntFromColor(DrawingColor color)
        {
            return (color.R << 16) | (color.G << 8) | (color.B);
        }

        public static void ToHsv(DrawingColor color, out double hue, out double saturation, out double value)
        {
            ToHsv((color.R, color.G, color.B), out hue, out saturation, out value);
        }

        public static void ToHsv((byte r, byte g, byte b) color, out double hue, out double saturation, out double value)
        {
            var max = Math.Max(color.r, Math.Max(color.g, color.b));
            var min = Math.Min(color.r, Math.Min(color.g, color.b));

            var delta = max - min;

            hue = 0d;
            if (delta != 0)
            {
                if (color.r == max) hue = (color.g - color.b) / (double)delta;
                else if (color.g == max) hue = 2d + (color.b - color.r) / (double)delta;
                else if (color.b == max) hue = 4d + (color.r - color.g) / (double)delta;
            }

            hue *= 60;
            if (hue < 0.0) hue += 360;

            saturation = (max == 0) ? 0 : 1d - (1d * min / max);
            value = max / 255d;
        }

        public static DrawingColor FromHsv(double hue, double saturation, double value)
        {
            saturation = Math.Max(Math.Min(saturation, 1), 0);
            value = Math.Max(Math.Min(value, 1), 0);

            var hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            var f = hue / 60 - Math.Floor(hue / 60);

            value *= 255;
            var v = (byte)(value);
            var p = (byte)(value * (1 - saturation));
            var q = (byte)(value * (1 - f * saturation));
            var t = (byte)(value * (1 - (1 - f) * saturation));

            switch (hi)
            {
                case 0: return FastColor(v, t, p);
                case 1: return FastColor(q, v, p);
                case 2: return FastColor(p, v, t);
                case 3: return FastColor(p, q, v);
                case 4: return FastColor(t, p, v);
                default: return FastColor(v, p, q);
            }
        }

        public static (byte r, byte g, byte b) FromHsvSimple(double hue, double saturation, double value)
        {
            saturation = Math.Max(Math.Min(saturation, 1), 0);
            value = Math.Max(Math.Min(value, 1), 0);

            var hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            var f = hue / 60 - Math.Floor(hue / 60);

            value *= 255;
            var v = (byte)(value);
            var p = (byte)(value * (1 - saturation));
            var q = (byte)(value * (1 - f * saturation));
            var t = (byte)(value * (1 - (1 - f) * saturation));

            return hi switch
            {
                0 => (v, t, p),
                1 => (q, v, p),
                2 => (p, v, t),
                3 => (p, q, v),
                4 => (t, p, v),
                _ => (v, p, q)
            };
        }

        /// <summary>
        /// Changes the hue of <paramref name="color"/>
        /// </summary>
        /// <param name="color">Color to be modified</param>
        /// <param name="offset">Hue offset in degrees</param>
        /// <returns>Color with modified hue</returns>
        public static DrawingColor ChangeHue(DrawingColor color, double offset)
        {
            if (offset == 0)
                return color;

            ToHsv(color, out var hue, out var saturation, out var value);

            hue += offset;

            while (hue > 360) hue -= 360;
            while (hue < 0) hue += 360;

            return FromHsv(hue, saturation, value);
        }
        public static (byte r, byte g, byte b) ChangeHue((byte r, byte g, byte b) color, double offset)
        {
            if (offset == 0)
                return color;

            ToHsv(color, out var hue, out var saturation, out var value);

            hue += offset;

            while (hue > 360) hue -= 360;
            while (hue < 0) hue += 360;

            return FromHsvSimple(hue, saturation, value);
        }

        /// <summary>
        /// Changes the brightness of <paramref name="color"/>
        /// </summary>
        /// <param name="color">Color to be modified</param>
        /// <param name="strength">
        /// The strength of brightness change.
        /// <para>Values between (0, 1] increase the brightness by (0%, inf%]</para>
        /// <para>Values between [-1, 0) decrease the brightness by [inf%, 0%)</para>
        /// </param>
        /// <returns>Color with modified brightness</returns>
        public static DrawingColor ChangeBrightness(DrawingColor color, double strength)
        {
            if (strength == 0)
                return color;

            ToHsv(color, out var hue, out var saturation, out var value);
            ChangeHsvComponent(ref value, strength);
            return FromHsv(hue, saturation, value);
        }
        public static (byte r, byte g, byte b) ChangeBrightness((byte r, byte g, byte b) color, double strength)
        {
            if (strength == 0)
                return color;

            ToHsv(color, out var hue, out var saturation, out var value);
            ChangeHsvComponent(ref value, strength);
            return FromHsvSimple(hue, saturation, value);
        }

        /// <summary>
        /// Changes the saturation of <paramref name="color"/>
        /// </summary>
        /// <param name="color">Color to be modified</param>
        /// <param name="strength">
        /// The strength of saturation change.
        /// <para>Values between (0, 1] increase the saturation by (0%, inf%]</para>
        /// <para>Values between [-1, 0) decrease the saturation by [inf%, 0%)</para>
        /// </param>
        /// <returns>Color with modified saturation</returns>
        public static DrawingColor ChangeSaturation(DrawingColor color, double strength)
        {
            if (strength == 0)
                return color;

            ToHsv(color, out var hue, out var saturation, out var value);
            ChangeHsvComponent(ref saturation, strength);
            return FromHsv(hue, saturation, value);
        }
        public static (byte r, byte g, byte b) ChangeSaturation((byte r, byte g, byte b) color, double strength)
        {
            if (strength == 0)
                return color;

            ToHsv(color, out var hue, out var saturation, out var value);
            ChangeHsvComponent(ref saturation, strength);
            return FromHsvSimple(hue, saturation, value);
        }

        private static void ChangeHsvComponent(ref double component, double strength)
        {
            if (component == 0)
                return;

            strength = strength >= 0 ? MathUtils.Clamp(strength, 0, 1) : MathUtils.Clamp(strength, -1, 0);
            if (strength == -1)
            {
                component = 0;
                return;
            }
            else if (strength == 1)
            {
                component = 1;
                return;
            }

            var result = strength >= 0 ? component / (1 - Math.Sin(Math.PI * strength / 2))
                                       : component * (1 - Math.Sin(-Math.PI * strength / 2));
            component = MathUtils.Clamp(result, 0, 1);
        }

        public static MediaColor CloneMediaColor(MediaColor clr)
        {
            return MediaColor.FromArgb(clr.A, clr.R, clr.G, clr.B);
        }

        public static DrawingColor CloneDrawingColor(DrawingColor clr)
        {
            return DrawingColor.FromArgb(clr.ToArgb());
        }

        public static bool NearlyEqual(float a, float b, float epsilon) {
            const double MinNormal = 2.2250738585072014E-308d;
            float absA = Math.Abs(a);
            float absB = Math.Abs(b);
            float diff = Math.Abs(a - b);

            if (a.Equals(b)) { // shortcut, handles infinities
                return true;
            }
            if (a == 0 || b == 0 || absA + absB < MinNormal) {
                // a or b is zero or both are extremely close to it
                // relative error is less meaningful here
                return diff < (epsilon * MinNormal);
            } // use relative error
            return diff / (absA + absB) < epsilon;
        }

        public static bool NearlyEqual(double a, double b, double epsilon) {
            const double MinNormal = 2.2250738585072014E-308d;
            double absA = Math.Abs(a);
            double absB = Math.Abs(b);
            double diff = Math.Abs(a - b);

            if (a.Equals(b)) { // shortcut, handles infinities
                return true;
            }
            if (a == 0 || b == 0 || absA + absB < MinNormal) {
                // a or b is zero or both are extremely close to it
                // relative error is less meaningful here
                return diff < (epsilon * MinNormal);
            } // use relative error
            return diff / (absA + absB) < epsilon;
        }

        public static DrawingColor FastColorTransparent(byte r, byte g, byte b)
        {
            return FastColor(r, g, b, Math.Max(b, Math.Max(g, r)));
        }

        public static DrawingColor FastColor(byte r, byte g, byte b, byte a = 255)
        {
            return DrawingColor.FromArgb(
                b | (g << 8) | (r << 16) | (a << 24)
            );
        }
    }

    /// <summary>
    /// Converts a <see cref="DrawingColor"/> to a <see cref="System.Windows.Media.Color"/> and back.
    /// </summary>
    public class DrawingMediaColorConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => ColorUtils.DrawingColorToMediaColor((DrawingColor)value);
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => ColorUtils.MediaColorToDrawingColor((MediaColor)value);
    }

    /// <summary>
    /// Converts between a RealColor and Media color so that the RealColor class can be used with the Xceed Color Picker
    /// </summary>
    public class RealColorConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => ((RealColor)value).GetMediaColor();
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => new RealColor((MediaColor)value);
    }

    /// <summary>
    /// Class to convert between a <see cref="EffectsEngine.EffectBrush"></see> and a <see cref="System.Windows.Media.Brush"></see> so that it can be
    /// used with the ColorBox gradient editor control.
    /// </summary>
    public class EffectMediaBrushConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => ((EffectsEngine.EffectBrush)value).GetMediaBrush();
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => new EffectsEngine.EffectBrush((System.Windows.Media.Brush)value);
    }

    public class BoolToColorConverter : IValueConverter
    {
        public static Tuple<DrawingColor, DrawingColor> TextWhiteRed = new(DrawingColor.FromArgb(255, 186, 186, 186), DrawingColor.Red);

        public static Tuple<DrawingColor, DrawingColor> TextRedWhite = new(DrawingColor.Red, DrawingColor.FromArgb(255, 186, 186, 186));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool b = (bool)value;
            Tuple<DrawingColor, DrawingColor> clrs = parameter as Tuple<DrawingColor, DrawingColor> ?? TextWhiteRed;
            DrawingColor clr = b ? clrs.Item1 : clrs.Item2;

            return new System.Windows.Media.SolidColorBrush(ColorUtils.DrawingColorToMediaColor(clr));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class RealColor : ICloneable
    {
        [JsonProperty]
        private DrawingColor Color { get; set; }

        public RealColor()
        {
            Color = DrawingColor.Transparent;
        }

        public RealColor(MediaColor clr)
        {
            this.SetMediaColor(clr);
        }

        public RealColor(DrawingColor color)
        {
            this.Color = color.Clone();
        }

        public DrawingColor GetDrawingColor()
        {
            return Color.Clone();
        }

        public MediaColor GetMediaColor()
        {
            return Color.ToMediaColor();
        }

        public void SetDrawingColor(DrawingColor clr)
        {
            this.Color = clr.Clone();
        }

        public void SetMediaColor(MediaColor clr)
        {
            this.Color = clr.ToDrawingColor();
        }

        public object Clone()
        {
            return new RealColor(this.Color.Clone());
        }

        public static implicit operator DrawingColor(RealColor c) => c.GetDrawingColor();
        public static implicit operator MediaColor(RealColor c) => c.GetMediaColor();
    }
}
