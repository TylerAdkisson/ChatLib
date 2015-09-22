using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatLib
{
    public class TextRun
    {
        [Flags]
        public enum StyleFlags
        {
            None = 0,
            Bold = 0x01,
            Italic = 0x02,
            Underline = 0x04,
            Strikethrough = 0x10,
        };

        public enum RunKind
        {
            Text,
            Image,
        };


        public string Text { get; set; }
        public string Content { get; set; }
        public StyleFlags Style { get; set; }
        public string Color { get; set; }
        public RunKind Kind { get; set; }

        public double FontSize { get; set; }
        public string FontFamily { get; set; }


        public TextRun(string text)
            : this(text, StyleFlags.None, "")
        {

        }

        public TextRun(string text, StyleFlags style, string color)
        {
            Text = text;
            Content = text;
            Style = style;
            Color = color;
        }


        public static implicit operator TextRun(string value)
        {
            return new TextRun(value);
        }


        public override string ToString()
        {
            return string.Format("\"{0}\" Style: {1} Color: {2} Kind: {3} Content: {4}",
                Text,
                Style,
                Color,
                Kind,
                Content);
        }
    }
}
