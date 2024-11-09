using System.Text.Json.Serialization;

namespace TwitchSimpleLib.Pubsub.Payloads.Predictions;

public class PredictionData
{
    [JsonPropertyName("timestamp")] public DateTimeOffset Timestamp { get; set; }
    [JsonPropertyName("event")] public PredictionEvent Event { get; set; }

    public PredictionData(DateTimeOffset timestamp, PredictionEvent @event)
    {
        this.Timestamp = timestamp;
        this.Event = @event;
    }
}