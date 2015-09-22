using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace ChatLib
{
    public interface IChatService : IDisposable
    {
        void Initialize();

        void SetDefaultServer(string hostnameOrIPAddress, int port);

        void SetDefaultAuthentication(string name, string key);

        IChatChannel ConnectChannel(string channelName);

        object Upgrade();
    }
}
