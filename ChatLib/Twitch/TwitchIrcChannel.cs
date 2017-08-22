using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;


namespace ChatLib.Twitch
{
    public class TwitchIrcChannel : IChatChannel
    {
        const string Src = "TwitchIrcChannel";

        private static uint Counter = 0;
        private uint Number = 0;
        private TwitchIrcService _service;
        private string _channelName;
        private IrcServerConnection _connection;


        public string ChannelName { get { return _channelName; } }

        public IChatService ParentService { get { return _service; } }



        public event EventHandler OnJoin;

        public event ChannelLeaveEventHandler OnLeave;

        public event ChatterJoinLeaveEventHandler OnChatterJoin;

        public event ChatterJoinLeaveEventHandler OnChatterLeave;

        public event ChatMessageEventHandler OnChatMessage;

        public event ChatMessageEventHandler OnChannelNotice;

        public event ChatMessagesDeletedEventHandler OnMessagesDeleted;

        public event ChatViewerListEventHandler OnViewerListCompleted;


        public TwitchIrcChannel(TwitchIrcService service, string channelName)
        {
            _service = service;
            _channelName = channelName;

            Number = Counter++;
        }



        public void SetAuthentication(string name, string key)
        {
        }

        public void Join()
        {
            ThreadPool.QueueUserWorkItem(JoinWorkerThread);
        }

        public void Leave()
        {
            IrcServerConnection connection = _connection;
            if (connection == null)
                return;

            connection.SendIrcCommand(new IrcMessage(IrcCommands.Part, "#" + _channelName));

            connection.OnLineReceived -= ProcessIrcMessage;
            connection.OnConnected -= ServerConnected;

            connection.Release();
            _connection = null;

            RaiseOnLeave(LeaveReason.ChannelLeave);
        }

        public void SendMessage(string message)
        {
            IrcMessage msg = new IrcMessage(
                IrcCommands.PrivateMessage,
                "#" + _channelName,
                message);

            _connection.SendIrcCommand(msg);
        }

        public void SendMessage(IEnumerable<TextRun> formattedMessage)
        {
            // IRC doesn't support formatted messages, just send text
            StringBuilder sb = new StringBuilder();
            foreach (var run in formattedMessage)
            {
                sb.Append(run.Text);
            }

            IrcMessage msg = new IrcMessage(
                IrcCommands.PrivateMessage,
                "#" + _channelName,
                sb.ToString());

            _connection.SendIrcCommand(msg);
        }

        public void GetViewerList()
        {
            WebRequest request = WebRequest.Create(TwitchIrcService.ViewerListUri.Replace(":channel", _channelName));
            request.ContentType = "application/json";
            request.Timeout = 5000;

            // Initiate the request
            request.BeginGetResponse(ViewerListCallback, request);
        }


        private void JoinWorkerThread(object state)
        {
            // Lookup which servers to connect to for this channel
            IPEndPoint[] servers = _service.GetChatServers(_channelName, false);
            if (servers == null || servers.Length == 0)
            {
                // Could not connect
                RaiseOnLeave(LeaveReason.Error);
                return;
            }

            IrcServerConnection connection = IrcServerConnection.ConnectServer(servers);
            if (connection == null)
            {
                // Could not connect
                RaiseOnLeave(LeaveReason.Error);
                return;
            }

            connection.OnLineReceived += ProcessIrcMessage;
            _connection = connection;

            JoinChannel();

            // Hook up handler here so we don't call JoinChannel() twice
            connection.OnConnected += ServerConnected;
        }

        private void JoinChannel()
        {
            // Auth server
            lock (_connection)
            {
                if (!_connection.HasAuthenticated)
                {
                    _connection.Authenticate(_service.Nickname, _service.AuthToken);
                }
            }

            // Auth channel
            // We don't auth to channels for twitch

            // Join channel
            // NOTE: We do not need to join a channel to participate in whispers, but we join anyway
            //   so we get the join message for things that expect it
            _connection.SendIrcCommand(new IrcMessage(IrcCommands.Join, "#" + _channelName));

            // TODO: Wait and call OnJoin after receiving the join message?
            //RaiseOnJoin();
        }

        private void ServerConnected(object sender, EventArgs e)
        {
            JoinChannel();
        }

        private void ProcessIrcMessage(object sender, IrcMessage line)
        {
            switch (line.Command)
            {
                case IrcCommands.PrivateMessage:
                    ChatMessage message = new ChatMessage();
                    message.Timestamp = DateTime.Now;
                    message.Author = new ChatterInfo(line.Source.Remove(line.Source.IndexOf('!')));
                    message.Id = line.Source.Remove(line.Source.IndexOf('!'));

                    // Strip CTCP formatting
                    // We only support ACTION for twitch
                    string messageText = line.Text;
                    int actionIndex = messageText.IndexOf("\x0001ACTION");
                    if (actionIndex > -1)
                    {
                        message.MessageKind = ChatMessage.Kind.Action;
                        int messageLength = messageText.IndexOf('\x0001', actionIndex + 7) - actionIndex;
                        messageText = messageText.Substring(actionIndex + 8, messageLength - 8);
                    }

                    // Unformatted text
                    message.AppendRun(messageText);

                    ParseTags(line.Tags, message);

                    RaiseOnChatMessage(message);

                    break;
                case IrcCommands.Join:
                    if (line.Source.StartsWith(_service.Nickname))
                        RaiseOnJoin();

                    RaiseOnChatterJoin(line.Source.Remove(line.Source.IndexOf('!')));

                    break;
                case IrcCommands.Part:
                    if (line.Source.StartsWith(_service.Nickname))
                        RaiseOnLeave(LeaveReason.ChannelLeave);

                    RaiseOnChatterLeave(line.Source.Remove(line.Source.IndexOf('!')));

                    break;
                case IrcCommands.Notice:

                    ChatMessage notice = new ChatMessage();
                    notice.Timestamp = DateTime.Now;
                    notice.Author = new ChatterInfo("#" + _channelName);

                    notice.AppendRun(line.Text);

                    RaiseOnChannelNotice(notice);

                    break;
                case IrcCommands.ClearChat:

                    RaiseOnMessagesDeleted(line.Text);

                    break;
                default:
                    break;
            }
        }

        private void ParseTags(string messageTags, ChatMessage message)
        {
            if (Net40.StringIsNullOrWhiteSpace(messageTags))
                return;

            string[] tags = messageTags.Split(';');
            List<ChatterStatusGroupItem> groups = new List<ChatterStatusGroupItem>(new ChatterStatusGroupItem[3]);

            for (int i = 0; i < tags.Length; i++)
            {
                int eqIndex = tags[i].IndexOf('=');
                string key = tags[i];
                string value = "";

                if (eqIndex > -1)
                {
                    // Some tags have no value
                    key = tags[i].Remove(eqIndex);
                    value = tags[i].Substring(eqIndex + 1);
                }

                switch (key)
                {
                    case "color":
                        message.Author.Name.Color = value;
                        break;
                    case "display-name":
                        if (!string.IsNullOrEmpty(value))
                            message.Author.Name.Text = message.Author.Name.Content = value;
                        break;
                    case "emotes":
                        if (string.IsNullOrEmpty(value))
                            break; // No emotes

                        string[] emotes = value.Split('/');

                        // First run is the full message text
                        LinkedList<TextRun> runs = new LinkedList<TextRun>();
                        runs.AddFirst(message.TextRuns[0]);

                        for (int p = 0; p < emotes.Length; p++)
                        {
                            int colonIndex = emotes[p].IndexOf(':');
                            int emoteId = int.Parse(emotes[p].Remove(colonIndex));
                            string[] extents = emotes[p].Substring(colonIndex + 1).Split(',');

                            for (int k = 0; k < extents.Length; k++)
                            {
                                int dashIndex = extents[k].IndexOf('-');

                                int emoteStart = int.Parse(extents[k].Remove(dashIndex));
                                int emoteEnd = int.Parse(extents[k].Substring(dashIndex + 1));

                                // Adjust emote start/end for UTF-16 sillyness
                                int emoteStartAdj = Utilities.AdjustCharIndex(message.TextRuns[0].Text, 0, emoteStart);
                                emoteEnd = Utilities.AdjustCharIndex(message.TextRuns[0].Text, emoteStartAdj, emoteEnd);

                                emoteEnd += (emoteStartAdj - emoteStart);
                                emoteStart = emoteStartAdj;

                                LinkedListNode<TextRun> theRun = null;
                                int position = 0;

                                // Find run that contains the emote
                                LinkedListNode<TextRun> node = runs.First;
                                while (node != null)
                                {
                                    if (position <= emoteStart &&
                                        (position + node.Value.Text.Length) >= emoteEnd)
                                    {
                                        theRun = node;
                                        break;
                                    }
                                    position += node.Value.Text.Length;

                                    // Move to next item
                                    node = node.Next;
                                }

                                System.Diagnostics.Debug.Assert(theRun != null, "theRun is null?!");

                                // Split run
                                string subStr = theRun.Value.Text.Substring(emoteStart - position, emoteEnd - emoteStart + 1);
                                TextRun emoteRun = new TextRun(subStr);

                                emoteRun.Kind = TextRun.RunKind.Image;
                                emoteRun.Content = TwitchIrcService.EmoteUri.Replace(":emote_id", emoteId.ToString());

                                if (emoteStart - position > 0)
                                    runs.AddBefore(theRun, new TextRun(theRun.Value.Text.Remove(emoteStart - position)));

                                runs.AddBefore(theRun, emoteRun);

                                if ((position + theRun.Value.Text.Length) - (emoteEnd + 1) > 0)
                                    runs.AddBefore(theRun, new TextRun(theRun.Value.Text.Substring(emoteEnd + 1 - position)));

                                runs.Remove(theRun);
                            }
                        }

                        message.ClearRuns();
                        message.AppendRuns(runs);

                        break;
                    case "user-type":
                        switch (value)
                        {
                            case "mod":
                                groups[0] = _service.GetStatusGroups()[0].GroupItems[0];
                                break;
                            case "global_mod":
                                groups[0] = _service.GetStatusGroups()[0].GroupItems[1];
                                break;
                            case "admin":
                                groups[0] = _service.GetStatusGroups()[0].GroupItems[2];
                                break;
                            case "staff":
                                groups[0] = _service.GetStatusGroups()[0].GroupItems[3];
                                break;
                            default:
                                break;
                        }

                        break;
                    case "turbo":
                        if (value == "1")
                            groups[1] = _service.GetStatusGroups()[1].GroupItems[0];
                        break;
                    case "subscriber":
                        if(value == "1")
                            groups[2] = _service.GetStatusGroups()[2].GroupItems[0];
                        break;
                    default:
                        break;
                }
            }

            message.Author.StatusGroupMembership = groups.AsReadOnly();
        }

        private void ViewerListCallback(IAsyncResult ar)
        {
            WebRequest request = (WebRequest)ar.AsyncState;
            try
            {
                WebResponse response = request.EndGetResponse(ar);
                TwitchViewerList viewerList;
                using (Stream str = response.GetResponseStream())
                {
                    using (StreamReader sr = new StreamReader(str))
                    {
                        JsonReader reader = new JsonTextReader(sr);
                        JsonSerializer serial = JsonSerializer.Create();
                        viewerList = serial.Deserialize<TwitchViewerList>(reader);
                    }
                }

                response.Close();

                RaiseOnViewerListCompleted(true, viewerList);
            }
            catch (Exception ex)
            {
                Log.Error(Src, "Failed to get viewer list: {0}", ex.Message);

                // Something went wrong
                RaiseOnViewerListCompleted(false, null);
            }
        }


        private void RaiseOnJoin()
        {
            EventHandler handler = OnJoin;
            if (handler == null)
                return;

            handler(this, EventArgs.Empty);
        }

        private void RaiseOnLeave(LeaveReason reason)
        {
            ChannelLeaveEventHandler handler = OnLeave;
            if (handler == null)
                return;

            handler(this, reason);
        }

        private void RaiseOnChatMessage(ChatMessage message)
        {
            ChatMessageEventHandler handler = OnChatMessage;
            if (handler == null)
                return;

            handler(this, message);
        }

        private void RaiseOnChatterJoin(string chatter)
        {
            ChatterJoinLeaveEventHandler handler = OnChatterJoin;
            if (handler == null)
                return;

            handler(this, chatter);
        }

        private void RaiseOnChatterLeave(string chatter)
        {
            ChatterJoinLeaveEventHandler handler = OnChatterLeave;
            if (handler == null)
                return;

            handler(this, chatter);
        }

        private void RaiseOnChannelNotice(ChatMessage message)
        {
            ChatMessageEventHandler handler = OnChannelNotice;
            if (handler == null)
                return;

            handler(this, message);
        }

        private void RaiseOnMessagesDeleted(params string[] messageIds)
        {
            ChatMessagesDeletedEventHandler handler = OnMessagesDeleted;
            if (handler == null)
                return;

            handler(this, messageIds);
        }

        private void RaiseOnViewerListCompleted(bool success, IViewerList viewerList)
        {
            ChatViewerListEventHandler handler = OnViewerListCompleted;
            if (handler == null)
                return;

            handler(this, success, viewerList);
        }
    }
}
