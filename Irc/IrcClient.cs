using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IrcParserNet.Irc;
using Microsoft.Extensions.Logging;

namespace TwitchSimpleLib.Irc;

public class IrcClient
{
    public bool IsConnected => connection?.Connected == true;

    public bool Closed { get; private set; }

    public event Action<RawIrcMessage>? RawIrcMessageReceived;

    protected readonly ReconnectionTime reconnectionTime;

    protected ILoggerFactory? _loggerFactory;
    protected ILogger? _logger;

    private readonly Uri uri;
    private readonly TimeSpan connectionTimeout;
    protected IrcConnection? connection;

    protected IrcClient(Uri uri, IIrcClientOpts opts, ILoggerFactory? loggerFactory)
    {
        this.uri = uri;
        this.connectionTimeout = opts.ConnectionTimeout;
        this._loggerFactory = loggerFactory;
        this._logger = loggerFactory?.CreateLogger(this.GetType());

        this.reconnectionTime = new ReconnectionTime(opts.MinReconnectTime, opts.MaxReconnectTime);
    }

    public async Task ConnectAsync()
    {
        IrcConnection caller = connection = new IrcConnection(uri, _loggerFactory);
        caller.MessageReceived += MessageReceived;
        caller.Disposing += ConnectionDisposing;

        if (await caller.StartAsync(connectionTimeout))
        {
            reconnectionTime.Connected();

            await ConnectedAsync(caller);
        }
    }

    public Task SendRawAsync(string content)
        => SendRawAsync(connection, content);

    public static Task SendRawAsync(IrcConnection? connection, string content)
    {
        if (connection != null)
            return connection.SendAsync(content);

        return Task.CompletedTask;
    }

    public void Close()
    {
        Closed = true;
        connection?.Dispose();
    }

    protected virtual Task ConnectedAsync(IrcConnection connection)
    {
        return Task.CompletedTask;
    }

    protected virtual void MessageReceived(object? sender, RawIrcMessage e)
    {
        RawIrcMessageReceived?.Invoke(e);
    }

    protected virtual void ConnectionDisposing(object? sender, Exception? e)
    {
        if (Closed)
            return;

        _logger?.LogDebug(e, "Disconnected");

        Task.Run(async () =>
        {
            TimeSpan waitTime = reconnectionTime.DoAttempt();

            await Task.Delay(waitTime);

            await ConnectAsync();
        });
    }
}
