using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatLib.Twitch
{
    public class TwitchIrcService : IChatService
    {
        private const string ApiAcceptString = "application/vnd.twitchtv.v3+json";
        public const string EmoteUri = "http://static-cdn.jtvnw.net/emoticons/v1/:emote_id/1.0";

        private string _nickname;
        private string _authToken;
        private List<TwitchIrcChannel> _channels;
        private Dictionary<EndPoint, ServerConnection> _connectionRegistry;


        internal string Nickname { get { return _nickname; } }
        internal string AuthToken { get { return _authToken; } }


        public TwitchIrcService()
        {
            _channels = new List<TwitchIrcChannel>();
            _connectionRegistry = new Dictionary<EndPoint, ServerConnection>();
        }


        public void Initialize()
        {
        }

        public void SetDefaultServer(string hostnameOrIPAddress, int port)
        {
        }

        public void SetDefaultAuthentication(string name, string key)
        {
            _nickname = name;
            _authToken = key;

            if (!key.StartsWith("oauth:"))
                _authToken = "oauth:" + key;
        }

        public IChatChannel ConnectChannel(string channelName)
        {
            TwitchIrcChannel channelInstance = new TwitchIrcChannel(
                this,
                channelName.TrimStart('#'));
            _channels.Add(channelInstance);

            return channelInstance;
        }

        public object Upgrade()
        {
            return null;
        }

        public void Dispose()
        {
            // TODO: Cleanup all socket connections
            for (int i = 0; i < _channels.Count; i++)
            {
                _channels[i].Leave();
            }
            _channels.Clear();
        }


        internal DnsEndPoint[] GetChatServers(string channelName)
        {
            if (string.IsNullOrWhiteSpace(channelName))
                throw new ArgumentNullException("channelName");

            string chatPropertyUri = string.Concat(
                "http://api.twitch.tv/api/channels/",
                channelName,
                "/chat_properties");
            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(chatPropertyUri);
            req.Accept = ApiAcceptString;

            HttpWebResponse response = null;
            try
            {
                response = (HttpWebResponse)req.GetResponse();
            }
            catch (WebException ex)
            {
                Console.WriteLine("Request failed due to \"{0}\"", ex.Message);
                return null;
            }
            catch (TimeoutException)
            {
                Console.WriteLine("Request timed out.");
                return null;
            }

            DnsEndPoint[] results = null;

            // Reader owns base stream and will dispose it for us
            using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
            {
                string responseText = reader.ReadToEnd();

                // NOTE: We're doing the parsing manually, as I don't want to add an entire JSON
                //   library just to parse this response.

                int index = responseText.IndexOf("\"chat_servers\":");
                if (index < 0)
                    return null;

                // Find array begin char
                index = responseText.IndexOf('[', index);
                if (index < 0)
                    return null;

                // Find array end char
                int endIndex = responseText.IndexOf(']', index);
                if (endIndex < 0)
                    return null;

                string[] serverList = responseText.Substring(index + 1, (endIndex - index) - 1).Split(',');

                results = new DnsEndPoint[serverList.Length];
                for (int i = 0; i < serverList.Length; i++)
                {
                    int portIndex = serverList[i].LastIndexOf(':');

                    string hostname = serverList[i].Remove(portIndex).TrimStart('"');
                    string portString = serverList[i].Substring(portIndex + 1).TrimEnd('"');

                    results[i] = new DnsEndPoint(hostname, int.Parse(portString));
                }
            }

            return results;
        }

    }
}
