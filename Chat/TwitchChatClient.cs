using System.Security.Cryptography;
using IrcParserNet.Irc;
using Microsoft.Extensions.Logging;
using TwitchSimpleLib.Chat.Messages;
using TwitchSimpleLib.Irc;

namespace TwitchSimpleLib.Chat;

public class TwitchChatClient : IrcClient
{
    public static readonly Uri wssUrl = new("wss://irc-ws.chat.twitch.tv:443");
    public static readonly Uri wsUrl = new("ws://irc-ws.chat.twitch.tv:80");

    /// <summary>
    /// Null, если вход анонимный.
    /// </summary>
    public event EventHandler<TwitchGlobalUserStateMessage?>? AuthFinished;

    /// <summary>
    /// Единственный вылет, когда клиент не будет пытаться переподключиться.
    /// </summary>
    public event EventHandler? AuthFailed;

    public event EventHandler<string>? ChannelJoined;
    public event EventHandler<TwitchPrivateMessage>? PrivateMessageReceived;
    public event EventHandler<TwitchRoomStateMessage>? RoomStateReceived;
    public event EventHandler<TwitchNoticeMessage>? NoticeReceived;
    public event EventHandler<TwitchClearChatMessage>? ClearChatReceived;

    public readonly TwitchChatClientOpts opts;

    private readonly List<ChatAutoChannel> autoChannels = new();
    private PingManager? pingManager;

    public TwitchChatClient(bool secure, TwitchChatClientOpts opts, ILoggerFactory? loggerFactory,
        CancellationToken cancellationToken = default)
        : this(secure ? wssUrl : wsUrl, opts, loggerFactory, cancellationToken)
    {
    }

    public TwitchChatClient(Uri uri, TwitchChatClientOpts opts, ILoggerFactory? loggerFactory,
        CancellationToken cancellationToken = default)
        : base(uri, opts, loggerFactory, cancellationToken)
    {
        this.opts = opts;
    }

    /// <summary>
    /// Использовать перед запуском бота.
    /// </summary>
    /// <param name="channel"></param>
    /// <returns></returns>
    public ChatAutoChannel AddAutoJoinChannel(string channel)
    {
        ChatAutoChannel? autoChannel = GetChannel(channel);

        if (autoChannel != null)
            return autoChannel;

        autoChannel = new(channel, this);

        lock (autoChannels)
        {
            autoChannels.Add(autoChannel);
        }

        return autoChannel;
    }

    /// <summary>
    /// Можно использовать после старта бота.
    /// </summary>
    /// <param name="channel"></param>
    /// <returns></returns>
    public async Task<ChatAutoChannel> AddAutoChannelAsync(string channel)
    {
        ChatAutoChannel? autoChannel = GetChannel(channel);

        if (autoChannel != null)
            return autoChannel;

        autoChannel = new(channel, this);

        lock (autoChannels)
        {
            autoChannels.Add(autoChannel);
        }

        if (IsConnected)
        {
            await JoinAsync(channel);
        }

        return autoChannel;
    }

    /// <summary>
    /// Можно использовать после старта бота.
    /// Асинхронная часть будет запущена отдельным таском.
    /// </summary>
    /// <param name="channel"></param>
    /// <returns></returns>
    public ChatAutoChannel AddAutoChannel(string channel)
    {
        ChatAutoChannel? autoChannel = GetChannel(channel);

        if (autoChannel != null)
            return autoChannel;

        autoChannel = new(channel, this);

        lock (autoChannels)
        {
            autoChannels.Add(autoChannel);
        }

        if (IsConnected)
        {
            Task.Run(async () => { await JoinAsync(channel); });
        }

        return autoChannel;
    }

    public async Task<bool> RemoveAutoChannelAsync(ChatAutoChannel autoChannel)
    {
        bool removed;
        lock (autoChannels)
        {
            removed = autoChannels.Remove(autoChannel);
        }

        if (removed && IsConnected)
        {
            await LeaveAsync(autoChannel.channel);
        }

        return removed;
    }

    /// <summary>
    /// Асинхронная часть будет запущена отдельным таском.
    /// </summary>
    /// <param name="autoChannel"></param>
    /// <returns></returns>
    public bool RemoveAutoChannel(ChatAutoChannel autoChannel)
    {
        bool removed;
        lock (autoChannels)
        {
            removed = autoChannels.Remove(autoChannel);
        }

        if (removed && IsConnected)
        {
            Task.Run(async () => { await LeaveAsync(autoChannel.channel); });
        }

        return removed;
    }

    public Task<bool> RemoveAutoChannelAsync(string channel)
    {
        ChatAutoChannel? autoChannel = GetChannel(channel);
        if (autoChannel == null)
            return Task.FromResult(false);

        return RemoveAutoChannelAsync(autoChannel);
    }

    /// <summary>
    /// Асинхронная часть будет запущена отдельным таском.
    /// </summary>
    /// <param name="channel"></param>
    /// <returns></returns>
    public bool RemoveAutoChannel(string channel)
    {
        ChatAutoChannel? autoChannel = GetChannel(channel);
        if (autoChannel == null)
            return false;

        return RemoveAutoChannel(autoChannel);
    }

    public List<ChatAutoChannel> CopyAutoChannels()
    {
        lock (autoChannels)
        {
            return autoChannels.ToList();
        }
    }

    public Task SendMessageAsync(string channel, string text)
        => SendRawAsync($"PRIVMSG #{channel.ToLower()} :{text}");

    public Task SendMessageAsync(string channel, string text, string parentMessageId)
        => SendRawAsync($"@reply-parent-msg-id={parentMessageId} PRIVMSG #{channel.ToLower()} :{text}");

    public Task JoinAsync(string channel)
        => JoinAsync(_connection, channel);

    public static Task JoinAsync(WsConnection? connection, string channel)
        => SendRawAsync(connection, $"JOIN #{channel.ToLower()}");

    public Task LeaveAsync(string channel)
        => SendRawAsync($"PART #{channel.ToLower()}");

    protected override async Task ConnectedAsync(WsConnection connection)
    {
        await base.ConnectedAsync(connection);

        await connection.SendAsync("CAP REQ :twitch.tv/tags twitch.tv/commands");
        await connection.SendAsync("PASS " + opts.OauthToken);
        await connection.SendAsync("NICK " + opts.Username /*.ToLower()*/);
        // USER urantij 8 * :urantij

        pingManager = new(true, opts.PingDelay, opts.PingTimeout, state: connection,
            cancellationToken: _cancellationToken);
        pingManager.Pinging += Pinging;
        pingManager.Timeouted += Timeouted;
        pingManager.Start();
    }

    protected override void IrcMessageReceived(WsConnection connection, RawIrcMessage message)
    {
        base.IrcMessageReceived(connection, message);

        switch (message.command)
        {
            // The text portion of the 353 message lists the users in the channel at the time you joined.
            // If the channel already has users joined to it, the reply may contain two 353 messages. The first shows the existing users in the chat room and the second shows the bot that joined.
            case "353":
                return;

            // the bot successfully joins the channel
            case "366":
                string channel = message.parameters![1][1..];
                ProcessChannelJoined(channel);
                return;

            // Это просто конец сообщения дня, но оно всегда приходит в конце аутентификации.
            case "376":
                Process376(connection);
                return;

            case "PING":
            {
                string text = message.parameters![0];
                ProcessPing(connection, text);
                return;
            }

            case "PONG":
            {
                string text = message.parameters!.Last();
                ProcessPong(connection, text);
                return;
            }

            // Sent when the Twitch IRC server needs to terminate the connection for maintenance reasons.
            case "RECONNECT":
                return;

            case "GLOBALUSERSTATE":
                TwitchGlobalUserStateMessage globalUserStateMessage = new(message);
                ProcessGlobalUserState(connection, globalUserStateMessage);
                return;

            case "USERSTATE":
                return;

            case "PRIVMSG":
                TwitchPrivateMessage privateMessage = new(message);
                ProcessPrivateMessage(privateMessage);
                return;

            case "ROOMSTATE":
                TwitchRoomStateMessage roomStateMessage = new(message);
                ProcessRoomState(roomStateMessage);
                return;

            case "NOTICE":
                TwitchNoticeMessage noticeMessage = new(message);
                ProcessNotice(noticeMessage);
                return;

            case "CLEARCHAT":
                TwitchClearChatMessage clearChatMessage = new(message);
                ProcessClearChat(clearChatMessage);
                return;
        }
    }

    private async void Process376(WsConnection connection)
    {
        // Если бот анонимный, ему не приходит GlobalUserState
        // И приходится ориентироваться на что-то другое.
        if (!opts.Anonymous)
            return;

        await ProcessAuthAsync(connection, null);
    }

    private async void ProcessGlobalUserState(WsConnection connection, TwitchGlobalUserStateMessage message)
    {
        await ProcessAuthAsync(connection, message);
    }

    private async void ProcessPing(WsConnection connection, string text)
    {
        await SendRawAsync(connection, $"PONG :{text}");
    }

    private void ProcessPong(WsConnection connection, string text)
    {
        if (connection == pingManager?.State)
            pingManager.PongReceived(text);
    }

    private void ProcessChannelJoined(string channel)
    {
        ChannelJoined?.Invoke(this, channel);

        ChatAutoChannel? autoChannel = GetChannel(channel);
        autoChannel?.OnChannelJoined();
    }

    private void ProcessPrivateMessage(TwitchPrivateMessage message)
    {
        PrivateMessageReceived?.Invoke(this, message);

        ChatAutoChannel? autoChannel = GetChannel(message.channel);
        autoChannel?.OnPrivateMessageReceived(message);
    }

    private void ProcessRoomState(TwitchRoomStateMessage message)
    {
        RoomStateReceived?.Invoke(this, message);

        ChatAutoChannel? autoChannel = GetChannel(message.channel);
        autoChannel?.OnRoomStateReceived(message);
    }

    private void ProcessNotice(TwitchNoticeMessage message)
    {
        NoticeReceived?.Invoke(this, message);

        if (message.notice == "Login authentication failed")
        {
            Close();

            AuthFailed?.Invoke(this, EventArgs.Empty);
        }
        else if (message.channel != null)
        {
            ChatAutoChannel? autoChannel = GetChannel(message.channel);
            autoChannel?.OnNoticeReceivedReceived(message);
        }
    }

    private void ProcessClearChat(TwitchClearChatMessage message)
    {
        ClearChatReceived?.Invoke(this, message);

        ChatAutoChannel? autoChannel = GetChannel(message.channel);
        autoChannel?.OnClearChatReceived(message);
    }

    private async Task Pinging(PingManager pingManager, string text)
    {
        if (pingManager != this.pingManager || pingManager.State != _connection || !IsConnected)
            return;

        await SendRawAsync($"PING :{text}");
    }

    private void Timeouted(PingManager pingManager)
    {
        if (pingManager != this.pingManager || pingManager.State != _connection || !IsConnected)
            return;

        _connection!.Dispose(new Exception("Ping Timeout"));
    }

    private async Task ProcessAuthAsync(WsConnection connection, TwitchGlobalUserStateMessage? message)
    {
        AuthFinished?.Invoke(this, message);

        ChatAutoChannel[] channels;
        lock (autoChannels)
            channels = autoChannels.ToArray();

        foreach (var autoChannel in channels)
        {
            await JoinAsync(connection, autoChannel.channel);
        }
    }

    protected override void ConnectionDisposing(object? sender, Exception? e)
    {
        lock (autoChannels)
        {
            foreach (var autoChannel in autoChannels)
            {
                autoChannel.IsJoined = false;
            }
        }

        if (pingManager != null)
        {
            pingManager.Pinging -= Pinging;
            pingManager.Timeouted -= Timeouted;
            pingManager.Stop();
        }

        base.ConnectionDisposing(sender, e);
    }

    private ChatAutoChannel? GetChannel(string channelName)
    {
        lock (autoChannels)
            return autoChannels.FirstOrDefault(c => c.channel.Equals(channelName, StringComparison.OrdinalIgnoreCase));
    }

    public static string GenerateAnonymName()
    {
        return $"justinfan{RandomNumberGenerator.GetInt32(0, 100000)}";
    }
}