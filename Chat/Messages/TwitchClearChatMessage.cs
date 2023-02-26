using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IrcParserNet.Irc;

namespace TwitchSimpleLib.Chat.Messages;

public class TwitchClearChatMessage : BaseIrcMessage
{
    public readonly string channel;

    public readonly string? username;

    /// <summary>
    /// Optional. 
    /// The message includes this tag if the user was put in a timeout. 
    /// The tag contains the duration of the timeout, in seconds.
    /// </summary>
    public readonly int? banDuration;

    /// <summary>
    /// The ID of the channel where the messages were removed from.
    /// </summary>
    public readonly string roomId;

    /// <summary>
    /// Optional. 
    /// The ID of the user that was banned or put in a timeout. 
    /// The user was banned if the message doesn’t include the ban-duration tag.
    /// </summary>
    public readonly string? targetUserId;

    /// <summary>
    /// The UNIX timestamp.
    /// </summary>
    public readonly DateTimeOffset tmiSentTs;

    public TwitchClearChatMessage(RawIrcMessage rawIrcMessage)
        : base(rawIrcMessage)
    {
        channel = FirstParameter()[1..];

        if (rawIrcMessage.parameters!.Length >= 2)
        {
            username = Parameter(1);
        }

        banDuration = OptionalIntTag("ban-duration");

        roomId = Tag("room-id");

        targetUserId = OptionalTag("target-user-id");

        tmiSentTs = UnixMillisecondsTag("tmi-sent-ts");
    }
}
