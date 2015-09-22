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
        public const string NameReply = "353";
    }
}
