namespace TwitchSimpleLib.Irc;

/// <summary>
/// Следит за процессом пропинговки сервера.
/// </summary>
public class PingManager
{
    public event Func<PingManager, string, Task>? Pinging;
    public event Action<PingManager>? Timeouted;

    public object? State { get; init; }

    public bool Stopped { get; private set; }

    private CancellationTokenSource cts = new();
    private string? expectedText;

    private readonly bool compareText;
    private readonly TimeSpan pingDelay;
    private readonly TimeSpan pingTimeout;
    private readonly CancellationToken cancellationToken;

    public PingManager(bool compareText, TimeSpan pingDelay, TimeSpan pingTimeout, object? state = null,
        CancellationToken cancellationToken = default)
    {
        this.compareText = compareText;
        this.pingDelay = pingDelay;
        this.pingTimeout = pingTimeout;
        State = state;
        this.cancellationToken = cancellationToken;
    }

    public void Start()
    {
        Task.Run(PingLoopAsync);
    }

    public void Stop()
    {
        Stopped = true;

        var thisCts = cts;
        try
        {
            thisCts.Cancel();
            thisCts.Dispose();
        }
        catch
        {
        }
    }

    public void PongReceived(string text)
    {
        if (!compareText || text == expectedText)
        {
            var thisCts = cts;
            try
            {
                thisCts.Cancel();
                thisCts.Dispose();
            }
            catch
            {
            }
        }
    }

    private async Task PingLoopAsync()
    {
        while (!Stopped)
        {
            cts = new CancellationTokenSource();

            using var resultCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);

            try
            {
                await Task.Delay(pingDelay, resultCts.Token);

                // Если отмена произошла здесь, нет смысла делать проверку на стоп.
                // Потому что мы либо уже получили понг, и ждём кд, либо не попадаем сюда.
            }
            catch
            {
                return;
            }

            expectedText = DateTime.Now.Ticks.ToString();

            Task? task = Pinging?.Invoke(this, expectedText);
            if (task != null)
                await task;

            try
            {
                await Task.Delay(pingTimeout, resultCts.Token);
            }
            catch
            {
                continue;
            }

            Timeouted?.Invoke(this);
            return;
        }
    }
}