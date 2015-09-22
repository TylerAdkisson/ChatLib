using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatLib
{
    class ConsoleColorConverter
    {
        static List<Tuple<ConsoleColor, string>> _colors;
        static uint _counter; 

        static ConsoleColorConverter()
        {
            _colors = new List<Tuple<ConsoleColor, string>>();
            //_colors.Add(Tuple.Create(ConsoleColor.Black, "000000"));
            //_colors.Add(Tuple.Create(ConsoleColor.DarkBlue, "000080"));
            _colors.Add(Tuple.Create(ConsoleColor.DarkGreen, "008000"));
            _colors.Add(Tuple.Create(ConsoleColor.DarkCyan, "008080"));
            _colors.Add(Tuple.Create(ConsoleColor.DarkRed, "800000"));
            _colors.Add(Tuple.Create(ConsoleColor.DarkMagenta, "800080"));
            _colors.Add(Tuple.Create(ConsoleColor.DarkYellow, "808000"));
            //_colors.Add(Tuple.Create(ConsoleColor.Gray, "C0C0C0"));
            //_colors.Add(Tuple.Create(ConsoleColor.DarkGray, "808080"));
            //_colors.Add(Tuple.Create(ConsoleColor.Blue, "FF0000"));
            _colors.Add(Tuple.Create(ConsoleColor.Green, "00FF00"));
            _colors.Add(Tuple.Create(ConsoleColor.Cyan, "00FFFF"));
            _colors.Add(Tuple.Create(ConsoleColor.Red, "FF0000"));
            _colors.Add(Tuple.Create(ConsoleColor.Magenta, "FF00FF"));
            _colors.Add(Tuple.Create(ConsoleColor.Yellow, "FFFF00"));
            _colors.Add(Tuple.Create(ConsoleColor.White, "FFFFFF"));
        }

        public static ConsoleColor GetColor()
        {
            int index = (int)(_counter++ % _colors.Count);
            return _colors[index].Item1;
        }

        public static ConsoleColor HexToColor(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
                return ConsoleColor.Gray;

            hex = hex.TrimStart('#');

            char[] chars = hex.ToCharArray();

            byte red = byte.Parse(new string(chars, 0, 2), System.Globalization.NumberStyles.HexNumber);
            byte green = byte.Parse(new string(chars, 2, 2), System.Globalization.NumberStyles.HexNumber);
            byte blue = byte.Parse(new string(chars, 4, 2), System.Globalization.NumberStyles.HexNumber);

            //double l, a, b;
            //double x, y, z;
            //RgbToXyz(red, green, blue, out x, out y, out z);
            //XyzToLab(x, y, z, out l, out a, out b);

            double mindist = double.MaxValue;
            ConsoleColor color = ConsoleColor.Gray;

            foreach (var item in _colors)
            {
                char[] chars2 = item.Item2.ToCharArray();

                byte red2 = byte.Parse(new string(chars2, 0, 2), System.Globalization.NumberStyles.HexNumber);
                byte green2 = byte.Parse(new string(chars2, 2, 2), System.Globalization.NumberStyles.HexNumber);
                byte blue2 = byte.Parse(new string(chars2, 4, 2), System.Globalization.NumberStyles.HexNumber);

                //double l2, a2, b2;
                //RgbToXyz(red2, green2, blue2, out x, out y, out z);
                //XyzToLab(x, y, z, out l2, out a2, out b2);

                double rdiff = (red - red2);
                double gdiff = (green - green2);
                double bdiff = (blue - blue2);
                double dist = Math.Sqrt((rdiff * rdiff) + (gdiff * gdiff) + (bdiff * bdiff));

                if (dist < mindist)
                {
                    mindist = dist;
                    color = item.Item1;
                }
            }

            return color;
        }

        static void RgbToXyz(int r, int g, int b, out double x, out double y, out double z)
        {
            double R = r / 255.0;
            double G = g / 255.0;
            double B = b / 255.0;

            if (R > 0.04045) R = Math.Pow((R + 0.055) / 1.055, 2.4);
            else R /= 12.92;

            if (G > 0.04045) G = Math.Pow((G + 0.055) / 1.055, 2.4);
            else G /= 12.92;

            if (B > 0.04045) B = Math.Pow((B + 0.055) / 1.055, 2.4);
            else B /= 12.92;

            R *= 100;
            G *= 100;
            B *= 100;

            x = (R * 0.4124) + (G * 0.3576) + (B * 0.1805);
            y = (R * 0.2126) + (G * 0.7152) + (B * 0.0722);
            z = (R * 0.0193) + (G * 0.1192) + (B * 0.9505);
        }

        static void XyzToLab(double x, double y, double z, out double l, out double a, out double b)
        {
            double X = x / 95.047;
            double Y = x / 100.000;
            double Z = x / 108.883;

            if (X > 0.008856) X = Math.Pow(X, (1.0 / 3));
            else X = (7.787 * X) + (16.0 / 116);
            if (Y > 0.008856) Y = Math.Pow(Y, (1.0 / 3));
            else Y = (7.787 * Y) + (16.0 / 116);
            if (Z > 0.008856) Z = Math.Pow(Z, (1.0 / 3));
            else Z = (7.787 * Z) + (16.0 / 116);

            l = (116 * Y) - 16;
            a = 500 * (X - Y);
            b = 200 * (Y - Z);
        }
    }
}
