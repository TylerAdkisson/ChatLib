using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChatLib
{
    public static class Utilities
    {
        public static string UnicodeSubstring(this string str, int startIndex)
        {
            return UnicodeSubstring(str, startIndex, int.MaxValue);
        }

        public static string UnicodeSubstring(this string str, int startIndex, int length)
        {
            startIndex = AdjustCharIndex(str, 0, startIndex);
            if(length == int.MaxValue)
                return str.Substring(startIndex);

            length = AdjustCharIndex(str, startIndex, length);
            return str.Substring(startIndex, length);
        }

        public static int AdjustCharIndex(string str, int index)
        {
            return AdjustCharIndex(str, 0, index);
        }

        public static int AdjustCharIndex(string str, int startIndex, int index)
        {
            int adjustedIndex = index;

            // Adjust for surrogate pairs leading up to start index
            for (int i = startIndex; i < adjustedIndex; i++)
            {
                if (str[i] < (char)0xD800 || str[i] >= (char)0xDC00)
                    continue;

                // High surrogate pair found, advance index
                adjustedIndex++;
            }

            return adjustedIndex;
        }
    }
}
