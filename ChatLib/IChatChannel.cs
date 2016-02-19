using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace ChatLib
{
    public interface IChatChannel
    {
        /// <summary>
        /// Gets the name of the channel
        /// </summary>
        string ChannelName { get; }

        /// <summary>
        /// Gets the <see cref="IChatService"/> instance for this channel
        /// </summary>
        IChatService ParentService { get; }


        /// <summary>
        /// Raised when the channel is successfully joined
        /// </summary>
        event EventHandler OnJoin;

        /// <summary>
        /// Raised when the channel has been left. This includes error cases causing
        /// the channel to be left.
        /// </summary>
        event ChannelLeaveEventHandler OnLeave;

        /// <summary>
        /// Raised when a chatter joins the channel, including yourself
        /// </summary>
        event ChatterJoinLeaveEventHandler OnChatterJoin;

        /// <summary>
        /// Raised when a chatter leaves the channel, including yourself
        /// </summary>
        event ChatterJoinLeaveEventHandler OnChatterLeave;

        /// <summary>
        /// Raised when a new chat message is received from the channel
        /// </summary>
        event ChatMessageEventHandler OnChatMessage;

        /// <summary>
        /// Raised when a channel-wide message is received from the channel
        /// </summary>
        event ChatMessageEventHandler OnChannelNotice;

        /// <summary>
        /// Raised when a series of chat messages have been deleted. Multiple chat messages
        /// can share the same identifier, so be sure to process them all
        /// </summary>
        event ChatMessagesDeletedEventHandler OnMessagesDeleted;

        /// <summary>
        /// Raised when a pending request for the viewer list has completed or failed
        /// </summary>
        event ChatViewerListEventHandler OnViewerListCompleted;


        /// <summary>
        /// Sets the channel-level authentication for this channel instance
        /// </summary>
        /// <param name="name">The name to authenticate with. Null if not required.</param>
        /// <param name="key">The password or key to authenticate with. Null if not required.</param>
        void SetAuthentication(string name, string key);

        /// <summary>
        /// Begins joining the channel asynchronously
        /// </summary>
        /// <remarks>
        /// Be sure to listen to the OnJoin and OnLeave events to be notified about the
        /// connection status
        /// </remarks>
        void Join();

        /// <summary>
        /// Begins leaving the channel asynchronously
        /// </summary>
        /// <remarks>
        /// Be sure to listen to the OnLeave events to be notified when the channel has been left
        /// </remarks>
        void Leave();

        /// <summary>
        /// Sends an unformatted message into the channel
        /// </summary>
        /// <param name="message">The message text</param>
        void SendMessage(string message);

        /// <summary>
        /// Sends a formatted message into the channel in the form of a series of text runs
        /// </summary>
        /// <param name="formattedMessage">The text runs that the message is composed of</param>
        void SendMessage(IEnumerable<TextRun> formattedMessage);

        /// <summary>
        /// Requests the list of chatters for the channel
        /// </summary>
        void GetViewerList();
    }
}
