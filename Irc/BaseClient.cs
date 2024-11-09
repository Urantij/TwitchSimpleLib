using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace TwitchSimpleLib.Irc;

public class BaseClient
{
    public bool IsConnected => _connection?.Connected == true;

    public bool Closed { get; private set; }

    public event Action? Connected;
    public event Action<Exception?>? ConnectionClosed;
    public event Action<(Exception exception, string message)>? MessageProcessingException;

    protected readonly ReconnectionTime _reconnectionTime;

    protected ILoggerFactory? _loggerFactory;
    protected ILogger? _logger;
    protected readonly CancellationToken _cancellationToken;

    private readonly Uri _uri;
    private readonly TimeSpan _connectionTimeout;
    protected WsConnection? _connection;

    protected BaseClient(Uri uri, IBaseClientOpts opts, ILoggerFactory? loggerFactory,
        CancellationToken cancellationToken = default)
    {
        this._uri = uri;
        this._connectionTimeout = opts.ConnectionTimeout;
        this._loggerFactory = loggerFactory;
        this._logger = loggerFactory?.CreateLogger(this.GetType());
        this._cancellationToken = cancellationToken;

        this._reconnectionTime = new ReconnectionTime(opts.MinReconnectTime, opts.MaxReconnectTime);
    }

    public async Task ConnectAsync()
    {
        Closed = false;

        WsConnection caller = _connection = new WsConnection(_uri, _loggerFactory, _cancellationToken);
        caller.MessageReceived += OnMessageReceived;
        caller.Disposing += ConnectionDisposing;

        if (await caller.StartAsync(_connectionTimeout))
        {
            _reconnectionTime.Connected();

            await ConnectedAsync(caller);

            Connected?.Invoke();
        }
    }

    public Task SendRawAsync(string content)
        => SendRawAsync(_connection, content);

    public static Task SendRawAsync(WsConnection? connection, string content)
    {
        if (connection != null)
            return connection.SendAsync(content);

        return Task.CompletedTask;
    }

    public void Close()
    {
        Closed = true;
        _connection?.Dispose();
    }

    protected virtual Task ConnectedAsync(WsConnection connection)
    {
        return Task.CompletedTask;
    }

    private void OnMessageReceived(object? sender, string e)
    {
        try
        {
            MessageReceived(sender, e);
        }
        catch (Exception ex)
        {
            MessageProcessingException?.Invoke((ex, e));
        }
    }

    protected virtual void MessageReceived(object? sender, string e)
    {
    }

    protected virtual void ConnectionDisposing(object? sender, Exception? e)
    {
        if (!Closed)
        {
            Task.Run(async () =>
            {
                TimeSpan waitTime = _reconnectionTime.DoAttempt();

                waitTime += TimeSpan.FromMilliseconds(RandomNumberGenerator.GetInt32(50, 750));

                try
                {
                    await Task.Delay(waitTime, _cancellationToken);
                }
                catch
                {
                    return;
                }

                await ConnectAsync();
            });
        }

        ConnectionClosed?.Invoke(e);
    }
}