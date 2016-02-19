using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChatLib.Twitch
{
    class TwitchChatterList
    {
        [JsonProperty("moderators")]
        public IList<string> Moderators;

        [JsonProperty("staff")]
        public IList<string> Staff;

        [JsonProperty("admins")]
        public IList<string> Admins;

        [JsonProperty("global_mods")]
        public IList<string> GlobalMods;

        [JsonProperty("viewers")]
        public IList<string> Viewers;
    }

    class TwitchViewerList : IViewerList
    {
        [JsonProperty("chatter_count")]
        private int _viewerCount;

        [JsonProperty("chatters")]
        private TwitchChatterList _chatters;


        public int TotalViewerCount
        {
            get { return _viewerCount; }
        }


        public IList<string> GetGroups()
        {
            return new[] { "viewers", "moderators", "global_mods", "admins", "staff" };
        }

        public IList<string> GetViewers(string category)
        {
            if (_chatters == null)
                return null;

            switch(category)
            {
                case "viewers":
                    return _chatters.Viewers;
                case "moderators":
                    return _chatters.Moderators;
                case "global_mods":
                    return _chatters.GlobalMods;
                case "admins":
                    return _chatters.Admins;
                case "staff":
                    return _chatters.Staff;
            }

            return null;
        }

        public IList<string> GetAllViewers()
        {
            List<string> allNames = new List<string>(_viewerCount);

            allNames.AddRange(_chatters.Viewers);
            allNames.AddRange(_chatters.Moderators);
            allNames.AddRange(_chatters.GlobalMods);
            allNames.AddRange(_chatters.Admins);
            allNames.AddRange(_chatters.Staff);

            return allNames;
        }

    }
}
