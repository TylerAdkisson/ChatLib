using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace ChatLib
{
    public interface IChatChannel
    {
        event EventHandler OnJoin;

        event ChannelLeaveEventHandler OnLeave;

        event ChatterJoinLeaveEventHandler OnChatterJoin;

        event ChatterJoinLeaveEventHandler OnChatterLeave;

        event ChatMessageEventHandler OnChatMessage;


        IChatService GetParentService();

        void SetAuthentication(string name, string key);

        void Join();

        void Leave();

        void SendMessage(string message);

        void SendMessage(IEnumerable<TextRun> formattedMessage);
    }
}
