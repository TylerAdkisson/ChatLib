using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;


namespace ChatLib
{
    public interface IChatService : IDisposable
    {
        /// <summary>
        /// Initializes the service instance for use
        /// </summary>
        void Initialize();

        /// <summary>
        /// Sets the default server to use for channel connections, if required
        /// </summary>
        /// <param name="hostnameOrIPAddress">The DNS hostname or IP address of the server</param>
        /// <param name="port">The port number to connect to</param>
        void SetDefaultServer(string hostnameOrIPAddress, int port);

        /// <summary>
        /// Sets the default authentication credentials to use for channel connections unless
        /// overridden by a channel
        /// </summary>
        /// <param name="name">The name to authenticate with. Null if not required.</param>
        /// <param name="key">The password or key to authenticate with. Null if not required.</param>
        void SetDefaultAuthentication(string name, string key);

        /// <summary>
        /// Creates a new channel instance set to join the specified channel
        /// </summary>
        /// <param name="channelName">The name of the channel to connect to</param>
        /// <returns>A new <see cref="IChatChannel"/> instance to join the specified channel</returns>
        IChatChannel ConnectChannel(string channelName);

        /// <summary>
        /// Reserved for future use
        /// </summary>
        /// <returns>To be determined</returns>
        object Upgrade();

        /// <summary>
        /// Gets a collection of chat member status groups
        /// </summary>
        /// <returns>A collection of chat member status groups</returns>
        ReadOnlyCollection<ChatterStatusGroup> GetStatusGroups();
    }
}
