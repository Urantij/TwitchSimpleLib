using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TwitchSimpleLib.Pubsub.Messages;

public class PubsubMessage
{
    public class MessageData
    {
        public string Topic { get; set; }
        public string Message { get; set; }

        public MessageData(string topic, string message)
        {
            Topic = topic;
            Message = message;
        }
    }

    public MessageData Data { get; set; }

    public PubsubMessage(MessageData data)
    {
        this.Data = data;
    }
}
