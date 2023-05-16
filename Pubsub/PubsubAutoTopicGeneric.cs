using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TwitchSimpleLib.Pubsub;

// Я долго думал, как можно было бы сделать десериализацию через женерик тип в топике.
// Самый простой вариант - рефлексия. Но мне это кажется слишком громоздким - каждый раз вызывать при десериализации из топика этот тип.
// Ещё можно было бы в месте регистрации сразу как-то делать и объявление десериализации. Например, разделить получение сообщение и десериализацию, и делать просто подписку при создании аутотопика, и там же класть хендлер. Но это много мороки.
// А ещё можно тупо хранить тип в поле. Ну а че.
// TODO подумать, мб рефлексия это и неплохо.

/// <summary>
/// Хранит ссылку на клиент бота, аккуратно.
/// </summary>
public class PubsubAutoTopic<T> : PubsubAutoTopic
{
    public Action<T>? DataReceived;

    public PubsubAutoTopic(string channelTwitchId, string topic, string? token, TwitchPubsubClient client)
        : base(channelTwitchId, topic, token, client)
    {
    }

    internal void OnDataReceived(T data)
    {
        DataReceived?.Invoke(data);
    }
}
