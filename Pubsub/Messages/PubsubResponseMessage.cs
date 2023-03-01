using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TwitchSimpleLib.Pubsub.Messages;

public class PubsubResponseMessage
{
    public string? Nonce { get; private set; }
    public string Error { get; private set; }

    public PubsubResponseMessage(string? nonce, string error)
    {
        this.Nonce = nonce;
        this.Error = error;
    }
}
