using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace ChatLib.Twitch
{
    static class IrcCommands
    {
        public const string Ping = "PING";
        public const string Pong = "PONG";
        public const string PrivateMessage = "PRIVMSG";
        public const string Notice = "NOTICE";
        public const string Join = "JOIN";
        public const string Part = "PART";
        public const string Password = "PASS";
        public const string Nickname = "NICK";
        public const string Mode = "MODE";

        // Numeric
        public const string Welcome = "001";
        public const string Yourhost = "002";
        public const string ServerCreated = "003";
        public const string ServerInfo = "004";
        public const string NameReply = "353";
        public const string NameListEnd = "366";
        public const string MotdStart = "375";
        public const string MotdBody = "372";
        public const string MotdEnd = "376";

        // Twitch-specific
        public const string HostTarget = "HOSTTARGET";
        public const string ClearChat = "CLEARCHAT";
        public const string UserState = "USERSTATE";
        public const string RoomState = "ROOMSTATE";
        public const string GlobalUserState = "GLOBALUSERSTATE";
        public const string Reconnect = "RECONNECT";
    }
}
