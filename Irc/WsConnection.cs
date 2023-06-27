using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using IrcParserNet.Irc;
using Microsoft.Extensions.Logging;

namespace TwitchSimpleLib.Irc;

public class WsConnection
{
    private const int bufferSizeStep = 512;

    private bool disposed = false;
    private bool started = false;

    private readonly SemaphoreSlim writeLocker = new(1, 1);

    private readonly Uri uri;
    private readonly ILogger? _logger;
    private readonly CancellationToken cancellationToken;

    private readonly ClientWebSocket client;

    public bool Connected => client?.State == WebSocketState.Open;

    public event EventHandler<string>? MessageReceived;
    public event EventHandler<Exception?>? Disposing;

    public WsConnection(Uri uri, ILoggerFactory? loggerFactory = null, CancellationToken cancellationToken = default)
    {
        this.uri = uri;
        this._logger = loggerFactory?.CreateLogger(this.GetType());
        this.cancellationToken = cancellationToken;

        client = new ClientWebSocket();
    }

    /// <summary>
    /// Кидает ошибки, если уже запущен, или уже диспозед.
    /// Короче больше одного раза просто не стартуй.
    /// </summary>
    /// <param name="connectionTimeout"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task<bool> StartAsync(TimeSpan connectionTimeout)
    {
        if (started)
        {
            throw new Exception("Конекшн уже работает");
        }
        if (disposed)
        {
            throw new Exception("Конекшн уже диспоузед");
        }

        started = true;

        using var timeoutCts = new CancellationTokenSource(connectionTimeout);
        using var resultCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

        try
        {
            await client.ConnectAsync(uri, resultCts.Token);

            _ = ListenAsync();

            return true;
        }
        catch (Exception e)
        {
            Dispose(e);

            return false;
        }
    }

    public void Send(string message)
    {
        if (!Connected)
            return;

        try
        {
            writeLocker.Wait(cancellationToken);

            byte[] bytes = Encoding.UTF8.GetBytes(message);
            client.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken).GetAwaiter().GetResult();
            writeLocker.Release();
        }
        catch (Exception e)
        {
            Dispose(e);
        }
    }

    public async Task SendAsync(string message)
    {
        if (!Connected)
            return;

        if (_logger?.IsEnabled(LogLevel.Trace) == true)
        {
            _logger.LogTrace($"{nameof(SendAsync)} {{content}}", message);
        }

        try
        {
            // Оптимизация НЕВЕРОЯТНАЯ.
            Task waitTask = writeLocker.WaitAsync(cancellationToken);

            byte[] bytes = Encoding.UTF8.GetBytes(message);
            await waitTask;
            await client.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);
            writeLocker.Release();
        }
        catch (Exception e)
        {
            Dispose(e);
        }
    }

    private async Task ListenAsync()
    {
        try
        {
            byte[] buffer = new byte[bufferSizeStep];
            int spaceLeft = bufferSizeStep;
            int currentCount = 0;

            while (Connected)
            {
                if (spaceLeft == 0)
                {
                    Array.Resize(ref buffer, buffer.Length + bufferSizeStep);
                    spaceLeft += bufferSizeStep;
                }
                ArraySegment<byte> segment = new(buffer, currentCount, spaceLeft);

                WebSocketReceiveResult result = await client.ReceiveAsync(segment, cancellationToken);
                currentCount += result.Count;
                spaceLeft -= result.Count;

                switch (result.MessageType)
                {
                    case WebSocketMessageType.Close:

                        Dispose(new Exception($"{nameof(WebSocketMessageType)}.{nameof(WebSocketMessageType.Close)}"));
                        return;

                    case WebSocketMessageType.Text when result.EndOfMessage:

                        string messagesString = Encoding.UTF8.GetString(buffer, 0, currentCount);

                        string[] messages = messagesString.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
                        foreach (string singleMessage in messages)
                        {
                            OnMessageReceived(singleMessage);
                        }
                        currentCount = 0;
                        spaceLeft = buffer.Length;
                        break;
                    case WebSocketMessageType.Text:
                        break;
                    case WebSocketMessageType.Binary:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(result.MessageType.ToString());
                }
            }
        }
        catch (Exception e)
        {
            Dispose(e);
        }
    }

    private void OnMessageReceived(string message)
    {
        try
        {
            MessageReceived?.Invoke(this, message);
        }
        catch (Exception e)
        {
            _logger?.LogCritical(e, $"{nameof(WsConnection)}.{nameof(OnMessageReceived)}");
        }
    }

    public void Dispose(Exception? e = null)
    {
        if (!disposed)
        {
            disposed = true;

            client?.Abort();
            client?.Dispose();
            writeLocker.Dispose();

            try
            {
                Disposing?.Invoke(this, e);
            }
            catch (Exception invokedException)
            {
                _logger?.LogCritical(invokedException, $"{nameof(WsConnection)}.{nameof(Disposing)}");
            }
        }
    }
}
