using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TwitchSimpleLib.Irc;

public interface IBaseClientOpts
{
    public TimeSpan ConnectionTimeout { get; }
    public TimeSpan MinReconnectTime { get; }
    public TimeSpan MaxReconnectTime { get; }
}
