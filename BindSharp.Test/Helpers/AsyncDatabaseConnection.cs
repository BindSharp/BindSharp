namespace BindSharp.Test.Helpers;

public sealed class AsyncDatabaseConnection
{
    public bool IsClosed { get; private set; }
    private bool _isOpen;

    public async Task OpenAsync()
    {
        await Task.Delay(1);
        _isOpen = true;
    }

    public async Task<int> QueryAsync()
    {
        await Task.Delay(1);
        return _isOpen ? 42 : throw new InvalidOperationException("Connection not open");
    }

    public async Task CloseAsync()
    {
        await Task.Delay(1);
        IsClosed = true;
        _isOpen = false;
    }
}