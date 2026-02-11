namespace BindSharp.Test.Helpers;

public sealed class StateManager
{
    public bool IsProcessing { get; set; }

    public int Process() => 42;
}