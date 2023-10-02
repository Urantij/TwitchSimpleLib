using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

    public PingManager(bool compareText, TimeSpan pingDelay, TimeSpan pingTimeout, object? state = null)
    {
        this.compareText = compareText;
        this.pingDelay = pingDelay;
        this.pingTimeout = pingTimeout;
        State = state;
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
        catch { }
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
            catch { }
        }
    }

    private async Task PingLoopAsync()
    {
        while (!Stopped)
        {
            cts = new CancellationTokenSource();

            try
            {
                await Task.Delay(pingDelay, cts.Token);

                // Если отмена произошла здесь, нет смысла делать проверку на стоп.
                // Потому что мы либо уже получили понг, и ждём кд, либо не попадаем сюда.
            }
            catch { return; }

            expectedText = DateTime.Now.Ticks.ToString();

            Task? task = Pinging?.Invoke(this, expectedText);
            if (task != null)
                await task;

            try
            {
                await Task.Delay(pingTimeout, cts.Token);
            }
            catch { continue; }

            Timeouted?.Invoke(this);
            return;
        }
    }
}
