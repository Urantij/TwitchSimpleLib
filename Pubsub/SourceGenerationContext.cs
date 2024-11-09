using System.Text.Json.Serialization;
using TwitchSimpleLib.Pubsub.Messages;
using TwitchSimpleLib.Pubsub.Payloads.BroadcastSettings;
using TwitchSimpleLib.Pubsub.Payloads.Playback;
using TwitchSimpleLib.Pubsub.Payloads.Predictions;

namespace TwitchSimpleLib.Pubsub;

[JsonSerializable(typeof(BasePubsubMessage))]
[JsonSerializable(typeof(PubsubListenMessage))]
[JsonSerializable(typeof(PubsubMessage))]
[JsonSerializable(typeof(PubsubPingMessage))]
[JsonSerializable(typeof(PubsubResponseMessage))]
[JsonSerializable(typeof(PubsubUnlistenMessage))]
[JsonSerializable(typeof(BroadcastSettingsData))]
[JsonSerializable(typeof(PlaybackData))]
[JsonSerializable(typeof(PredictionData))]
public partial class SourceGenerationContext : JsonSerializerContext
{
}