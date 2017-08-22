# Initial Setup #

Import namespace
`using ChatLib;`

Call this ONCE somewhere before creating the first chat service instance
This registers all built-in chat services, making them available for use
`ChatServiceFactory.RegisterBuiltinServices();`

To get a list of all registered services, call `ChatServiceFactory.GetRegisteredServices()`

Create a service instance.  This object must stay alive for the duration of chat usage.
We're using Twitch chat in this example.
`IChatService service = ChatServiceFactory.CreateServiceInstance("irc_twitch");`

Initialize the service instance
`service.Initialize();`

Sets the authentication to be used when joining a chat
`service.SetDefaultAuthentication("twitchUsername", "OAUTH_TOKEN_GOES_HERE");`


# Chat Channels #

Create a chat channel instance.  This is the object you use to send and receive messages to/from the channel.
IChatChannel channel = service.ConnectChannel("channelName");

Hook up events as desired

Raised every time a message is received
`channel.OnChatMessage += channel_OnChatMessage;`

Raised after joining the channel successfully.
`channel.OnJoin += channel_OnJoin;`

Raised after leaving the channel.  This could be manually leaving the channel, or due to errors.
`channel.OnLeave += channel_OnLeave;`

Raised after a chatter joined the channel.  With Twitch's chat, this event could be delayed up to 10 seconds due to caching in Twitch's API
`channel.OnChatterJoin += channel_OnChatterJoin;`

Raised after a chatter has left the channel.  Same delay applies
`channel.OnChatterLeave += channel_OnChatterLeave;`


Request to join the channel
`channel.Join();`

Send a message
`channel.SendMessage("Hi there")`

Call this to request leaving the channel.
Calling Dispose() on a service instance should also leave all connected channels.
`channel.Leave();`

## Chat member list ##

If supported by the service, you can get a list of all members of the chat

Hook up this event before requesting the viewer list
`channel.OnViewerListCompleted += channel_OnViewerListCompleted;`

Sends a request for the viewer list.  Depending on how loaded the API is, it may take several seconds before the complete event is raised.  Under normal circumstances, the request may fail from time-to-time depending on the service.
`channel.GetViewerList();`


# Private Messaging #

If supported by the service, you can send and receive direct private messages.
Some services may not support private messaging

Create private message instance
`IPrivateMessageChannel pm = service.ConnectPrivateMessage();`

Hook up event handlers as required
```
pm.OnJoin += pm_OnJoin;
pm.OnLeave += pm_OnLeave;
pm.OnMessage += pm_OnMessage;
```

Connect
`pm.Join();`

Send messages to users
```
pm.SendMessage("someUser", "Message text here");
pm.SendMessage("otherUser", "Other message text");
```

Clean up when done
pm.Leave();