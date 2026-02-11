namespace BindSharp.Test.Helpers;

public sealed class AsyncDisposableResource
{
    public bool IsCleanedUp { get; private set; }
    private bool _used;

    public async Task UseAsync()
    {
        await Task.Delay(1);
        _used = true;
    }

    public async Task<int> GetDataAsync()
    {
        await Task.Delay(1);
        return _used ? 42 : 0;
    }

    public async Task CleanupAsync()
    {
        await Task.Delay(1);
        IsCleanedUp = true;
    }
}