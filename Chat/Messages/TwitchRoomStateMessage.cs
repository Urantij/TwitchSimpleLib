using IrcParserNet.Irc;

namespace TwitchSimpleLib.Chat.Messages;

/// <summary>
/// For JOIN messages, the message contains all chat room setting tags, but for actions that change a single chat room setting, the message includes only that chat room setting tag. For example, if the moderator turned on unique chat, the message includes only the r9k tag.
/// </summary>
public class TwitchRoomStateMessage : BaseIrcMessage
{
    public readonly string channel;

    public readonly bool? emoteOnly;

    public readonly int? followersOnly;

    public readonly bool? r9k;

    public readonly string? roomId;

    public readonly int? slow;

    public readonly bool? subsOnly;

    public TwitchRoomStateMessage(RawIrcMessage rawIrcMessage)
        : base(rawIrcMessage)
    {
        channel = FirstParameter()[1..];

        emoteOnly = OptionalBoolTag("emote-only");

        followersOnly = OptionalIntTag("followers-only");

        r9k = OptionalBoolTag("r9k");

        roomId = OptionalTag("room-id");

        slow = OptionalIntTag("slow");

        subsOnly = OptionalBoolTag("subs-only");
    }
}