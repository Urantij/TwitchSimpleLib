using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IrcParserNet.Irc;

namespace TwitchSimpleLib.Chat.Messages;

/// <summary>
/// The Twitch IRC server sends this message after the bot authenticates with the server.
/// </summary>
public class TwitchGlobalUserStateMessage : BaseIrcMessage
{
    /// <summary>
    /// The color of the user’s name in the chat room. This is a hexadecimal RGB color code in the form, #<RGB>. This tag may be empty if it is never set.
    /// </summary>
    public readonly string? color;
    /// <summary>
    /// The user’s display name.
    /// </summary>
    public readonly string? displayName;
    /// <summary>
    /// Indicates whether the user has site-wide commercial free mode enabled
    /// </summary>
    public readonly bool turbo;
    /// <summary>
    /// The user’s ID.
    /// </summary>
    public readonly string userId;

    public TwitchGlobalUserStateMessage(RawIrcMessage rawIrcMessage)
        : base(rawIrcMessage)
    {
        color = OptionalTag("color");
        displayName = OptionalTag("display-name");
        turbo = BoolTag("turbo");
        userId = Tag("user-id");
    }
}
