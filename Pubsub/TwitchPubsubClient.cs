using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using TwitchSimpleLib.Irc;
using TwitchSimpleLib.Pubsub.Messages;
using TwitchSimpleLib.Pubsub.Payloads.BroadcastSettings;
using TwitchSimpleLib.Pubsub.Payloads.Playback;
using TwitchSimpleLib.Pubsub.Payloads.Predictions;

namespace TwitchSimpleLib.Pubsub;

public class TwitchPubsubClient : BaseClient
{
    public const string PredictionsTopic = "predictions-channel-v1";
    public const string VideoPlaybackTopic = "video-playback-by-id";
    public const string BroadcastSettingsTopic = "broadcast-settings-update";

    public static readonly Uri Url = new("wss://pubsub-edge.twitch.tv");

    private readonly List<PubsubAutoTopic> _autoTopics = new();
    private PingManager? _pingManager;

    public readonly TwitchPubsubClientOpts Opts;

    public TwitchPubsubClient(TwitchPubsubClientOpts opts, ILoggerFactory? loggerFactory,
        CancellationToken cancellationToken = default)
        : this(Url, opts, loggerFactory, cancellationToken)
    {
    }

    public TwitchPubsubClient(Uri uri, TwitchPubsubClientOpts opts, ILoggerFactory? loggerFactory,
        CancellationToken cancellationToken = default)
        : base(uri, opts, loggerFactory, cancellationToken)
    {
        this.Opts = opts;
    }

    public int CountTopics()
    {
        lock (_autoTopics)
        {
            return _autoTopics.Count;
        }
    }

    /// <summary>
    /// Использовать перед запуском бота.
    /// </summary>
    /// <param name="channelTwitchId"></param>
    public PubsubAutoTopic<PredictionPayload> AddPredictionsTopic(string channelTwitchId)
    {
        return AddManualTopic<PredictionPayload>(channelTwitchId, $"{PredictionsTopic}.{channelTwitchId}");
    }

    /// <summary>
    /// Использовать перед запуском бота.
    /// </summary>
    /// <param name="channelTwitchId"></param>
    public PubsubAutoTopic<PlaybackData> AddPlaybackTopic(string channelTwitchId)
    {
        return AddManualTopic<PlaybackData>(channelTwitchId, $"{VideoPlaybackTopic}.{channelTwitchId}");
    }

    /// <summary>
    /// Использовать перед запуском бота.
    /// </summary>
    /// <param name="channelTwitchId"></param>
    public PubsubAutoTopic<BroadcastSettingsData> AddBroadcastSettingsTopic(string channelTwitchId)
    {
        return AddManualTopic<BroadcastSettingsData>(channelTwitchId, $"{BroadcastSettingsTopic}.{channelTwitchId}");
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

        lock (_autoTopics)
        {
            _autoTopics.Add(autoTopic);
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
    /// <param name="serializerContext">Нужен для десериализации в aot сценарии</param>
    /// <returns></returns>
    public PubsubAutoTopic<T> AddManualTopic<T>(string channelTwitchId, string fullTopic,
        JsonSerializerContext serializerContext, string? token = null) where T : class
    {
        // TODO я очень устал, я не знаю, как сделать 1 код
        // Типа... как сделать, чтобы оно не супресилось там или тут...

        PubsubAutoTopic<T> autoTopic = new(channelTwitchId, fullTopic, token, this);

        autoTopic.RawDataReceived += (rawData) =>
        {
            T? data = JsonSerializer.Deserialize(rawData, typeof(T), serializerContext) as T;

            if (data == null)
                return;

            autoTopic.OnDataReceived(data);
        };

        lock (_autoTopics)
        {
            _autoTopics.Add(autoTopic);
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
    [RequiresDynamicCode("Use JsonSerializerContext overload")]
    [RequiresUnreferencedCode("Use JsonSerializerContext overload")]
    public PubsubAutoTopic<T> AddManualTopic<T>(string channelTwitchId, string fullTopic, string? token = null)
        where T : class
    {
        // TODO я очень устал, я не знаю, как сделать 1 код

        PubsubAutoTopic<T> autoTopic = new(channelTwitchId, fullTopic, token, this);

        autoTopic.RawDataReceived += (rawData) =>
        {
            T? data = JsonSerializer.Deserialize<T>(rawData);

            if (data == null)
                return;

            autoTopic.OnDataReceived(data);
        };

        lock (_autoTopics)
        {
            _autoTopics.Add(autoTopic);
        }

        return autoTopic;
    }

    /// <summary>
    /// Можно использовать после запуска бота.
    /// </summary>
    /// <param name="channelTwitchId"></param>
    /// <param name="fullTopic">Полный топик, включающий и ключ и параметры.</param>
    /// <param name="token">Если указан, будет использоваться этот токен для аутентификации топика.</param>
    public async Task<PubsubAutoTopic> AddManualAutoTopicAsync(string channelTwitchId, string fullTopic,
        string? token = null)
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
    /// <param name="serializerContext"></param>
    /// <param name="token">Если указан, будет использоваться этот токен для аутентификации топика.</param>
    /// <returns></returns>
    public async Task<PubsubAutoTopic<T>> AddManualAutoTopicAsync<T>(string channelTwitchId, string fullTopic,
        JsonSerializerContext serializerContext, string? token) where T : class
    {
        // TODO я очень устал, я не знаю, как сделать 1 код

        var autoTopic = AddManualTopic<T>(channelTwitchId, fullTopic, serializerContext, token);

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
    [RequiresDynamicCode("Use JsonSerializerContext overload")]
    [RequiresUnreferencedCode("Use JsonSerializerContext overload")]
    public async Task<PubsubAutoTopic<T>> AddManualAutoTopicAsync<T>(string channelTwitchId, string fullTopic,
        string? token) where T : class
    {
        // TODO я очень устал, я не знаю, как сделать 1 код

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
    /// <param name="serializerContext"></param>
    /// <param name="token">Если указан, будет использоваться этот токен для аутентификации топика.</param>
    /// <returns></returns>
    public PubsubAutoTopic<T> AddManualAutoTopic<T>(string channelTwitchId, string fullTopic,
        JsonSerializerContext serializerContext, string? token) where T : class
    {
        // TODO я очень устал, я не знаю, как сделать 1 код

        var autoTopic = AddManualTopic<T>(channelTwitchId, fullTopic, serializerContext, token);

        if (IsConnected)
        {
            Task.Run(async () => { await ListenAsync(new[] { autoTopic.fullTopic }, token); });
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
    [RequiresDynamicCode("Use JsonSerializerContext overload")]
    [RequiresUnreferencedCode("Use JsonSerializerContext overload")]
    public PubsubAutoTopic<T> AddManualAutoTopic<T>(string channelTwitchId, string fullTopic, string? token)
        where T : class
    {
        // TODO я очень устал, я не знаю, как сделать 1 код

        var autoTopic = AddManualTopic<T>(channelTwitchId, fullTopic, token);

        if (IsConnected)
        {
            Task.Run(async () => { await ListenAsync(new[] { autoTopic.fullTopic }, token); });
        }

        return autoTopic;
    }

    public async Task RemoveAutoTopicAsync(PubsubAutoTopic autoTopic)
    {
        bool removed;

        lock (_autoTopics)
        {
            removed = _autoTopics.Remove(autoTopic);
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

        lock (_autoTopics)
        {
            removed = _autoTopics.Remove(autoTopic);
        }

        if (!removed)
        {
            _logger?.LogWarning("Попытка удалить автотопик, которого уже нет. {topic}", autoTopic.fullTopic);
        }

        if (!IsConnected)
            return;

        Task.Run(async () => { await UnListenAsync(new[] { autoTopic.fullTopic }); });
    }

    public async Task RemoveAutoTopicsAsync(IEnumerable<PubsubAutoTopic> autoTopicsToRemove)
    {
        List<string> topics = new();

        foreach (var autoTopic in autoTopicsToRemove)
        {
            bool removed;

            lock (_autoTopics)
            {
                removed = _autoTopics.Remove(autoTopic);
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

            lock (_autoTopics)
            {
                removed = _autoTopics.Remove(autoTopic);
            }

            if (!removed)
            {
                _logger?.LogWarning("Попытка удалить автотопик, которого уже нет. {topic}", autoTopic.fullTopic);
            }

            topics.Add(autoTopic.fullTopic);
        }

        if (!IsConnected)
            return;

        Task.Run(async () => { await UnListenAsync(topics); });
    }

    /// <summary>
    /// Удаление всех топиков по айди канала.
    /// </summary>
    /// <param name="channelTwitchId"></param>
    /// <returns></returns>
    public async Task<PubsubAutoTopic[]> RemoveAutoTopicsAsync(string channelTwitchId)
    {
        PubsubAutoTopic[] autoTopicsToRemove;
        lock (_autoTopics)
        {
            autoTopicsToRemove = _autoTopics.Where(at => at.channelTwitchId == channelTwitchId).ToArray();
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
        lock (_autoTopics)
        {
            autoTopicsToRemove = _autoTopics.Where(at => at.channelTwitchId == channelTwitchId).ToArray();
        }

        if (IsConnected && autoTopicsToRemove.Length > 0)
        {
            var topics = autoTopicsToRemove.Select(at => at.fullTopic).ToArray();

            Task.Run(async () => { await UnListenAsync(topics); });
        }

        return autoTopicsToRemove;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="messageObject"></param>
    /// <param name="serializerContext">В aot сценарии. Если сообщение из стандартного списка, указывать не нужно. Иначе нужен контекст, способный сериализовать поданное сообщение.</param>
    /// <returns></returns>
    public Task SendMessageAsync(BasePubsubMessage messageObject, JsonSerializerContext? serializerContext = null)
        => SendMessageAsync(_connection, messageObject, serializerContext);

    private Task SendMessageAsync(WsConnection? caller, BasePubsubMessage messageObject,
        JsonSerializerContext? serializerContext)
    {
        string messageString = JsonSerializer.Serialize(messageObject, messageObject.GetType(),
            serializerContext ?? SourceGenerationContext.Default);

        return SendRawAsync(caller, messageString);
    }

    private async Task ListenAsync(IEnumerable<string> topics, string? token)
    {
        token ??= Opts.OauthToken;

        PubsubListenMessage listenMessage = new(new PubsubListenMessage.ListenData(topics, token), "Starting");

        await SendMessageAsync(_connection, listenMessage, null);
    }

    private async Task UnListenAsync(IEnumerable<string> topics)
    {
        PubsubUnlistenMessage listenMessage = new(new PubsubUnlistenMessage.ListenData(topics), "Starting");

        await SendMessageAsync(_connection, listenMessage, null);
    }

    protected override async Task ConnectedAsync(WsConnection connection)
    {
        await base.ConnectedAsync(connection);

        (string? token, string[] topic)[] topicsTuples;
        lock (_autoTopics)
        {
            topicsTuples = _autoTopics
                .GroupBy(t => t.token ?? Opts.OauthToken)
                .Select(g => (g.Key ?? Opts.OauthToken, g.Select(auto => auto.fullTopic).ToArray()))
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
            _logger?.LogWarning(
                "Клиент обязан подписаться на какой-нибудь топик в течение 15 секунд после подключения. Но никаких топиков на данный момент нет.");
        }

        _pingManager = new(false, Opts.PingDelay, Opts.PingTimeout, state: connection,
            cancellationToken: _cancellationToken);
        _pingManager.Pinging += Pinging;
        _pingManager.Timeouted += Timeouted;
        _pingManager.Start();
    }

    protected override void MessageReceived(object? sender, string e)
    {
        WsConnection thisConnection = (WsConnection)sender!;

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
                if (_pingManager?.State == thisConnection)
                    _pingManager.PongReceived("");
                return;

            case "RECONNECT":
                return;

            case "RESPONSE":
            {
                PubsubResponseMessage? message =
                    JsonSerializer.Deserialize(e, typeof(PubsubResponseMessage), SourceGenerationContext.Default) as
                        PubsubResponseMessage;
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
                PubsubMessage? message =
                    JsonSerializer.Deserialize(e, typeof(PubsubMessage), SourceGenerationContext.Default) as
                        PubsubMessage;
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
        lock (_autoTopics)
        {
            autoTopic = _autoTopics.FirstOrDefault(at => at.fullTopic == message.Data.Topic);
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
        if (pingManager != this._pingManager || pingManager.State != _connection || !IsConnected)
            return;

        PubsubPingMessage message = new();
        await SendMessageAsync(message);
    }

    private void Timeouted(PingManager pingManager)
    {
        if (pingManager != this._pingManager || pingManager.State != _connection || !IsConnected)
            return;

        _connection!.Dispose(new Exception("Ping Timeout"));
    }

    protected override void ConnectionDisposing(object? sender, Exception? e)
    {
        if (_pingManager != null)
        {
            _pingManager.Pinging -= Pinging;
            _pingManager.Timeouted -= Timeouted;
            _pingManager.Stop();
        }

        base.ConnectionDisposing(sender, e);
    }
}