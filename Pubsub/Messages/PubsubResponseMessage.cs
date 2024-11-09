using System.Text.Json.Serialization;

namespace TwitchSimpleLib.Pubsub.Messages;

public class PubsubResponseMessage
{
    [JsonPropertyName("nonce")] public string? Nonce { get; private set; }
    [JsonPropertyName("error")] public string Error { get; private set; }

    public PubsubResponseMessage(string? nonce, string error)
    {
        this.Nonce = nonce;
        this.Error = error;
    }
}