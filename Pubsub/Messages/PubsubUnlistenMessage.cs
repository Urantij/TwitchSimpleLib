using System.Text.Json.Serialization;

namespace TwitchSimpleLib.Pubsub.Messages;

public class PubsubUnlistenMessage : BasePubsubMessage
{
    public class ListenData
    {
        [JsonPropertyName("topics")] public IEnumerable<string> Topics { get; set; }

        public ListenData(IEnumerable<string> topics)
        {
            Topics = topics;
        }
    }

    public PubsubUnlistenMessage(ListenData data, string? nonce)
        : base("UNLISTEN", data, nonce)
    {
    }
}