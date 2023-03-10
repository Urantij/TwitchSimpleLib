using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TwitchSimpleLib.Pubsub;

public abstract class BasePubsubMessage
{
    [JsonPropertyName("type")]
    public string Type { get; set; }
    [JsonPropertyName("nonce")]
    public string? Nonce { get; set; }
    [JsonPropertyName("data")]
    public object? Data { get; set; }

    protected BasePubsubMessage(string type, object? data, string? nonce)
    {
        Type = type;
        Data = data;
        Nonce = nonce;
    }
}
