using BindSharp.Test.Helpers;

namespace BindSharp.Test.ResultUtilitiesTest.Try.ErrorFactory;

public sealed class AsyncTryWithErrorFactoryAndFinallyTests
{
    [Fact]
    public async Task TryAsync_WithErrorFactoryAndFinally_WhenSucceeds_ExecutesFinallyBlock()
    {
        // Arrange
        bool finallyExecuted = false;

        // Act
        var result = await Result.TryAsync(
            async () =>
            {
                await Task.Delay(1);
                return 42;
            },
            ex => "error",
            @finally: async () =>
            {
                await Task.Delay(1);
                finallyExecuted = true;
            });

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.True(finallyExecuted);
    }

    [Fact]
    public async Task TryAsync_WithErrorFactoryAndFinally_WhenThrows_ExecutesFinallyBlock()
    {
        // Arrange
        bool finallyExecuted = false;

        // Act
        var result = await Result.TryAsync<int, string>(
            async () =>
            {
                await Task.Delay(1);
                throw new InvalidOperationException("Test");
            },
            ex => $"Error: {ex.Message}",
            @finally: async () =>
            {
                await Task.Delay(1);
                finallyExecuted = true;
            });

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Error: Test", result.Error);
        Assert.True(finallyExecuted);
    }

    [Fact]
    public async Task TryAsync_WithErrorFactoryAndFinally_FinallyExecutesInCorrectOrder()
    {
        // Arrange
        var executionLog = new System.Collections.Generic.List<string>();

        // Act
        var result = await Result.TryAsync(
            async () =>
            {
                await Task.Delay(1);
                executionLog.Add("operation");
                return 42;
            },
            ex => "error",
            @finally: async () =>
            {
                await Task.Delay(1);
                executionLog.Add("finally");
            });

        executionLog.Add("after-return");

        // Assert
        Assert.Equal(new[] { "operation", "finally", "after-return" }, executionLog);
    }

    [Fact]
    public async Task TryAsync_WithErrorFactoryAndFinally_FinallyCanAccessCapturedVariables()
    {
        // Arrange
        int counter = 0;

        // Act
        var result = await Result.TryAsync(
            async () =>
            {
                await Task.Delay(1);
                return 42;
            },
            ex => "error",
            @finally: async () =>
            {
                await Task.Delay(1);
                counter++;
            });

        // Assert
        Assert.Equal(1, counter);
    }

    [Fact]
    public async Task TryAsync_WithErrorFactoryAndFinally_WithoutFinally_WorksAsExpected()
    {
        // Act
        var result = await Result.TryAsync(
            async () =>
            {
                await Task.Delay(1);
                return 42;
            },
            ex => "error");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task TryAsync_WithErrorFactoryAndFinally_RealWorld_DatabaseConnectionCleanup()
    {
        // Arrange
        var connection = new AsyncDatabaseConnection();

        // Act
        var result = await Result.TryAsync(
            async () =>
            {
                await connection.OpenAsync();
                return await connection.QueryAsync();
            },
            ex => $"Query failed: {ex.Message}",
            @finally: async () => await connection.CloseAsync());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.True(connection.IsClosed);
    }

    [Fact]
    public async Task TryAsync_WithErrorFactoryAndFinally_RealWorld_ConnectionCleanupOnException()
    {
        // Arrange
        var connection = new AsyncDatabaseConnection();

        // Act
        var result = await Result.TryAsync<int, string>(
            async () =>
            {
                await connection.OpenAsync();
                throw new TimeoutException("Query timeout");
            },
            ex => $"Error: {ex.Message}",
            @finally: async () => await connection.CloseAsync());

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Error: Query timeout", result.Error);
        Assert.True(connection.IsClosed, "Connection should be closed even on exception");
    }

    [Fact]
    public async Task TryAsync_WithErrorFactoryAndFinally_RealWorld_AsyncMetrics()
    {
        // Arrange
        var metrics = new AsyncMetricsTracker();

        // Act - Success
        var result1 = await Result.TryAsync(
            async () =>
            {
                await Task.Delay(10);
                return 42;
            },
            ex => "error",
            @finally: async () => await metrics.RecordAttemptAsync());

        // Act - Failure
        var result2 = await Result.TryAsync<int, string>(
            async () =>
            {
                await Task.Delay(10);
                throw new Exception("Failed");
            },
            ex => "error",
            @finally: async () => await metrics.RecordAttemptAsync());

        // Assert
        Assert.Equal(2, metrics.Attempts);
    }

    [Fact]
    public async Task TryAsync_WithErrorFactoryAndFinally_RealWorld_AsyncLockRelease()
    {
        // Arrange
        var asyncLock = new AsyncLock();

        // Act
        var result = await Result.TryAsync(
            async () =>
            {
                await asyncLock.AcquireAsync();
                await Task.Delay(10);
                return 42;
            },
            ex => $"Error: {ex.Message}",
            @finally: async () => await asyncLock.ReleaseAsync());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(asyncLock.IsLocked);
    }
}