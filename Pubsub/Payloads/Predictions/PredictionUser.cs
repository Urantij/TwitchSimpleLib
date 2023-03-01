using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TwitchSimpleLib.Pubsub.Payloads.Predictions;

public class PredictionUser
{
    [JsonPropertyName("user_id")]
    public string UserId { get; set; }
    [JsonPropertyName("user_display_name")]
    public string UserDisplayName { get; set; }

    public PredictionUser(string userId, string userDisplayName)
    {
        UserId = userId;
        UserDisplayName = userDisplayName;
    }
}
