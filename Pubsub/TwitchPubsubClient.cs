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

    public readonly TwitchPubsubClientOpts opts;

    public TwitchPubsubClient(TwitchPubsubClientOpts opts, ILoggerFactory? loggerFactory)
        : this(url, opts, loggerFactory)
    {
    }

    public TwitchPubsubClient(Uri uri, TwitchPubsubClientOpts opts, ILoggerFactory? loggerFactory)
        : base(uri, opts, loggerFactory)
    {
        this.opts = opts;
    }

    public int CountTopics()
    {
        lock (autoTopics)
        {
            return autoTopics.Count;
        }
    }

    /// <summary>
    /// Использовать перед запуском бота.
    /// </summary>
    /// <param name="channelTwitchId"></param>
    public PubsubAutoTopic<PredictionPayload> AddPredictionsTopic(string channelTwitchId)
    {
        return AddManualTopic<PredictionPayload>(channelTwitchId, $"{predictionsTopic}.{channelTwitchId}");
    }

    /// <summary>
    /// Использовать перед запуском бота.
    /// </summary>
    /// <param name="channelTwitchId"></param>
    public PubsubAutoTopic<PlaybackData> AddPlaybackTopic(string channelTwitchId)
    {
        return AddManualTopic<PlaybackData>(channelTwitchId, $"{videoPlaybackTopic}.{channelTwitchId}");
    }

    /// <summary>
    /// Использовать перед запуском бота.
    /// </summary>
    /// <param name="channelTwitchId"></param>
    public PubsubAutoTopic<BroadcastSettingsData> AddBroadcastSettingsTopic(string channelTwitchId)
    {
        return AddManualTopic<BroadcastSettingsData>(channelTwitchId, $"{broadcastSettingsTopic}.{channelTwitchId}");
    }

    /// <summary>
    /// Использовать перед запуском бота.
    /// </summary>
    /// <param name="channelTwitchId"></param>
    /// <param name="fullTopic">Полный топик, включающий и ключ и параметры.</param>
    /// <param name="token">Если указан, будет использоваться этот токен для аутентификации топика.</param>
    /// <returns></returns>
    public PubsubAutoTopic AddManualTopic(string channelTwitchId, string fullTopic, string? token = null)
    {
        PubsubAutoTopic autoTopic = new(channelTwitchId, fullTopic, token, this);

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
    /// <param name="fullTopic">Полный топик, включающий и ключ и параметры.</param>
    /// <param name="token">Если указан, будет использоваться этот токен для аутентификации топика.</param>
    /// <returns></returns>
    public PubsubAutoTopic<T> AddManualTopic<T>(string channelTwitchId, string fullTopic, string? token = null)
    {
        PubsubAutoTopic<T> autoTopic = new(channelTwitchId, fullTopic, token, this);

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
    /// <param name="channelTwitchId"></param>
    /// <param name="fullTopic">Полный топик, включающий и ключ и параметры.</param>
    /// <param name="token">Если указан, будет использоваться этот токен для аутентификации топика.</param>
    public async Task<PubsubAutoTopic> AddManualAutoTopicAsync(string channelTwitchId, string fullTopic, string? token = null)
    {
        var autoTopic = AddManualTopic(channelTwitchId, fullTopic, token);

        if (IsConnected)
        {
            await ListenAsync(new[] { autoTopic.fullTopic }, token);
        }

        return autoTopic;
    }

    /// <summary>
    /// Можно использовать после запуска бота.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="channelTwitchId"></param>
    /// <param name="fullTopic">Полный топик, включающий и ключ и параметры.</param>
    /// <param name="token">Если указан, будет использоваться этот токен для аутентификации топика.</param>
    /// <returns></returns>
    public async Task<PubsubAutoTopic<T>> AddManualAutoTopicAsync<T>(string channelTwitchId, string fullTopic, string? token)
    {
        var autoTopic = AddManualTopic<T>(channelTwitchId, fullTopic, token);

        if (IsConnected)
        {
            await ListenAsync(new[] { autoTopic.fullTopic }, token);
        }

        return autoTopic;
    }

    /// <summary>
    /// Можно использовать после запуска бота.
    /// Асинхронная часть будет запущена отдельным таском.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="channelTwitchId"></param>
    /// <param name="fullTopic">Полный топик, включающий и ключ и параметры.</param>
    /// <param name="token">Если указан, будет использоваться этот токен для аутентификации топика.</param>
    /// <returns></returns>
    public PubsubAutoTopic<T> AddManualAutoTopic<T>(string channelTwitchId, string fullTopic, string? token)
    {
        var autoTopic = AddManualTopic<T>(channelTwitchId, fullTopic, token);

        if (IsConnected)
        {
            Task.Run(async () =>
            {
                await ListenAsync(new[] { autoTopic.fullTopic }, token);
            });
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
            _logger?.LogWarning("Попытка удалить автотопик, которого уже нет. {topic}", autoTopic.fullTopic);
        }

        if (!IsConnected)
            return;

        await UnListenAsync(new[] { autoTopic.fullTopic });
    }

    /// <summary>
    /// Асинхронная часть будет запущена отдельным таском.
    /// </summary>
    /// <param name="autoTopic"></param>
    /// <returns></returns>
    public void RemoveAutoTopic(PubsubAutoTopic autoTopic)
    {
        bool removed;

        lock (autoTopics)
        {
            removed = autoTopics.Remove(autoTopic);
        }

        if (!removed)
        {
            _logger?.LogWarning("Попытка удалить автотопик, которого уже нет. {topic}", autoTopic.fullTopic);
        }

        if (!IsConnected)
            return;

        Task.Run(async () =>
        {
            await UnListenAsync(new[] { autoTopic.fullTopic });
        });
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
                _logger?.LogWarning("Попытка удалить автотопик, которого уже нет. {topic}", autoTopic.fullTopic);
            }

            topics.Add(autoTopic.fullTopic);
        }

        if (!IsConnected)
            return;

        await UnListenAsync(topics);
    }

    /// <summary>
    /// Асинхронная часть будет запущена отдельным таском.
    /// </summary>
    /// <param name="autoTopicsToRemove"></param>
    /// <returns></returns>
    public void RemoveAutoTopics(IEnumerable<PubsubAutoTopic> autoTopicsToRemove)
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
                _logger?.LogWarning("Попытка удалить автотопик, которого уже нет. {topic}", autoTopic.fullTopic);
            }

            topics.Add(autoTopic.fullTopic);
        }

        if (!IsConnected)
            return;

        Task.Run(async () =>
        {
            await UnListenAsync(topics);
        });
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
            var topics = autoTopicsToRemove.Select(at => at.fullTopic).ToArray();

            await UnListenAsync(topics);
        }

        return autoTopicsToRemove;
    }

    /// <summary>
    /// Удаление всех топиков по айди канала.
    /// Асинхронная часть будет запущена отдельным таском.
    /// </summary>
    /// <param name="channelTwitchId"></param>
    /// <returns></returns>
    public PubsubAutoTopic[] RemoveAutoTopics(string channelTwitchId)
    {
        PubsubAutoTopic[] autoTopicsToRemove;
        lock (autoTopics)
        {
            autoTopicsToRemove = autoTopics.Where(at => at.channelTwitchId == channelTwitchId).ToArray();
        }

        if (IsConnected && autoTopicsToRemove.Length > 0)
        {
            var topics = autoTopicsToRemove.Select(at => at.fullTopic).ToArray();

            Task.Run(async () =>
            {
                await UnListenAsync(topics);
            });
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

    private async Task ListenAsync(IEnumerable<string> topics, string? token)
    {
        token ??= opts.OauthToken;

        PubsubListenMessage listenMessage = new(new PubsubListenMessage.ListenData(topics, token), "Starting");

        await SendMessageAsync(connection, listenMessage);
    }

    private async Task UnListenAsync(IEnumerable<string> topics)
    {
        PubsubUnlistenMessage listenMessage = new(new PubsubUnlistenMessage.ListenData(topics), "Starting");

        await SendMessageAsync(connection, listenMessage);
    }

    protected override async Task ConnectedAsync(WsConnection connection)
    {
        await base.ConnectedAsync(connection);

        (string? token, string[] topic)[] topicsTuples;
        lock (autoTopics)
        {
            topicsTuples = autoTopics
            .GroupBy(t => t.token ?? opts.OauthToken)
            .Select(g => (g.Key ?? opts.OauthToken, g.Select(auto => auto.fullTopic).ToArray()))
            .ToArray();
        }

        if (topicsTuples.Length > 0)
        {
            foreach (var (token, topic) in topicsTuples)
            {
                await ListenAsync(topic, token);
            }
        }
        else
        {
            _logger?.LogWarning("Клиент обязан подписаться на какой-нибудь топик в течение 15 секунд после подключения. Но никаких топиков на данный момент нет.");
        }

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
        string unescapedMessage = message.Data.Message.Replace("\\\"", "\"");

        PubsubAutoTopic? autoTopic;
        lock (autoTopics)
        {
            autoTopic = autoTopics.FirstOrDefault(at => at.fullTopic == message.Data.Topic);
        }

        if (autoTopic != null)
        {
            autoTopic.OnRawDataReceived(unescapedMessage);
        }
        else
        {
            _logger?.LogError("Пришло сообщение по неизвестному автотопику {topic}", message.Data.Topic);
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
