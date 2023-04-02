using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TwitchSimpleLib.Chat.Messages;

namespace TwitchSimpleLib.Chat;

/// <summary>
/// Хранит ссылку на клиент бота, аккуратно.
/// </summary>
public class ChatAutoChannel
{
    public readonly string channel;

    public readonly TwitchChatClient client;

    /// <summary>
    /// Присоединён ли бот к каналу.
    /// </summary>
    public bool IsJoined { get; internal set; }

    public bool? EmoteOnly { get; private set; }
    /// <summary>
    /// В минутах.
    /// </summary>
    public int? FollowersOnly { get; private set; }
    public bool? R9k { get; private set; }
    /// <summary>
    /// В секундах.
    /// </summary>
    public int? Slow { get; private set; }
    public bool? SubsOnly { get; private set; }

    public event EventHandler? ChannelJoined;

    public event EventHandler<TwitchPrivateMessage>? PrivateMessageReceived;
    public event EventHandler<TwitchRoomStateMessage>? RoomStateReceived;
    public event EventHandler<TwitchNoticeMessage>? NoticeReceived;
    public event EventHandler<TwitchClearChatMessage>? ClearChatReceived;

    public ChatAutoChannel(string channel, TwitchChatClient client)
    {
        this.channel = channel;
        this.client = client;
    }

    public Task SendMessageAsync(string text)
    {
        return client.SendMessageAsync(channel, text);
    }

    public Task SendMessageAsync(string text, string parentMessageId)
    {
        return client.SendMessageAsync(channel, text, parentMessageId);
    }

    internal void OnChannelJoined()
    {
        IsJoined = true;

        ChannelJoined?.Invoke(this, EventArgs.Empty);
    }

    internal void OnPrivateMessageReceived(TwitchPrivateMessage msg)
    {
        PrivateMessageReceived?.Invoke(this, msg);
    }

    internal void OnRoomStateReceived(TwitchRoomStateMessage msg)
    {
        if (msg.emoteOnly != null)
            EmoteOnly = msg.emoteOnly;
        if (msg.followersOnly != null)
            FollowersOnly = msg.followersOnly;
        if (msg.r9k != null)
            R9k = msg.r9k;
        if (msg.slow != null)
            Slow = msg.slow;
        if (msg.subsOnly != null)
            SubsOnly = msg.subsOnly;

        RoomStateReceived?.Invoke(this, msg);
    }

    internal void OnNoticeReceivedReceived(TwitchNoticeMessage msg)
    {
        NoticeReceived?.Invoke(this, msg);
    }

    internal void OnClearChatReceived(TwitchClearChatMessage msg)
    {
        ClearChatReceived?.Invoke(this, msg);
    }
}
