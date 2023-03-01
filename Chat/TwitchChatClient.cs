using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using IrcParserNet.Irc;
using Microsoft.Extensions.Logging;
using TwitchSimpleLib.Chat.Messages;
using TwitchSimpleLib.Irc;

namespace TwitchSimpleLib.Chat;

public class TwitchChatClient : IrcClient
{
    public static readonly Uri wssUrl = new("wss://irc-ws.chat.twitch.tv:443");
    public static readonly Uri wsUrl = new("ws://irc-ws.chat.twitch.tv:80");

    public event EventHandler<Exception?>? ConnectionClosed;
    public event EventHandler<TwitchGlobalUserStateMessage>? AuthFinished;
    public event EventHandler<string>? ChannelJoined;
    public event EventHandler<TwitchPrivateMessage>? PrivateMessageReceived;
    public event EventHandler<TwitchRoomStateMessage>? RoomStateReceived;
    public event EventHandler<TwitchNoticeMessage>? NoticeReceived;
    public event EventHandler<TwitchClearChatMessage>? ClearChatReceived;

    private readonly TwitchChatClientOpts opts;

    private readonly List<string> autoJoinChannels = new();
    private PingManager? pingManager;

    public TwitchChatClient(bool secure, TwitchChatClientOpts opts, ILoggerFactory? loggerFactory)
        : this(secure ? wssUrl : wsUrl, opts, loggerFactory)
    {

    }

    public TwitchChatClient(Uri uri, TwitchChatClientOpts opts, ILoggerFactory? loggerFactory)
        : base(uri, opts, loggerFactory)
    {
        this.opts = opts;
    }

    public void AddAutoJoinChannel(string channel)
    {
        lock (autoJoinChannels)
        {
            autoJoinChannels.Add(channel);
        }
    }

    public Task SendMessageAsync(string channel, string text)
        => SendRawAsync($"PRIVMSG #{channel.ToLower()} :{text}");

    public Task SendMessageAsync(string channel, string text, string parentMessageId)
        => SendRawAsync($"@reply-parent-msg-id={parentMessageId} PRIVMSG #{channel.ToLower()} :{text}");

    public Task JoinAsync(string channel)
        => JoinAsync(connection, channel);

    public static Task JoinAsync(WsConnection? connection, string channel)
        => SendRawAsync(connection, $"JOIN #{channel.ToLower()}");

    public Task LeaveAsync(string channel)
        => SendRawAsync($"PART #{channel.ToLower()}");

    protected override async Task ConnectedAsync(WsConnection connection)
    {
        await base.ConnectAsync();

        await connection.SendAsync("CAP REQ :twitch.tv/tags twitch.tv/commands");
        await connection.SendAsync("PASS " + opts.OauthToken ?? GenerateAnonymName());
        await connection.SendAsync("NICK " + opts.Username?.ToLower() ?? "SCHMOOPIIE");

        pingManager = new(true, opts.PingDelay, opts.PingTimeout);
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

            case "PING":
                {
                    string text = message.parameters![0];
                    ProcessPing(connection, text);
                    return;
                }

            case "PONG":
                {
                    string text = message.parameters![0];
                    ProcessPong(text);
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

    private async void ProcessGlobalUserState(WsConnection connection, TwitchGlobalUserStateMessage message)
    {
        AuthFinished?.Invoke(this, message);

        string[] channels;
        lock (autoJoinChannels)
            channels = autoJoinChannels.ToArray();

        foreach (string channel in channels)
        {
            await JoinAsync(connection, channel);
        }
    }

    private async void ProcessPing(WsConnection connection, string text)
    {
        await SendRawAsync(connection, $"PONG :{text}");
    }

    private void ProcessPong(string text)
    {
        pingManager!.PongReceived(text);
    }

    private void ProcessChannelJoined(string channel)
    {
        ChannelJoined?.Invoke(this, channel);
    }

    private void ProcessPrivateMessage(TwitchPrivateMessage message)
    {
        PrivateMessageReceived?.Invoke(this, message);
    }

    private void ProcessRoomState(TwitchRoomStateMessage message)
    {
        RoomStateReceived?.Invoke(this, message);
    }

    private void ProcessNotice(TwitchNoticeMessage message)
    {
        NoticeReceived?.Invoke(this, message);
    }

    private void ProcessClearChat(TwitchClearChatMessage message)
    {
        ClearChatReceived?.Invoke(this, message);
    }

    private async Task Pinging(PingManager pingManager, string text)
    {
        if (pingManager != this.pingManager)
            return;

        await SendRawAsync($"PING :{text}");
    }

    private void Timeouted(PingManager pingManager)
    {
        if (pingManager != this.pingManager)
            return;

        connection!.Dispose(new Exception("Ping Timeout"));
    }

    protected override void ConnectionDisposing(object? sender, Exception? e)
    {
        if (pingManager != null)
        {
            pingManager.Stop();
            pingManager.Pinging -= Pinging;
            pingManager.Timeouted -= Timeouted;
        }

        base.ConnectionDisposing(sender, e);

        ConnectionClosed?.Invoke(this, e);
    }

    public static string GenerateAnonymName()
    {
        return $"justinfan{RandomNumberGenerator.GetInt32(0, 100000)}";
    }
}
