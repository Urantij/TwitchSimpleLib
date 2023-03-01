using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TwitchSimpleLib.Pubsub.Payloads.Predictions;

public class PredictionData
{
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }
    [JsonPropertyName("event")]
    public PredictionEvent Event { get; set; }

    public PredictionData(DateTimeOffset timestamp, PredictionEvent @event)
    {
        this.Timestamp = timestamp;
        this.Event = @event;
    }
}
