namespace BindSharp.Test.Helpers;

public sealed class AsyncLock
{
    public bool IsLocked { get; private set; }

    public async Task AcquireAsync()
    {
        await Task.Delay(1);
        IsLocked = true;
    }

    public async Task ReleaseAsync()
    {
        await Task.Delay(1);
        IsLocked = false;
    }
}