using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TwitchSimpleLib.Pubsub.Payloads.Playback;

// {"server_time":1677679211,"play_delay":0,"type":"stream-up"}
public class PlaybackData
{
    /// <summary>
    /// DateTimeOffset.FromUnixTimeSeconds
    /// Доступно, если тип "stream-up" "stream-down"
    /// </summary>
    [JsonPropertyName("server_time")]
    public long? ServerTime { get; set; }

    /// <summary>
    /// Доступно, если тип "stream-up"
    /// </summary>
    [JsonPropertyName("play_delay")]
    public int? PlayDelay { get; set; }

    /// <summary>
    /// Доступно, если тип "viewcount"
    /// </summary>
    [JsonPropertyName("viewers")]
    public int? Viewers { get; set; }

    /// <summary>
    /// "stream-up", "stream-down", "viewcount"
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }

    public PlaybackData(long? serverTime, int? playDelay, int? viewers, string type)
    {
        ServerTime = serverTime;
        PlayDelay = playDelay;
        Viewers = viewers;
        Type = type;
    }
}
