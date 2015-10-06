using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace ChatLib
{
    public class ChatterInfo
    {
        /// <summary>
        /// Gets or set the possibly-formatted name of the chatter
        /// </summary>
        public TextRun Name { get; set; }

        /// <summary>
        /// Gets or sets the status of the chatter (i.e. moderator, administrator, etc.)
        /// </summary>
        public ReadOnlyCollection<ChatterStatusGroupItem> StatusGroupMembership { get; set; }


        public ChatterInfo(TextRun name)
            : this(name, null)
        {
        }

        public ChatterInfo(TextRun name, IEnumerable<ChatterStatusGroupItem> statusMembers)
            : this(name, new List<ChatterStatusGroupItem>(statusMembers ?? Enumerable.Empty<ChatterStatusGroupItem>()))
        {
        }

        public ChatterInfo(TextRun name, IList<ChatterStatusGroupItem> statusMembers)
        {
            Name = name;

            if (statusMembers == null)
                statusMembers = new List<ChatterStatusGroupItem>(0);

            StatusGroupMembership = new ReadOnlyCollection<ChatterStatusGroupItem>(statusMembers);
        }
    }
}
