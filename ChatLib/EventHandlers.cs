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

    /// <summary>
    /// Represents the method that will handle chatter join and leave events
    /// </summary>
    /// <param name="sender">The object that raised the event</param>
    /// <param name="chatterName">The name of the chatter who joined or left</param>
    public delegate void ChatterJoinLeaveEventHandler(object sender, string chatterName);

    /// <summary>
    /// Represents the method that will handle newly-received chat messages
    /// </summary>
    /// <param name="sender">The object that raised the event</param>
    /// <param name="message">The message that was received</param>
    public delegate void ChatMessageEventHandler(object sender, ChatMessage message);

    /// <summary>
    /// Represents the method that will handle channel leave events
    /// </summary>
    /// <param name="sender">The object that raised the event</param>
    /// <param name="reason">The reason for leaving the channel</param>
    public delegate void ChannelLeaveEventHandler(object sender, LeaveReason reason);

    /// <summary>
    /// Represents the method that will handle message removal events
    /// </summary>
    /// <param name="sender">The object that raised the event</param>
    /// <param name="messageIds">A series of message identifiers that specify the message(s) to remove</param>
    public delegate void ChatMessagesDeletedEventHandler(object sender, IEnumerable<string> messageIds);

    /// <summary>
    /// Represents the method that will handle poll results
    /// </summary>
    /// <param name="sender">The object that raised the event</param>
    /// <param name="results">The results of the poll</param>
    public delegate void PollResultsEventHandler(object sender, PollResults results);

    /// <summary>
    /// Represents the method that will handle the result of viewer list requests
    /// </summary>
    /// <param name="sender">The object that raised the event</param>
    /// <param name="successResult">True if the request succeeded, otherwise false</param>
    /// <param name="viewerList">An instance of _TYPE_NAME_HERE_, holding the names of all users in chat</param>
    public delegate void ChatViewerListEventHandler(object sender, bool successResult, IViewerList viewerList);
}
