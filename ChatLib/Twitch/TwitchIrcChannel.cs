using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatLib.Twitch
{
    public class TwitchIrcChannel : IChatChannel
    {
        private static uint Counter = 0;
        private uint Number = 0;
        private TwitchIrcService _service;
        private string _channelName;
        private ServerConnection _connection;


        public string ChannelName { get { return _channelName; } }


        public event EventHandler OnJoin;

        public event ChannelLeaveEventHandler OnLeave;

        public event ChatterJoinLeaveEventHandler OnChatterJoin;

        public event ChatterJoinLeaveEventHandler OnChatterLeave;

        public event ChatMessageEventHandler OnChatMessage;


        public TwitchIrcChannel(TwitchIrcService service, string channelName)
        {
            _service = service;
            _channelName = channelName;

            Number = Counter++;
        }


        public IChatService GetParentService()
        {
            return _service;
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
            // TODO: Leave channel, then tell service that we are done with the server connection.
            //   If no other channels are using the connection we were, the service can clean up
            //   the connection.

            ServerConnection connection = _connection;
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


        private void JoinWorkerThread(object state)
        {
            // Lookup which servers to connect to for this channel
            DnsEndPoint[] servers = _service.GetChatServers(_channelName);
            ServerConnection connection = ServerConnection.ConnectServer(servers);
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
            _connection.SendIrcCommand(new IrcMessage(IrcCommands.Join, "#" + _channelName));

            // TODO: Wait and call OnJoin after receiving the join message?
            //RaiseOnJoin();
        }

        private void ServerConnected(object sender, EventArgs e)
        {
            JoinChannel();
        }

        private Dictionary<string, ConsoleColor> _nameColors = new Dictionary<string, ConsoleColor>();
        private void ProcessIrcMessage(object sender, IrcMessage line)
        {
            switch (line.Command)
            {
                case IrcCommands.PrivateMessage:
                    //if (!line.Parameters.StartsWith("#" + _channelName))
                    //    break;

                    //
                    //
                    //

                    ChatMessage message = new ChatMessage();
                    message.Timestamp = DateTime.Now;
                    message.Author = line.Source.Remove(line.Source.IndexOf('!'));

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

                    if (line.Tags != null)
                    {
                        string[] tags = line.Tags.Split(';');

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
                                    message.Author.Color = value;
                                    break;
                                case "display-name":
                                    if (!string.IsNullOrEmpty(value))
                                        message.Author.Text = message.Author.Content = value;
                                    break;
                                case "emotes":
                                    if (string.IsNullOrEmpty(value))
                                        break; // No emotes

                                    string[] emotes = value.Split('/');

                                    LinkedList<TextRun> runs = new LinkedList<TextRun>();
                                    runs.AddFirst(messageText);

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
                                            TextRun leftRun = new TextRun(theRun.Value.Text.Remove(emoteStart - position));
                                            TextRun rightRun = new TextRun(theRun.Value.Text.Substring(emoteEnd + 1 - position));
                                            TextRun emoteRun = new TextRun(theRun.Value.Text.Substring(emoteStart - position, emoteEnd - emoteStart + 1));

                                            emoteRun.Kind = TextRun.RunKind.Image;
                                            emoteRun.Content = TwitchIrcService.EmoteUri.Replace(":emote_id", emoteId.ToString());

                                            if (leftRun.Text.Length > 0)
                                                runs.AddBefore(theRun, leftRun);
                                            runs.AddBefore(theRun, emoteRun);
                                            if (rightRun.Text.Length > 0)
                                                runs.AddBefore(theRun, rightRun);
                                            runs.Remove(theRun);
                                        }
                                    }

                                    message.ClearRuns();
                                    message.AppendRuns(runs);

                                    break;
                                default:
                                    break;
                            }
                        }
                    }

                    //
                    //
                    //

                    lock (Console.OutputEncoding)
                    {
                        ConsoleColor nameColor = ConsoleColor.Gray;

                        if (!string.IsNullOrEmpty(message.Author.Color))
                        {
                            nameColor = ConsoleColorConverter.HexToColor(message.Author.Color.TrimStart('#'));
                        }

                        if (nameColor == ConsoleColor.Gray)
                        {
                            if (!_nameColors.TryGetValue(line.Source, out nameColor))
                                nameColor = _nameColors[line.Source] = ConsoleColorConverter.GetColor();
                        }

                        if (true)
                        {

                            Console.Write("[{0}] #{1} ",
                                message.Timestamp.ToLongTimeString(),
                                _channelName);

                            Console.ForegroundColor = nameColor;
                            Console.Write("{0}", message.Author.Text);

                            //Console.Write("[{0,20}] ", _channelName);
                            //Console.ForegroundColor = nameColor;
                            //Console.Write("{0,20}", message.Author.Text);
                            //Console.ResetColor();

                            if (message.MessageKind == ChatMessage.Kind.Action)
                            {
                                Console.ForegroundColor = nameColor;
                                Console.Write(" ");
                            }
                            else
                            {
                                Console.ResetColor();
                                Console.Write(": ");
                            }

                            foreach (var segment in message.TextRuns)
                            {
                                if (segment.Kind == TextRun.RunKind.Image)
                                    Console.ForegroundColor = ConsoleColor.Yellow;

                                Console.Write(segment.Text);
                                if (message.MessageKind == ChatMessage.Kind.Action)
                                    Console.ForegroundColor = nameColor;
                                else
                                    Console.ResetColor();
                            }
                            //if (message.MessageKind == ChatMessage.Kind.Action)
                            //    Console.Write("*");

                            Console.ResetColor();
                            Console.WriteLine();
                        }

                        //Console.WriteLine("]: {0}", message.ToString());

                        RaiseOnChatMessage(message);
                    }
                    break;
                case IrcCommands.Join:
                    //if (!line.Parameters.StartsWith("#" + _channelName))
                    //    break;

                    if (line.Source.StartsWith(_service.Nickname))
                        RaiseOnJoin();
                    //else
                    //{
                    //    Console.WriteLine("[{0}] #{1} {2} has joined.",
                    //        DateTime.Now.ToLongTimeString(),
                    //        _channelName, line.Source.Remove(line.Source.IndexOf('!')));
                    //}

                    break;
                case IrcCommands.Part:
                    //if (!line.Parameters.StartsWith("#" + _channelName))
                    //    break;

                    if (line.Source.StartsWith(_service.Nickname))
                        RaiseOnLeave(LeaveReason.ChannelLeave);
                    //else
                    //{
                    //    Console.WriteLine("[{0}] #{1} {2} has left.",
                    //        DateTime.Now.ToLongTimeString(),
                    //        _channelName, line.Source.Remove(line.Source.IndexOf('!')));
                    //}

                    break;
                default:
                    break;
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
    }
}
