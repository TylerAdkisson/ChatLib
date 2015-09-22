using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatLib
{
    public class IrcMessage
    {
        public string Tags { get; private set; }
        public string Source { get; private set; }
        public string Command { get; private set; }
        public string Parameters { get; private set; }
        public string Text { get; private set; }


        public IrcMessage()
        {
        }

        public IrcMessage(string cmd, string param)
            : this(null, null, cmd, param, null)
        {
        }

        public IrcMessage(string cmd, string param, string text)
            : this(null, null, cmd, param, text)
        {
        }

        public IrcMessage(string tags, string src, string cmd, string param, string text)
        {
            Tags = tags;
            Source = src;
            Command = cmd;
            Parameters = param;
            Text = text;
        }

        public static IrcMessage Parse(string line)
        {
            if (line == null)
                throw new ArgumentNullException("line");

            // Split at most 4 parts to include possibility of IRC v3 tags and a prefix
            string[] segments = line.Split(new char[] { ' ' }, 4);

            IrcMessage msg = new IrcMessage();

            for (int i = 0; i < segments.Length; i++)
            {
                switch (segments[i][0])
                {
                    case ':': // IRC v2 source prefix
                        msg.Source = segments[i].Remove(0, 1);
                        continue;
                    case '@': // IRC v3 tags
                        msg.Tags = segments[i].Remove(0, 1);
                        continue;
                    default: // Found rest of message
                        msg.Command = segments[i];
                        if (i < segments.Length - 1)
                        {
                            msg.Parameters = string.Join(" ", segments, i + 1, segments.Length - i - 1);

                            int colonIndex = msg.Parameters.IndexOf(':');
                            if (colonIndex >= 0)
                            {
                                msg.Text = msg.Parameters.Substring(colonIndex + 1);
                                msg.Parameters = msg.Parameters.Remove(colonIndex).TrimEnd(' ');
                            }
                        }
                        break;
                }
                break;
            }

            return msg;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(Tags))
            {
                sb.Append("@");
                sb.Append(Tags);
            }

            if (!string.IsNullOrWhiteSpace(Source))
            {
                if (sb.Length > 0)
                    sb.Append(" ");
                sb.Append(":");
                sb.Append(Source);
            }

            if (sb.Length > 0)
                sb.Append(" ");
            sb.Append(Command);

            if (!string.IsNullOrWhiteSpace(Parameters))
            {
                sb.Append(" ");
                sb.Append(Parameters);
            }

            if (!string.IsNullOrWhiteSpace(Text))
            {
                sb.Append(" :");
                sb.Append(Text);
            }

            return sb.ToString();
        }
    }
}
