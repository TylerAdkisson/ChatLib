﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;


namespace ChatLib.Twitch
{
    class IrcServerConnection
    {
        private static Dictionary<EndPoint, IrcServerConnection> _connectionRegistry;
        private static string LogSrc = "IrcServerConnection";

        private Socket _socket;
        private Thread _workerThread;
        private NetworkStream _socketStream;
        private bool _runThread;
        private EndPoint _serverEndpoint;
        private string _nickname;
        private string _authToken;
        private int _maxReconnectInterval;
        private int _minReconnectInterval;
        private int _reconnectInterval;
        private Timer _reconnectTimer;


        public EndPoint Destination { get; private set; }
        public bool HasAuthenticated { get; private set; }
        public bool AutomaticReconnect { get; set; }


        public event LineReceivedEventHandler OnLineReceived;
        public event EventHandler OnConnected;
        public event EventHandler OnDisconnected;


        static IrcServerConnection()
        {
            _connectionRegistry = new Dictionary<EndPoint, IrcServerConnection>();
        }

        public IrcServerConnection(EndPoint serverEndpoint, EndPoint endpoint)
        {
            Destination = endpoint;
            _serverEndpoint = serverEndpoint;
            _minReconnectInterval = 500;
            _maxReconnectInterval = 5000;

            AutomaticReconnect = true;

            _reconnectTimer = new Timer(Reconnect);
        }


        public static IrcServerConnection ConnectServer(params EndPoint[] servers)
        {
            if (servers == null)
                throw new ArgumentNullException("servers");

            // Check if we're already connected to one of the servers
            IrcServerConnection connection = null;

            for (int i = 0; i < servers.Length; i++)
            {
                if (_connectionRegistry.TryGetValue(servers[i], out connection))
                    break; // Found one
            }

            // We cound one, return it
            if (connection != null)
                return connection;

            // TODO: Possibly rework this so if two threads need to connect to two separat hosts,
            //   they don't block each other?
            lock (_connectionRegistry)
            {
                // Check again in case we blocked while getting the lock, and another thread
                //   connected to the same place we're trying to connect to
                for (int i = 0; i < servers.Length; i++)
                {
                    if (_connectionRegistry.TryGetValue(servers[i], out connection))
                        return connection; // Found one
                }

                // Find a suitable endpoint 
                for (int i = 0; i < servers.Length; i++)
                {
                    // Resolve endpoint
                    IPEndPoint endpoint = servers[i] as IPEndPoint;
                    if (endpoint == null)
                        continue; // Non-IP endpoint

                    // Create new connection
                    connection = new IrcServerConnection(endpoint, servers[i]);
                    if (connection.Connect())
                    {
                        // Connection successful, return connection
                        Log.Debug(LogSrc, "Connected to {0}", connection.Destination);
                        _connectionRegistry[servers[i]] = connection;

                        break;
                    }
                }
            }

            return connection;
        }

        public void Release()
        {
            LineReceivedEventHandler handler = OnLineReceived;
            if (handler != null && handler.GetInvocationList().Length > 0)
                return;

            // Clear event listeners
            OnLineReceived = null;
            OnConnected = null;

            _connectionRegistry.Remove(Destination);

            _runThread = false;
            _socket.Close();

            if (_socketStream != null)
                _socketStream.Dispose();

            if (!_workerThread.Join(2000))
                _workerThread.Abort();
        }

        public void StartReceive()
        {
            Thread thread = _workerThread;
            if (thread != null && thread.IsAlive)
                return; // Already started

            _workerThread = new Thread(ReceiveThread);
            _workerThread.IsBackground = true;

            _workerThread.Start();
        }

        public void Authenticate(string nickname, string authToken)
        {
            if (HasAuthenticated || nickname == null)
                return;

            _nickname = nickname;
            _authToken = authToken;

            SendIrcCommand(new IrcMessage(IrcCommands.Password, authToken));
            SendIrcCommand(new IrcMessage(IrcCommands.Nickname, nickname));

            // Enable IRCv3 tags to get emotes, colors, and etc.
            SendIrcCommand(new IrcMessage("CAP", "REQ :twitch.tv/tags"));

            // Enable join/part and user mode messages
            SendIrcCommand(new IrcMessage("CAP", "REQ :twitch.tv/membership"));

            // Enable timeout, host, etc. messages
            SendIrcCommand(new IrcMessage("CAP", "REQ :twitch.tv/commands"));


            HasAuthenticated = true;
        }

        public void SendIrcCommand(IrcMessage message)
        {
            WriteLine(message.ToString());
        }


        private void WriteLine(string text)
        {
            Log.Debug(LogSrc, "--> {0}", text);
            byte[] data = Encoding.UTF8.GetBytes(text + "\r\n");

            try
            {
                _socket.Send(data);
            }
            catch (ObjectDisposedException)
            {
                // Socket closed
            }
            catch (SocketException ex)
            {
                // Network error
            }
        }

        private bool Connect()
        {
            try
            {
                HasAuthenticated = false;

                _socket = new Socket(_serverEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                _socket.Connect(_serverEndpoint);
                StartReceive();

                RaiseOnConnected();

                return true;
            }
            catch (SocketException)
            {
                // Connection error
            }
            return false;
        }

        private void Reconnect(object state)
        {
            if (Connect())
                return;

            _reconnectInterval *= 2;
            if (_reconnectInterval > _maxReconnectInterval)
                _reconnectInterval = _maxReconnectInterval;

            _reconnectTimer.Change(_reconnectInterval, -1);
        }

        private void ReceiveThread(object state)
        {
            _runThread = true;
            try
            {
                _socketStream = new NetworkStream(_socket, false);
                using (StreamReader reader = new StreamReader(_socketStream, Encoding.UTF8))
                {
                    string line;
                    while (_runThread && (line = reader.ReadLine()) != null)
                    {
                        ProcessLine(IrcMessage.Parse(line));
                    }
                }
            }
            catch (IOException ex)
            {
                // Network error
            }
                // TODO: We may be able to remove this catch. Determine of streamreader can throw
                //   SocketExceptions
            catch (SocketException ex)
            {
                // Network error
            }
            catch (ObjectDisposedException)
            {
                // Socket closed
                _runThread = false;
            }

            Log.Debug(LogSrc, "Socket disconnected");

            if (AutomaticReconnect && _runThread)
            {
                // Begin reconnecting
                RaiseOnDisconnected();
                ThreadPool.QueueUserWorkItem(obj => _reconnectTimer.Change(_minReconnectInterval, -1));
            }
        }

        private void ProcessLine(IrcMessage line)
        {
            if (line == null)
                throw new ArgumentNullException("line");


            switch (line.Command)
            {
                case IrcCommands.Ping:
                    // Must reply with pong, or we'll get disconnected
                    SendIrcCommand(new IrcMessage(IrcCommands.Pong, "", line.Text));
                    return;
                case IrcCommands.PrivateMessage:
                case IrcCommands.Part:
                case IrcCommands.Join:
                case IrcCommands.Mode:
                case IrcCommands.Notice:
                case IrcCommands.NameReply:
                case IrcCommands.ClearChat:
                    break;
                default:
                    Log.Debug(LogSrc, "<-- {0}", line);
                    break;
            }

            RaiseOnLineReceived(line);
        }

        private void RaiseOnLineReceived(IrcMessage line)
        {
            LineReceivedEventHandler handler = OnLineReceived;
            if (handler == null)
                return;

            // Only send IRC messages to the correct channel instances
            Delegate[] delegates = handler.GetInvocationList();
            for (int i = 0; i < delegates.Length; i++)
            {
                if (delegates[i].Target.GetType() == typeof(TwitchIrcChannel))
                {
                    // Chat channel messages
                    TwitchIrcChannel channel = delegates[i].Target as TwitchIrcChannel;
                    if (channel == null ||
                        (line.Parameters != null && line.Parameters.StartsWith("#" + channel.ChannelName)) ||
                        line.Parameters == "*")
                    {
                        ((LineReceivedEventHandler)delegates[i])(this, line);
                    }
                }
                else
                {
                    // Private messages
                    ((LineReceivedEventHandler)delegates[i])(this, line);
                }
            }
        }

        private void RaiseOnConnected()
        {
            EventHandler handler = OnConnected;
            if (handler == null)
                return;

            handler(this, EventArgs.Empty);
        }

        private void RaiseOnDisconnected()
        {
            EventHandler handler = OnDisconnected;
            if (handler == null)
                return;

            handler(this, EventArgs.Empty);
        }
    }
}
