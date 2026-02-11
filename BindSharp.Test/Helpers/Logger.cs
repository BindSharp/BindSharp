namespace BindSharp.Test.Helpers;

public sealed class Logger
{
    public int Attempts { get; private set; }
    public bool ErrorLogged { get; private set; }

    public void RecordAttempt() => Attempts++;
    public void LogError(Exception ex) => ErrorLogged = true;
}