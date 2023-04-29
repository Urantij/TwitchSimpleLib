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
    public const string predictionsTopic = "predictions-channel-v1";
    public const string videoPlaybackTopic = "video-playback-by-id";
    public const string broadcastSettingsTopic = "broadcast-settings-update";

    public static readonly Uri url = new("wss://pubsub-edge.twitch.tv");

    private readonly List<PubsubAutoTopic> autoTopics = new();
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

    /// <summary>
    /// Использовать перед запуском бота.
    /// </summary>
    /// <param name="channelTwitchId"></param>
    public PubsubAutoTopic<PredictionPayload> AddPredictionsTopic(string channelTwitchId)
    {
        return AddTopic<PredictionPayload>(channelTwitchId, predictionsTopic);
    }

    /// <summary>
    /// Использовать перед запуском бота.
    /// </summary>
    /// <param name="channelTwitchId"></param>
    public PubsubAutoTopic<PlaybackData> AddPlaybackTopic(string channelTwitchId)
    {
        return AddTopic<PlaybackData>(channelTwitchId, videoPlaybackTopic);
    }

    /// <summary>
    /// Использовать перед запуском бота.
    /// </summary>
    /// <param name="channelTwitchId"></param>
    public PubsubAutoTopic<BroadcastSettingsData> AddBroadcastSettingsTopic(string channelTwitchId)
    {
        return AddTopic<BroadcastSettingsData>(channelTwitchId, broadcastSettingsTopic);
    }

    /// <summary>
    /// Использовать перед запуском бота.
    /// </summary>
    /// <param name="channelTwitchId"></param>
    /// <param name="topic"></param>
    /// <returns></returns>
    public PubsubAutoTopic AddTopic(string channelTwitchId, string topic)
    {
        PubsubAutoTopic autoTopic = new(channelTwitchId, topic, this);

        lock (autoTopics)
        {
            autoTopics.Add(autoTopic);
        }

        return autoTopic;
    }

    /// <summary>
    /// Использовать перед запуском бота.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="channelTwitchId"></param>
    /// <param name="topic"></param>
    /// <returns></returns>
    public PubsubAutoTopic<T> AddTopic<T>(string channelTwitchId, string topic)
    {
        PubsubAutoTopic<T> autoTopic = new(channelTwitchId, topic, this);

        autoTopic.RawDataReceived += (rawData) =>
        {
            var data = JsonSerializer.Deserialize<T>(rawData)!;
            autoTopic.OnDataReceived(data);
        };

        lock (autoTopics)
        {
            autoTopics.Add(autoTopic);
        }

        return autoTopic;
    }

    /// <summary>
    /// Можно использовать после запуска бота.
    /// </summary>
    public async Task<PubsubAutoTopic> AddAutoTopicAsync(string channelTwitchId, string topic)
    {
        var autoTopic = AddTopic(channelTwitchId, topic);

        if (IsConnected)
        {
            await ListenAsync(new[] { autoTopic.MakeFullTopic() });
        }

        return autoTopic;
    }

    /// <summary>
    /// Можно использовать после запуска бота.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="channelTwitchId"></param>
    /// <param name="topic"></param>
    /// <returns></returns>
    public async Task<PubsubAutoTopic<T>> AddAutoTopicAsync<T>(string channelTwitchId, string topic)
    {
        var autoTopic = AddTopic<T>(channelTwitchId, topic);

        if (IsConnected)
        {
            await ListenAsync(new[] { autoTopic.MakeFullTopic() });
        }

        return autoTopic;
    }

    public async Task RemoveAutoTopicAsync(PubsubAutoTopic autoTopic)
    {
        bool removed;

        lock (autoTopics)
        {
            removed = autoTopics.Remove(autoTopic);
        }

        if (!removed)
        {
            _logger?.LogWarning("Попытка удалить автотопик, которого уже нет. {topic}.{channel}", autoTopic.topic, autoTopic.channelTwitchId);
        }

        if (!IsConnected)
            return;

        await UnListenAsync(new[] { autoTopic.MakeFullTopic() });
    }

    public async Task RemoveAutoTopicsAsync(IEnumerable<PubsubAutoTopic> autoTopicsToRemove)
    {
        List<string> topics = new();

        foreach (var autoTopic in autoTopicsToRemove)
        {
            bool removed;

            lock (autoTopics)
            {
                removed = autoTopics.Remove(autoTopic);
            }

            if (!removed)
            {
                _logger?.LogWarning("Попытка удалить автотопик, которого уже нет. {topic}.{channel}", autoTopic.topic, autoTopic.channelTwitchId);
            }

            topics.Add(autoTopic.MakeFullTopic());
        }

        if (!IsConnected)
            return;

        await UnListenAsync(topics);
    }

    /// <summary>
    /// Удаление всех топиков по айди канала.
    /// </summary>
    /// <param name="channelTwitchId"></param>
    /// <returns></returns>
    public async Task<PubsubAutoTopic[]> RemoveAutoTopicsAsync(string channelTwitchId)
    {
        PubsubAutoTopic[] autoTopicsToRemove;
        lock (autoTopics)
        {
            autoTopicsToRemove = autoTopics.Where(at => at.channelTwitchId == channelTwitchId).ToArray();
        }

        if (IsConnected && autoTopicsToRemove.Length > 0)
        {
            var topics = autoTopicsToRemove.Select(at => at.MakeFullTopic()).ToArray();

            await UnListenAsync(topics);
        }

        return autoTopicsToRemove;
    }

    public Task SendMessageAsync(BasePubsubMessage messageObject)
        => SendMessageAsync(connection, messageObject);

    private Task SendMessageAsync(WsConnection? caller, BasePubsubMessage messageObject)
    {
        string messageString = JsonSerializer.Serialize(messageObject, messageObject.GetType());

        return SendRawAsync(caller, messageString);
    }

    private async Task ListenAsync(IEnumerable<string> topics)
    {
        PubsubListenMessage listenMessage = new(new PubsubListenMessage.ListenData(topics, opts.OauthToken), "Starting");

        await SendMessageAsync(connection, listenMessage);
    }

    private async Task UnListenAsync(IEnumerable<string> topics)
    {
        PubsubUnlistenMessage listenMessage = new(new PubsubUnlistenMessage.ListenData(topics, opts.OauthToken), "Starting");

        await SendMessageAsync(connection, listenMessage);
    }

    protected override async Task ConnectedAsync(WsConnection connection)
    {
        await base.ConnectedAsync(connection);

        var topics = autoTopics.Select(auto => auto.MakeFullTopic()).ToArray();
        await ListenAsync(topics);

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

        PubsubAutoTopic? autoTopic;
        lock (autoTopics)
        {
            autoTopic = autoTopics.FirstOrDefault(at => at.channelTwitchId == channelId && at.topic == topic);
        }

        if (autoTopic != null)
        {
            autoTopic.OnRawDataReceived(unescapedMessage);
        }
        else
        {
            _logger?.LogError("Пришло сообщение по неизвестному автотопику {topic}.{channel}", topic, channelId);
        }
    }

    private async Task Pinging(PingManager pingManager, string text)
    {
        if (pingManager != this.pingManager || !IsConnected)
            return;

        PubsubPingMessage message = new();
        await SendMessageAsync(message);
    }

    private void Timeouted(PingManager pingManager)
    {
        if (pingManager != this.pingManager || !IsConnected)
            return;

        connection!.Dispose(new Exception("Ping Timeout"));
    }

    protected override void ConnectionDisposing(object? sender, Exception? e)
    {
        if (pingManager != null)
        {
            pingManager.Pinging -= Pinging;
            pingManager.Timeouted -= Timeouted;
            pingManager.Stop();
        }

        base.ConnectionDisposing(sender, e);
    }
}
