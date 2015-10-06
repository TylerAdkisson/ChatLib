using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace ChatLib
{
    public class ChatterStatusGroup
    {
        public int GroupId { get; private set; }
        public ReadOnlyCollection<ChatterStatusGroupItem> GroupItems { get; private set; }


        public ChatterStatusGroup(int groupId, IEnumerable<ChatterStatusGroupItem> items)
            : this(groupId, new List<ChatterStatusGroupItem>(items ?? Enumerable.Empty<ChatterStatusGroupItem>()))
        {
        }

        public ChatterStatusGroup(int groupId, IList<ChatterStatusGroupItem> items)
        {
            GroupId = groupId;

            if (items == null)
                items = new List<ChatterStatusGroupItem>(0);

            GroupItems = new ReadOnlyCollection<ChatterStatusGroupItem>(items);
        }
    }
}
