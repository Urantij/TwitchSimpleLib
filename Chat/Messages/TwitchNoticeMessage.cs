using IrcParserNet.Irc;

namespace TwitchSimpleLib.Chat.Messages;

public class TwitchNoticeMessage : BaseIrcMessage
{
    public readonly string channel;

    public readonly string notice;

    /// <summary>
    /// Может быть нулл, если пришло уведомление о плохой аутентификации.
    /// An ID that you can use to programmatically determine the action’s outcome. 
    /// For a list of possible IDs, see NOTICE Message IDs. 
    /// https://dev.twitch.tv/docs/irc/msg-id
    /// </summary>
    public readonly string? msgId;

    /// <summary>
    /// The ID of the user that the action targeted.
    /// </summary>
    public readonly string? targetUserId;

    public TwitchNoticeMessage(RawIrcMessage rawIrcMessage)
        : base(rawIrcMessage)
    {
        channel = FirstParameter()[1..];

        notice = Parameter(1);

        if (rawIrcMessage.tags != null)
        {
            msgId = OptionalTag("msg-id");

            targetUserId = OptionalTag("target-user-id");
        }
    }
}