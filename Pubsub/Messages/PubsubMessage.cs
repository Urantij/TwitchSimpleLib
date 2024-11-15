using System.Text.Json.Serialization;

namespace TwitchSimpleLib.Pubsub.Messages;

public class PubsubMessage
{
    public class MessageData
    {
        [JsonPropertyName("topic")] public string Topic { get; set; }
        [JsonPropertyName("message")] public string Message { get; set; }

        public MessageData(string topic, string message)
        {
            Topic = topic;
            Message = message;
        }
    }

    [JsonPropertyName("data")] public MessageData Data { get; set; }

    public PubsubMessage(MessageData data)
    {
        this.Data = data;
    }
}