using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IrcParserNet.Irc;

namespace TwitchSimpleLib.Chat.Messages;

public class TwitchPrivateMessage : BaseIrcMessage
{
    public readonly string channel;

    public readonly string text;

    public readonly string username;

    /// <summary>
    /// Comma-separated list of chat badges in the form, {badge}/{version}. 
    /// For example, admin/1. 
    /// There are many possible badge values, but here are few:
    /// admin
    /// bits
    /// broadcaster
    /// moderator
    /// subscriber
    /// staff
    /// turbo
    /// Most badges have only 1 version, but some badges like subscriber badges offer different versions of the badge depending on how long the user has subscribed.
    /// </summary>
    public readonly IReadOnlyDictionary<string, string> badges;

    /// <summary>
    /// The color of the user’s name in the chat room. 
    /// This is a hexadecimal RGB color code in the form, #{RGB}.
    /// This tag may be empty if it is never set.
    /// </summary>
    public readonly string? color;

    /// <summary>
    /// The user’s display name, escaped as described in the IRCv3 spec. 
    /// This tag may be empty if it is never set.
    /// https://ircv3.net/specs/core/message-tags-3.2.html
    /// </summary>
    public readonly string? displayName;

    /// <summary>
    /// An ID that uniquely identifies the message.
    /// </summary>
    public readonly string id;

    /// <summary>
    /// Determines whether the user is a moderator.
    /// </summary>
    public readonly bool mod;

    /// <summary>
    /// An ID that identifies the chat room (channel).
    /// </summary>
    public readonly string roomId;

    /// <summary>
    /// Determines whether the user is a subscriber.
    /// </summary>
    public readonly bool subscriber;

    /// <summary>
    /// The UNIX timestamp.
    /// </summary>
    public readonly DateTimeOffset tmiSentTs;

    /// <summary>
    /// Indicates whether the user has site-wide commercial free mode enabled.
    /// </summary>
    public readonly bool turbo;

    /// <summary>
    /// The user’s ID.
    /// </summary>
    public readonly string userId;

    /// <summary>
    /// The type of user. Possible values are:
    /// null or "" — A normal user
    /// admin — A Twitch administrator
    /// global_mod — A global moderator
    /// staff — A Twitch employee
    /// </summary>
    public readonly string? userType;

    /// <summary>
    /// Determines whether the user that sent the chat is a VIP.
    /// </summary>
    public readonly bool vip;

    public readonly string? customRewardId;

    public TwitchPrivateMessage(RawIrcMessage rawIrcMessage)
        : base(rawIrcMessage)
    {
        channel = FirstParameter()[1..];

        text = LastParameter();

        username = rawIrcMessage.prefix!.Split('!')[0];

        color = OptionalTag("color");

        displayName = OptionalTag("display-name");

        id = Tag("id");

        mod = BoolTag("mod");

        roomId = Tag("room-id");

        subscriber = BoolTag("subscriber");

        tmiSentTs = UnixMillisecondsTag("tmi-sent-ts");

        turbo = BoolTag("turbo");

        userId = Tag("user-id");

        customRewardId = OptionalTag("custom-reward-id");

        {
            Dictionary<string, string> badgesDict = new();

            string? badges = OptionalTag("badges");
            if (!string.IsNullOrEmpty(badges))
            {
                ReadOnlySpan<char> remainingBadgesSpan = badges.AsSpan();
                ReadOnlySpan<char> badgeSpan;
                do
                {
                    int delimiterIndex = remainingBadgesSpan.IndexOf(',');
                    if (delimiterIndex == -1)
                    {
                        badgeSpan = remainingBadgesSpan;
                    }
                    else
                    {
                        badgeSpan = remainingBadgesSpan[..delimiterIndex];

                        remainingBadgesSpan = remainingBadgesSpan[(delimiterIndex + 1)..];
                    }

                    int splitIndex = badgeSpan.IndexOf('/');

                    badgesDict.Add(new string(badgeSpan[..splitIndex]),
                                    new string(badgeSpan[(splitIndex + 1)..]));
                }
                while (badgeSpan != remainingBadgesSpan);
            }

            this.badges = badgesDict;
        }

        userType = OptionalTag("user-type");

        vip = HasTag("vip");
    }
}
