namespace BindSharp.Test.Helpers;

public sealed class ExecutionTracker
{
    public bool OperationExecuted { get; set; }
    public bool CleanupExecuted { get; set; }
}