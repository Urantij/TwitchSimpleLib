using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TwitchSimpleLib.Pubsub.Payloads.Predictions;

public class PredictionPayload
{
    [JsonPropertyName("type")]
    public string Type { get; set; }
    [JsonPropertyName("data")]
    public PredictionData Data { get; set; }

    public PredictionPayload(string type, PredictionData data)
    {
        Type = type;
        Data = data;
    }
}
