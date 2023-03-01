using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TwitchSimpleLib.Pubsub.Payloads.Predictions;

public class PredictionOutcome
{
    public string Id { get; set; }
    public string Color { get; set; }
    public string Title { get; set; }

    [JsonPropertyName("total_points")]
    public int TotalPoints { get; set; }
    [JsonPropertyName("total_users")]
    public int TotalUsers { get; set; }

    [JsonPropertyName("top_predictors")]
    public PredictionPredictor[] TopPredictors { get; set; }

    public PredictionOutcome(string id, string color, string title, int totalPoints, int totalUsers, PredictionPredictor[] topPredictors)
    {
        Id = id;
        Color = color;
        Title = title;
        TotalPoints = totalPoints;
        TotalUsers = totalUsers;
        TopPredictors = topPredictors;
    }
}
