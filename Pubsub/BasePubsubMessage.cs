using System.Text.Json.Serialization;

namespace TwitchSimpleLib.Pubsub;

public abstract class BasePubsubMessage
{
    [JsonPropertyName("type")] public string Type { get; set; }
    [JsonPropertyName("nonce")] public string? Nonce { get; set; }
    [JsonPropertyName("data")] public object? Data { get; set; }

    protected BasePubsubMessage(string type, object? data, string? nonce)
    {
        Type = type;
        Data = data;
        Nonce = nonce;
    }
}