namespace BindSharp.Test.Helpers;

public sealed class MetricsTracker
{
    public int Attempts { get; private set; }

    public void RecordAttempt() => Attempts++;
}