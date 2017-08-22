using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChatLib.Twitch
{
    class ServerList
    {
        [JsonProperty("servers")]
        public IList<string> Servers;

        [JsonProperty("chat_servers")]
        public IList<string> ChatServers;

    }
}
