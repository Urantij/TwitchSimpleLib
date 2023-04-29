using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TwitchSimpleLib.Pubsub;

/// <summary>
/// Хранит ссылку на клиент бота, аккуратно.
/// </summary>
public class PubsubAutoTopic
{
    public readonly string channelTwitchId;

    /// <summary>
    /// Просто топик, без айди канала.
    /// </summary>
    public readonly string topic;

    public readonly TwitchPubsubClient client;

    public Action<string>? RawDataReceived;

    public PubsubAutoTopic(string channelTwitchId, string topic, TwitchPubsubClient client)
    {
        this.channelTwitchId = channelTwitchId;
        this.topic = topic;
        this.client = client;
    }

    public Task UnlistenAsync()
    => client.RemoveAutoTopicAsync(this);

    internal void OnRawDataReceived(string data)
    {
        RawDataReceived?.Invoke(data);
    }

    /// <summary>
    /// "<see cref="topic"/>.<see cref="channelTwitchId"/>"
    /// </summary>
    /// <returns></returns>
    public string MakeFullTopic()
        => $"{topic}.{channelTwitchId}";
}
