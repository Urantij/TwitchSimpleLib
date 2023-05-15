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

    public readonly string fullTopic;

    public readonly TwitchPubsubClient client;

    public Action<string>? RawDataReceived;

    public PubsubAutoTopic(string channelTwitchId, string fullTopic, TwitchPubsubClient client)
    {
        this.channelTwitchId = channelTwitchId;
        this.fullTopic = fullTopic;
        this.client = client;
    }

    public Task UnlistenAsync()
    => client.RemoveAutoTopicAsync(this);

    internal void OnRawDataReceived(string data)
    {
        RawDataReceived?.Invoke(data);
    }
}
