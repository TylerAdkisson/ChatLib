using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChatLib
{
    public class ChatterStatusGroupItem
    {
        public string FullName { get; private set; }
        public string ShortName { get; private set; }
        public string Description { get; private set; }
        public string PreferredBackgroundColor { get; private set; }


        public ChatterStatusGroupItem(string fullName, string shortName, string description)
            : this(fullName, shortName, description, "")
        {
        }

        public ChatterStatusGroupItem(string fullName, string shortName, string description, string backgroundColor)
        {
            FullName = fullName;
            ShortName = shortName;
            Description = description;
            PreferredBackgroundColor = backgroundColor;
        }
    }
}
