using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TwitchSimpleLib.Irc;

public class ReconnectionTime
{
    int attempts = 0;

    readonly TimeSpan minTime;
    readonly TimeSpan maxTime;

    public ReconnectionTime(TimeSpan minTime, TimeSpan maxTime)
    {
        this.minTime = minTime;
        this.maxTime = maxTime;
    }

    public void Connected()
    {
        attempts = 0;
    }

    public TimeSpan DoAttempt()
    {
        // С каждой попыткой время x2
        TimeSpan time = minTime * Math.Pow(2, attempts);

        attempts++;

        if (time < maxTime)
            return time;

        return maxTime;
    }
}
