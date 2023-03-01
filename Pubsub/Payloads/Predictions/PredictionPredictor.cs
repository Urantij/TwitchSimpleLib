using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TwitchSimpleLib.Pubsub.Payloads.Predictions;

public class PredictionPredictor
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    // В комментах, потому что это нам не нужно.
    // public string event_id { get; set; }
    // public string outcome_id { get; set; }
    // public string channel_id { get; set; }
    // public PredictionResultPayload? result { get; set; }

    [JsonPropertyName("points")]
    public int Points { get; set; }

    [JsonPropertyName("predicted_at")]
    public DateTimeOffset PredictedAt { get; set; }
    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    [JsonPropertyName("user_id")]
    public string UserId { get; set; }
    [JsonPropertyName("user_display_name")]
    public string UserDisplayName { get; set; }

    public PredictionPredictor(string id, int points, DateTimeOffset predictedAt, DateTimeOffset updatedAt, string userId, string userDisplayName)
    {
        Id = id;
        Points = points;
        PredictedAt = predictedAt;
        UpdatedAt = updatedAt;
        UserId = userId;
        UserDisplayName = userDisplayName;
    }
}
