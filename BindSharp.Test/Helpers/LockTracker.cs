namespace BindSharp.Test.Helpers;

public sealed class LockTracker
{
    public bool IsLocked { get; private set; }

    public void AcquireLock() => IsLocked = true;
    public void ReleaseLock() => IsLocked = false;
}