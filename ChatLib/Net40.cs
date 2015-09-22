using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChatLib
{
    /// <summary>
    /// Collection of useful functions from .NET 4 that are missing from .NET 3.5
    /// </summary>
    static class Net40
    {
        public static bool StringIsNullOrWhiteSpace(string value)
        {
            return value == null || value.Trim().Equals(string.Empty);
        }
    }
}
