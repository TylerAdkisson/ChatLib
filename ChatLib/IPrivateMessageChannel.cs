using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChatLib
{
    public interface IPrivateMessageChannel
    {
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
        /// Raised when a new private message is received
        /// </summary>
        event ChatMessageEventHandler OnMessage;


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
        /// Sends an unformatted message to the specified recipient
        /// </summary>
        /// <param name="username">The user to send the message to</param>
        /// <param name="message">The message text</param>
        void SendMessage(string username, string message);
    }
}
