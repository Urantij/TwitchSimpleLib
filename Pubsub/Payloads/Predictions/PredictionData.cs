using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TwitchSimpleLib.Pubsub.Payloads.Predictions;

public class PredictionData
{
    public DateTimeOffset Timestamp { get; set; }
    public PredictionEvent Event { get; set; }

    public PredictionData(DateTimeOffset timestamp, PredictionEvent @event)
    {
        this.Timestamp = timestamp;
        this.Event = @event;
    }
}
