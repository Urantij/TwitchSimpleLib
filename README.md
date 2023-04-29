# TwitchSimpleLib
Клиенты для чата и пабсаба твича.

### Как

Чат
```cs
TwitchChatClient chatClient = new(true, new TwitchChatClientOpts("Username", "oauth:token"), loggerFactory);
chatClient.AuthFailed += AuthFailed;

var autoChannel = chatClient.AddAutoJoinChannel("urantij");
autoChannel.ChannelJoined += MyChannelJoined;
autoChannel.PrivateMessageReceived += MyPrivateMessageReceived;

await chatClient.ConnectAsync();
```

Пабсаб
```cs
TwitchPubsubClient pubsubClient = new(new TwitchPubsubClientOpts(), loggerFactory);

var topic1 = pubsubClient.AddBroadcastSettingsTopic("100596648");
var topic2 = pubsubClient.AddPlaybackTopic("100596648");
var topic3 = pubsubClient.AddPredictionsTopic("100596648");

topic3.DataReceived += MyPredictionReceived;

await pubsubClient.ConnectAsync();
```

Клиенты будут переподключаться до победного. Исключение - проблема при аутентификации в клиенте чата.

### Зачем

Твичлиб такая громоздкая, что я сдался её использовать и написал свою. Без трёх проектов на один клиент и бесконечных интерфейсов.
