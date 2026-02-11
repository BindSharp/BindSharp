namespace BindSharp.Test.Helpers;

public sealed class AsyncLogger
{
    public int LoggedAttempts { get; private set; }
    public bool ErrorLogged { get; private set; }

    public async Task LogAttemptAsync()
    {
        await Task.Delay(1);
        LoggedAttempts++;
    }

    public async Task LogErrorAsync(Exception ex)
    {
        await Task.Delay(1);
        ErrorLogged = true;
    }
}