using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TwitchSimpleLib.Pubsub.Payloads.BroadcastSettings;

// {"channel_id":"100596648","type":"broadcast_settings_update","channel":"urantij","old_status":"ÑÐ´Ð¸Ð²Ð»ÐµÐ½Ð¸Ðµ","status":"udivleniye","old_game":"","game":"","old_game_id":0,"game_id":0}
public class BroadcastSettingsData
{
    /// <summary>
    /// broadcast_settings_update
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("old_status")]
    public string OldStatus { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("old_game")]
    public string OldGame { get; set; }

    [JsonPropertyName("game")]
    public string Game { get; set; }

    [JsonPropertyName("old_game_id")]
    public int OldGameId { get; set; }

    [JsonPropertyName("game_id")]
    public int GameId { get; set; }

    public BroadcastSettingsData(string type, string oldStatus, string status, string oldGame, string game, int oldGameId, int gameId)
    {
        Type = type;
        OldStatus = oldStatus;
        Status = status;
        OldGame = oldGame;
        Game = game;
        OldGameId = oldGameId;
        GameId = gameId;
    }
}
