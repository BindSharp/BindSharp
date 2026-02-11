namespace BindSharp.Test.Helpers;

public sealed class DisposableResource
{
    public bool IsCleanedUp { get; private set; }
    private bool _used;

    public void Use() => _used = true;
    public int GetData() => _used ? 42 : 0;
    public void Cleanup() => IsCleanedUp = true;
}