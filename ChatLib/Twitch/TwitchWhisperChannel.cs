using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace ChatLib.Twitch
{
    class TwitchWhisperChannel : IPrivateMessageChannel
    {
        private static string Src = "TwitchWhisperChannel";
        private TwitchIrcService _service;
        private IrcServerConnection _connection;


        public IChatService ParentService { get { return _service; } }


        public event EventHandler OnJoin;

        public event ChannelLeaveEventHandler OnLeave;

        public event ChatMessageEventHandler OnMessage;


        public TwitchWhisperChannel(TwitchIrcService parent)
        {
            _service = parent;
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

            connection.OnLineReceived -= ProcessIrcMessage;
            connection.OnConnected -= ServerConnected;
            connection.OnDisconnected -= ServerDisconnected;

            connection.Release();
            _connection = null;

            RaiseOnLeave(LeaveReason.ChannelLeave);
        }

        public void SendMessage(string username, string message)
        {
            IrcMessage msg = new IrcMessage(
                IrcCommands.PrivateMessage,
                "#jtv",
                string.Concat("/w ", username, " ", message));

            _connection.SendIrcCommand(msg);
        }


        private void JoinWorkerThread(object state)
        {
            // Lookup which servers to connect to for this channel
            IPEndPoint[] servers = _service.GetChatServers(string.Empty, true);
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
            connection.OnDisconnected += ServerDisconnected;
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

            RaiseOnJoin();
        }

        private void ServerConnected(object sender, EventArgs e)
        {
            JoinChannel();
        }

        private void ServerDisconnected(object sender, EventArgs e)
        {
            RaiseOnLeave(LeaveReason.Error);
        }

        private void ProcessIrcMessage(object sender, IrcMessage line)
        {
            switch (line.Command)
            {
                case IrcCommands.Whisper:
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

                    RaiseOnMessage(message);

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
                        if (value == "1")
                            groups[2] = _service.GetStatusGroups()[2].GroupItems[0];
                        break;
                    default:
                        break;
                }
            }

            message.Author.StatusGroupMembership = groups.AsReadOnly();
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

        private void RaiseOnMessage(ChatMessage message)
        {
            ChatMessageEventHandler handler = OnMessage;
            if (handler == null)
                return;

            handler(this, message);
        }
    }
}
