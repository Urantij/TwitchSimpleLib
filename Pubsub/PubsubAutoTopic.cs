namespace TwitchSimpleLib.Pubsub;

/// <summary>
/// Хранит ссылку на клиент бота, аккуратно.
/// </summary>
public class PubsubAutoTopic
{
    public readonly string channelTwitchId;

    public readonly string fullTopic;

    /// <summary>
    /// Если не нулл, используется для аутентификации этот токен вместо стандартного.
    /// </summary>
    public readonly string? token;

    public readonly TwitchPubsubClient client;

    public Action<string>? RawDataReceived;

    public PubsubAutoTopic(string channelTwitchId, string fullTopic, string? token, TwitchPubsubClient client)
    {
        this.channelTwitchId = channelTwitchId;
        this.fullTopic = fullTopic;
        this.token = token;
        this.client = client;
    }

    public Task UnlistenAsync()
        => client.RemoveAutoTopicAsync(this);

    internal void OnRawDataReceived(string data)
    {
        RawDataReceived?.Invoke(data);
    }
}