using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using IrcParserNet.Irc;
using Microsoft.Extensions.Logging;
using TwitchSimpleLib.Irc;
using TwitchSimpleLib.Pubsub.Messages;
using TwitchSimpleLib.Pubsub.Payloads.BroadcastSettings;
using TwitchSimpleLib.Pubsub.Payloads.Playback;
using TwitchSimpleLib.Pubsub.Payloads.Predictions;

namespace TwitchSimpleLib.Pubsub;

public class TwitchPubsubClient : BaseClient
{
    public static readonly Uri url = new("wss://pubsub-edge.twitch.tv");

    public event Action<(string channelId, PredictionPayload)>? PredictionReceived;
    public event Action<(string channelId, PlaybackData)>? PlaybackReceived;
    public event Action<(string channelId, BroadcastSettingsData)>? BroadcastSettingsReceived;

    private readonly List<string> topics = new();

    private PingManager? pingManager;

    private readonly TwitchPubsubClientOpts opts;

    public TwitchPubsubClient(TwitchPubsubClientOpts opts, ILoggerFactory? loggerFactory)
        : this(url, opts, loggerFactory)
    {
    }

    public TwitchPubsubClient(Uri uri, TwitchPubsubClientOpts opts, ILoggerFactory? loggerFactory)
        : base(uri, opts, loggerFactory)
    {
        this.opts = opts;
    }

    public void AddPredictionsTopic(string channelTwitchId)
    {
        topics.Add("predictions-channel-v1." + channelTwitchId);
    }

    public void AddPlaybackTopic(string channelTwitchId)
    {
        topics.Add("video-playback-by-id." + channelTwitchId);
    }

    public void AddBroadcastSettingsTopic(string channelTwitchId)
    {
        topics.Add("broadcast-settings-update." + channelTwitchId);
    }

    public Task SendMessageAsync(BasePubsubMessage messageObject)
        => SendMessageAsync(connection, messageObject);

    private Task SendMessageAsync(WsConnection? caller, BasePubsubMessage messageObject)
    {
        string messageString = JsonSerializer.Serialize(messageObject, messageObject.GetType());

        return SendRawAsync(caller, messageString);
    }

    protected override async Task ConnectedAsync(WsConnection connection)
    {
        await base.ConnectedAsync(connection);

        PubsubListenMessage listenMessage = new(new PubsubListenMessage.ListenData(topics, opts.OauthToken), "Starting");

        await SendMessageAsync(connection, listenMessage);

        pingManager = new(false, opts.PingDelay, opts.PingTimeout);
        pingManager.Pinging += Pinging;
        pingManager.Timeouted += Timeouted;
        pingManager.Start();
    }

    protected override void MessageReceived(object? sender, string e)
    {
        base.MessageReceived(sender, e);

        JsonDocument doc = JsonDocument.Parse(e);
        string? type = doc.RootElement.GetProperty("type").GetString();

        if (type == null)
        {
            _logger?.LogCritical("Unknown message {text}", e);
            return;
        }

        switch (type)
        {
            case "PONG":
                pingManager?.PongReceived("");
                return;

            case "RECONNECT":
                return;

            case "RESPONSE":
                {
                    PubsubResponseMessage? message = JsonSerializer.Deserialize<PubsubResponseMessage>(e);
                    if (message == null)
                    {
                        _logger?.LogCritical("Bad RESPONSE message {text}", e);
                        return;
                    }
                    ProcessResponseMessage(message);
                    return;
                }

            case "MESSAGE":
                {
                    PubsubMessage? message = JsonSerializer.Deserialize<PubsubMessage>(e);
                    if (message == null)
                    {
                        _logger?.LogCritical("Bad MESSAGE message {text}", e);
                        return;
                    }
                    ProcessMessage(message);
                    return;
                }
        }
    }

    private void ProcessResponseMessage(PubsubResponseMessage message)
    {
        if (!string.IsNullOrEmpty(message.Error))
        {
            _logger?.LogError("Response error {nonce}, {error}", message.Nonce, message.Error);
        }
    }

    private void ProcessMessage(PubsubMessage message)
    {
        string topic;
        string channelId;

        {
            string[] split = message.Data.Topic.Split('.');
            topic = split[0];
            channelId = split[1];
        }

        string unescapedMessage = message.Data.Message.Replace("\\\"", "\"");

        switch (topic)
        {
            case "predictions-channel-v1":
                {
                    var data = JsonSerializer.Deserialize<PredictionPayload>(unescapedMessage)!;

                    PredictionReceived?.Invoke((channelId, data));
                }
                return;
            case "video-playback-by-id":
                {
                    var data = JsonSerializer.Deserialize<PlaybackData>(unescapedMessage)!;

                    PlaybackReceived?.Invoke((channelId, data));
                }
                return;
            case "broadcast-settings-update":
                {
                    var data = JsonSerializer.Deserialize<BroadcastSettingsData>(unescapedMessage)!;

                    BroadcastSettingsReceived?.Invoke((channelId, data));
                }
                return;
        }
    }

    private async Task Pinging(PingManager pingManager, string text)
    {
        if (pingManager != this.pingManager)
            return;

        PubsubPingMessage message = new();
        await SendMessageAsync(message);
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
    }
}
