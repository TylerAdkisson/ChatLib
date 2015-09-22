using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace ChatLib
{
    public enum LeaveReason
    {
        ChannelLeave,
        Error
    };

    public delegate void ChatterJoinLeaveEventHandler(object sender, string chatterName);
    public delegate void ChatMessageEventHandler(object sender, ChatMessage message);

    public delegate void ChannelLeaveEventHandler(object sender, LeaveReason reason);
}
