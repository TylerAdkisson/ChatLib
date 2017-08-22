using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;


namespace ChatLib.Twitch
{
    public class TwitchIrcService : IChatService
    {
        private const string Src = "TwitchIrcService";
        private const string ApiAcceptString = "application/vnd.twitchtv.v3+json";
        internal const string EmoteUri = "http://static-cdn.jtvnw.net/emoticons/v1/:emote_id/1.0";
        internal const string ViewerListUri = "http://tmi.twitch.tv/group/user/:channel/chatters";

        private string _nickname;
        private string _authToken;
        private List<TwitchIrcChannel> _channels;
        private List<TwitchWhisperChannel> _whispers;
        private Dictionary<EndPoint, IrcServerConnection> _connectionRegistry;
        private List<ChatterStatusGroup> _statusGroups;


        internal string Nickname { get { return _nickname; } }
        internal string AuthToken { get { return _authToken; } }


        public TwitchIrcService()
        {
            _channels = new List<TwitchIrcChannel>();
            _whispers = new List<TwitchWhisperChannel>();
            _connectionRegistry = new Dictionary<EndPoint, IrcServerConnection>();

            _statusGroups = new List<ChatterStatusGroup>();

            // Capabilities group
            List<ChatterStatusGroupItem> statuses = new List<ChatterStatusGroupItem>();
            statuses.Add(new ChatterStatusGroupItem("Moderator", "Mod", "Moderates this channel", "#34ae0a"));
            statuses.Add(new ChatterStatusGroupItem("Global Moderator", "GMod", "Moderates all channels", "#34ae0a"));
            statuses.Add(new ChatterStatusGroupItem("Administrator", "Admin", "Helps maintain the site", "#faaf19"));
            statuses.Add(new ChatterStatusGroupItem("Staff", "Staff", "Twitch staff member", "#200f33"));
            //#ae1010

            _statusGroups.Add(new ChatterStatusGroup(0, statuses));

            // Turbo group
            statuses = new List<ChatterStatusGroupItem>();
            statuses.Add(new ChatterStatusGroupItem("Twitch Turbo", "Turbo", "Subscribes to Twitch Turbo", "#6441a5"));

            _statusGroups.Add(new ChatterStatusGroup(1, statuses));

            // Subscriber group
            statuses = new List<ChatterStatusGroupItem>();
            statuses.Add(new ChatterStatusGroupItem("Channel Subscriber", "Sub", "Subscribes to this channel", "#3059BF"));

            _statusGroups.Add(new ChatterStatusGroup(2, statuses));
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
                channelName.ToLower().TrimStart('#'));
            _channels.Add(channelInstance);

            return channelInstance;
        }

        public IPrivateMessageChannel ConnectPrivateMessage()
        {
            TwitchWhisperChannel whisperInstance = new TwitchWhisperChannel(this);
            _whispers.Add(whisperInstance);

            return whisperInstance;
        }

        public object Upgrade()
        {
            return null;
        }

        public ReadOnlyCollection<ChatterStatusGroup> GetStatusGroups()
        {
            return _statusGroups.AsReadOnly();
        }

        public void Dispose()
        {
            // Cleanup all socket connections
            for (int i = 0; i < _channels.Count; i++)
            {
                _channels[i].Leave();
            }
            _channels.Clear();

            for (int i = 0; i < _whispers.Count; i++)
            {
                _whispers[i].Leave();
            }
            _whispers.Clear();
        }


        public IPEndPoint[] GetChatServers(string channelName, bool isGroup)
        {
            if (!isGroup && Net40.StringIsNullOrWhiteSpace(channelName))
                throw new ArgumentNullException("channelName");

            string chatPropertyUri = "";

            if (isGroup)
            {
                chatPropertyUri = "http://tmi.twitch.tv/servers?cluster=group";
            }
            else
            {
                chatPropertyUri = string.Concat(
                     "http://api.twitch.tv/api/channels/",
                     channelName,
                     "/chat_properties");
            }
            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(chatPropertyUri);
            req.Accept = ApiAcceptString;

            HttpWebResponse response = null;
            try
            {
                response = (HttpWebResponse)req.GetResponse();
            }
            catch (WebException ex)
            {
                Log.Error(Src, "Request failed due to \"{0}\"", ex.Message);
                return null;
            }
            catch (TimeoutException)
            {
                Log.Error(Src, "Request timed out.");
                return null;
            }

            List<IPEndPoint> results = new List<IPEndPoint>(3);

            // Reader owns base stream and will dispose it for us
            using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
            {
                JsonReader jsonReader = new JsonTextReader(reader);

                JsonSerializer serial = JsonSerializer.Create();
                ServerList list = serial.Deserialize<ServerList>(jsonReader);
                IList<string> serverList = isGroup ? list.Servers : list.ChatServers;

                for (int i = 0; i < serverList.Count; i++)
                {
                    int portIndex = serverList[i].LastIndexOf(':');

                    string hostname = serverList[i].Remove(portIndex);
                    string portString = serverList[i].Substring(portIndex + 1);

                    IPAddress[] hostAddresses = new IPAddress[1];
                    int hostPort = 0;

                    // Attempt to parse as an IP address first, so we don't take a DNS
                    //   hit every time we come across an IP address
                    if (!IPAddress.TryParse(hostname, out hostAddresses[0]))
                    {
                        // It's probably a hostname, do a lookup
                        try
                        {
                            IPHostEntry entry = Dns.GetHostEntry(hostname);
                            hostAddresses = entry.AddressList;
                            Log.Debug(Src, "Resolved {0} to {1} address(es)", hostname, hostAddresses.Length);
                        }
                        catch (SocketException)
                        {
                            // Bad host name
                            continue;
                        }
                    }
                    
                    if (!int.TryParse(portString, out hostPort) ||
                        hostPort < 1 || hostPort > 65535)
                    {
                        // Bad port number
                        continue;
                    }

                    // Add entries for all addresses of a host
                    for (int p = 0; p < hostAddresses.Length; p++)
                    {
                        Log.Debug(Src, "Chat host: {0}:{1}", hostAddresses[p], hostPort);
                        results.Add(new IPEndPoint(hostAddresses[p], hostPort));
                    }
                }
            }

            response.Close();

            return results.ToArray();
        }
    }
}
