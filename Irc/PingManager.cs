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

    public bool Stopped { get; private set; }

    private CancellationTokenSource cts = new();
    private string? expectedText;

    private readonly bool compareText;
    private readonly TimeSpan pingDelay;
    private readonly TimeSpan pingTimeout;

    public PingManager(bool compareText, TimeSpan pingDelay, TimeSpan pingTimeout)
    {
        this.compareText = compareText;
        this.pingDelay = pingDelay;
        this.pingTimeout = pingTimeout;
    }

    public void Start()
    {
        Task.Run(PingLoopAsync);
    }

    public void Stop()
    {
        Stopped = true;
        cts.Cancel();
        cts.Dispose();
    }

    public void PongReceived(string text)
    {
        if (compareText && text == expectedText)
        {
            cts.Cancel();
            cts.Dispose();
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
