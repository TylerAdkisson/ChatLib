using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChatLib
{
    public interface IViewerList
    {
        /// <summary>
        /// Gets the total number of viewers across all groups
        /// </summary>
        int TotalViewerCount { get; }

        /// <summary>
        /// Gets a list of chatter groups
        /// </summary>
        /// <returns>A list of chatter groups</returns>
        IList<string> GetGroups();

        /// <summary>
        /// Gets a list of users from a group
        /// </summary>
        /// <param name="group">A group acquired from a call to GetGroups()</param>
        /// <returns>A list of the names of all users in a group</returns>
        IList<string> GetViewers(string group);

        /// <summary>
        /// Gets a list of all users from all groups. 
        /// </summary>
        /// <returns>A list of the names of all users from all groups</returns>
        IList<string> GetAllViewers();
    }
}
