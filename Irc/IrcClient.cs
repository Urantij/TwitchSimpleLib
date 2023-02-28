using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IrcParserNet.Irc;
using Microsoft.Extensions.Logging;

namespace TwitchSimpleLib.Irc;

public class IrcClient : BaseClient
{
    public event Action<RawIrcMessage>? RawIrcMessageReceived;

    protected IrcClient(Uri uri, IBaseClientOpts opts, ILoggerFactory? loggerFactory)
        : base(uri, opts, loggerFactory)
    {
    }

    protected override void MessageReceived(object? sender, string e)
    {
        base.MessageReceived(sender, e);

        WsConnection connection = (WsConnection)sender!;

        RawIrcMessage ircMessage;
        try
        {
            ircMessage = IrcParser.Parse(e);
        }
        catch (Exception ex)
        {
            _logger?.LogCritical(ex, $"{nameof(IrcParser)}.{nameof(IrcParser.Parse)}");
            return;
        }

        IrcMessageReceived(connection, ircMessage);
    }

    protected virtual void IrcMessageReceived(WsConnection connection, RawIrcMessage message)
    {
        RawIrcMessageReceived?.Invoke(message);
    }
}
