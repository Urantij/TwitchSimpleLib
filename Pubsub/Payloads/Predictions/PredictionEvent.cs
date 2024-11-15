using System.Text.Json.Serialization;

namespace TwitchSimpleLib.Pubsub.Payloads.Predictions;

public class PredictionEvent
{
    [JsonPropertyName("id")] public string Id { get; set; }
    // Не нужен
    //public string channel_id { get; set; }

    [JsonPropertyName("created_at")] public DateTimeOffset CreatedAt { get; set; }
    [JsonPropertyName("created_by")] public PredictionUser CreatedBy { get; set; }

    [JsonPropertyName("ended_at")] public DateTimeOffset? EndedAt { get; set; }
    [JsonPropertyName("ended_by")] public PredictionUser? EndedBy { get; set; }

    [JsonPropertyName("locked_at")] public DateTimeOffset? LockedAt { get; set; }
    [JsonPropertyName("locked_by")] public PredictionUser? LockedBy { get; set; }

    [JsonPropertyName("outcomes")] public PredictionOutcome[] Outcomes { get; set; }

    [JsonPropertyName("prediction_window_seconds")]
    public int PredictionWindowSeconds { get; set; }

    [JsonPropertyName("status")] public string Status { get; set; }
    [JsonPropertyName("title")] public string Title { get; set; }

    [JsonPropertyName("winning_outcome_id")]
    public string? WinningOutcomeId { get; set; }

    public PredictionEvent(string id, DateTimeOffset createdAt, PredictionUser createdBy, DateTimeOffset? endedAt,
        PredictionUser? endedBy, DateTimeOffset? lockedAt, PredictionUser? lockedBy, PredictionOutcome[] outcomes,
        int predictionWindowSeconds, string status, string title, string? winningOutcomeId)
    {
        Id = id;
        CreatedAt = createdAt;
        CreatedBy = createdBy;
        EndedAt = endedAt;
        EndedBy = endedBy;
        LockedAt = lockedAt;
        LockedBy = lockedBy;
        Outcomes = outcomes;
        PredictionWindowSeconds = predictionWindowSeconds;
        Status = status;
        Title = title;
        WinningOutcomeId = winningOutcomeId;
    }
}