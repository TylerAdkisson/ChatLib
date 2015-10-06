using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace ChatLib
{
    [System.Diagnostics.DebuggerDisplay("\"{Text}\" Style: {Style} Color: {Color} Kind: {Kind} Content: {Content}")]
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


        /// <summary>
        /// Gets or sets the text representation of the content of the run
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the type-dependent content of the run
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Gets or sets formatting style flags
        /// </summary>
        public StyleFlags Style { get; set; }

        /// <summary>
        /// Gets or sets the color for the run in hexidecimal (e.g. #FF00FF for magenta)
        /// </summary>
        public string Color { get; set; }

        /// <summary>
        /// Gets or sets the type of run
        /// </summary>
        public RunKind Kind { get; set; }

        /// <summary>
        /// Gets or sets the font size of text content in the run
        /// </summary>
        public double FontSize { get; set; }

        /// <summary>
        /// Gets or sets the font family of text content in the run
        /// </summary>
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
            return Text;

            return string.Format("\"{0}\" Style: {1} Color: {2} Kind: {3} Content: {4}",
                Text,
                Style,
                Color,
                Kind,
                Content);
        }
    }
}
