using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TwitchSimpleLib.Pubsub.Messages;

public class PubsubListenMessage : BasePubsubMessage
{
    public class ListenData
    {
        [JsonPropertyName("topics")]
        public IEnumerable<string> Topics { get; set; }
        [JsonPropertyName("auth_token")]
        public string? AuthToken { get; set; }

        public ListenData(IEnumerable<string> topics, string? authToken)
        {
            Topics = topics;
            AuthToken = authToken;
        }
    }

    public PubsubListenMessage(ListenData data, string? nonce)
        : base("LISTEN", data, nonce)
    {
    }
}
