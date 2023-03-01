using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using IrcParserNet.Irc;
using Microsoft.Extensions.Logging;
using TwitchSimpleLib.Irc;
using TwitchSimpleLib.Pubsub.Messages;
using TwitchSimpleLib.Pubsub.Payloads.Predictions;

namespace TwitchSimpleLib.Pubsub;

public class TwitchPubsubClient : BaseClient
{
    public static readonly Uri url = new("wss://pubsub-edge.twitch.tv");

    public event Action<(string channelId, PredictionData)>? PredictionReceived;

    private readonly List<string> topics = new();

    private readonly string? oauthToken;

    private static readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    protected TwitchPubsubClient(Uri uri, TwitchPubsubClientOpts opts, ILoggerFactory? loggerFactory)
        : base(uri, opts, loggerFactory)
    {
        this.oauthToken = opts.OauthToken;
    }

    public void AddPredictionsTopic(string channelTwitchId)
    {
        topics.Add("predictions-channel-v1." + channelTwitchId);
    }

    public Task SendMessageAsync(BasePubsubMessage messageObject)
        => SendMessageAsync(connection, messageObject);

    private Task SendMessageAsync(WsConnection? caller, BasePubsubMessage messageObject)
    {
        string messageString = JsonSerializer.Serialize(messageObject, messageObject.GetType(), options: jsonSerializerOptions);

        return SendRawAsync(caller, messageString);
    }

    protected override async Task ConnectedAsync(WsConnection connection)
    {
        await base.ConnectedAsync(connection);

        PubsubListenMessage listenMessage = new(new PubsubListenMessage.ListenData(topics, oauthToken), "Starting");

        await SendMessageAsync(connection, listenMessage);
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
                    var data = JsonSerializer.Deserialize<PredictionData>(unescapedMessage)!;

                    PredictionReceived?.Invoke((channelId, data));
                }
                return;
        }
    }
}
