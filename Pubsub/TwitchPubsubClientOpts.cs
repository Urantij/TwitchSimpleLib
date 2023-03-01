using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TwitchSimpleLib.Irc;

namespace TwitchSimpleLib.Pubsub;

public class TwitchPubsubClientOpts : IBaseClientOpts
{
    public string? OauthToken { get; set; }

    public TimeSpan PingDelay { get; set; } = TimeSpan.FromMinutes(1);
    public TimeSpan PingTimeout { get; set; } = TimeSpan.FromSeconds(10);

    public TimeSpan ConnectionTimeout { get; set; }
    public TimeSpan MinReconnectTime { get; set; }
    public TimeSpan MaxReconnectTime { get; set; }
}
