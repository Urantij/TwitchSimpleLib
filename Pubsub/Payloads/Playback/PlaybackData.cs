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
    /// </summary>
    [JsonPropertyName("server_time")]
    public long ServerTime { get; set; }

    [JsonPropertyName("play_delay")]
    public int? PlayDelay { get; set; }

    /// <summary>
    /// "stream-up" "stream-down"
    /// </summary>
    public string Type { get; set; }

    public PlaybackData(long serverTime, int? playDelay, string type)
    {
        ServerTime = serverTime;
        PlayDelay = playDelay;
        Type = type;
    }
}
