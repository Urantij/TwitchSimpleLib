namespace TwitchSimpleLib.Pubsub.Messages;

public class PubsubPingMessage : BasePubsubMessage
{
    public PubsubPingMessage()
        : base("PING", null, null)
    {
    }
}