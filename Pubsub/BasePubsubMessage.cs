using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TwitchSimpleLib.Pubsub;

public abstract class BasePubsubMessage
{
    public string Type { get; set; }
    public string? Nonce { get; set; }
    public object? Data { get; set; }

    protected BasePubsubMessage(string type, object? data, string? nonce)
    {
        Type = type;
        Data = data;
        Nonce = nonce;
    }
}
