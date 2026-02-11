namespace BindSharp.Test.Helpers;

public sealed class AsyncMetricsTracker
{
    public int Attempts { get; private set; }
    public int NetworkErrors { get; private set; }
    public int Timeouts { get; private set; }

    public async Task RecordAttemptAsync()
    {
        await Task.Delay(1);
        Attempts++;
    }

    public async Task RecordNetworkErrorAsync()
    {
        await Task.Delay(1);
        NetworkErrors++;
    }

    public async Task RecordTimeoutAsync()
    {
        await Task.Delay(1);
        Timeouts++;
    }
}