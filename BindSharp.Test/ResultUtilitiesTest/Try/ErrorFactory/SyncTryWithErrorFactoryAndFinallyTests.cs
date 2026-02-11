using BindSharp.Test.Helpers;

namespace BindSharp.Test.ResultUtilitiesTest.Try.ErrorFactory;

public sealed class SyncTryWithErrorFactoryAndFinallyTests
{
    [Fact]
    public void Try_WithErrorFactoryAndFinally_WhenSucceeds_ExecutesFinallyBlock()
    {
        // Arrange
        bool finallyExecuted = false;

        // Act
        var result = Result.Try(
            () => 42,
            ex => "error",
            @finally: () => finallyExecuted = true);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.True(finallyExecuted, "Finally block should execute on success");
    }

    [Fact]
    public void Try_WithErrorFactoryAndFinally_WhenThrows_ExecutesFinallyBlock()
    {
        // Arrange
        bool finallyExecuted = false;

        // Act
        var result = Result.Try<int, string>(
            () => throw new InvalidOperationException("Test"),
            ex => $"Error: {ex.Message}",
            @finally: () => finallyExecuted = true);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Error: Test", result.Error);
        Assert.True(finallyExecuted, "Finally block should execute on failure");
    }

    [Fact]
    public void Try_WithErrorFactoryAndFinally_FinallyExecutesInCorrectOrder()
    {
        // Arrange
        var executionLog = new System.Collections.Generic.List<string>();

        // Act
        var result = Result.Try(
            () =>
            {
                executionLog.Add("operation");
                return 42;
            },
            ex => "error",
            @finally: () => executionLog.Add("finally"));

        executionLog.Add("after-return");

        // Assert
        Assert.Equal(new[] { "operation", "finally", "after-return" }, executionLog);
    }

    [Fact]
    public void Try_WithErrorFactoryAndFinally_FinallyExecutesBeforeReturningResult()
    {
        // Arrange
        int executionOrder = 0;
        int finallyOrder = 0;
        int resultOrder = 0;

        // Act
        var result = Result.Try(
            () =>
            {
                executionOrder = 1;
                return 42;
            },
            ex => "error",
            @finally: () => finallyOrder = 2);

        resultOrder = 3;

        // Assert
        Assert.Equal(1, executionOrder);
        Assert.Equal(2, finallyOrder);
        Assert.Equal(3, resultOrder);
    }

    [Fact]
    public void Try_WithErrorFactoryAndFinally_FinallyCanAccessCapturedVariables()
    {
        // Arrange
        int counter = 0;

        // Act
        var result = Result.Try(
            () => 42,
            ex => "error",
            @finally: () => counter++);

        // Assert
        Assert.Equal(1, counter);
    }

    [Fact]
    public void Try_WithErrorFactoryAndFinally_FinallyCanModifySharedState()
    {
        // Arrange
        var tracker = new ExecutionTracker();

        // Act
        var result = Result.Try(
            () =>
            {
                tracker.OperationExecuted = true;
                return 42;
            },
            ex => "error",
            @finally: () => tracker.CleanupExecuted = true);

        // Assert
        Assert.True(tracker.OperationExecuted);
        Assert.True(tracker.CleanupExecuted);
    }

    [Fact]
    public void Try_WithErrorFactoryAndFinally_FinallyExecutesEvenWhenExceptionThrown()
    {
        // Arrange
        bool finallyExecuted = false;
        var exception = new InvalidOperationException("Operation failed");

        // Act
        var result = Result.Try<int, string>(
            () => throw exception,
            ex => "Handled error",
            @finally: () => finallyExecuted = true);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Handled error", result.Error);
        Assert.True(finallyExecuted);
    }

    [Fact]
    public void Try_WithErrorFactoryAndFinally_WithoutFinally_WorksAsExpected()
    {
        // Act
        var result = Result.Try(
            () => 42,
            ex => "error");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Try_WithErrorFactoryAndFinally_RealWorld_LockRelease()
    {
        // Arrange
        var lockTracker = new LockTracker();

        // Act
        var result = Result.Try(
            () =>
            {
                lockTracker.AcquireLock();
                return ProcessWithLock(lockTracker);
            },
            ex => $"Error: {ex.Message}",
            @finally: () => lockTracker.ReleaseLock());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.False(lockTracker.IsLocked);
    }

    [Fact]
    public void Try_WithErrorFactoryAndFinally_RealWorld_LockReleaseOnException()
    {
        // Arrange
        var lockTracker = new LockTracker();

        // Act
        var result = Result.Try<int, string>(
            () =>
            {
                lockTracker.AcquireLock();
                throw new InvalidOperationException("Failed");
            },
            ex => $"Error: {ex.Message}",
            @finally: () => lockTracker.ReleaseLock());

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Error: Failed", result.Error);
        Assert.False(lockTracker.IsLocked, "Lock should be released even on exception");
    }

    [Fact]
    public void Try_WithErrorFactoryAndFinally_RealWorld_MetricsRecording()
    {
        // Arrange
        var metrics = new MetricsTracker();

        // Act - Success case
        var result1 = Result.Try(
            () => 42,
            ex => "error",
            @finally: () => metrics.RecordAttempt());

        // Act - Failure case
        var result2 = Result.Try<int, string>(
            () => throw new Exception("Failed"),
            ex => "error",
            @finally: () => metrics.RecordAttempt());

        // Assert
        Assert.Equal(2, metrics.Attempts);
    }

    [Fact]
    public void Try_WithErrorFactoryAndFinally_RealWorld_StateRestoration()
    {
        // Arrange
        var stateManager = new StateManager();

        // Act
        var result = Result.Try(
            () =>
            {
                stateManager.IsProcessing = true;
                return stateManager.Process();
            },
            ex => $"Processing failed: {ex.Message}",
            @finally: () => stateManager.IsProcessing = false);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(stateManager.IsProcessing, "State should be restored");
    }

    private static int ProcessWithLock(LockTracker tracker)
    {
        return tracker.IsLocked ? 42 : throw new InvalidOperationException("No lock");
    }
}