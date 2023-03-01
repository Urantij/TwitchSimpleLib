using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TwitchSimpleLib.Pubsub.Messages;

public class PubsubPingMessage : BasePubsubMessage
{
    public PubsubPingMessage()
        : base("PING", null, null)
    {
    }
}
