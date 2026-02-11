using BindSharp.Extensions;
using BindSharp.Test.Helpers;

namespace BindSharp.Test.ResultUtilitiesTest.Try.ErrorFactory;

public sealed class AsyncTryExceptionFirstWithFinallyTests
{
    [Fact]
    public async Task TryAsync_ExceptionFirstWithFinally_WhenSucceeds_ExecutesFinallyBlock()
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
    public async Task TryAsync_ExceptionFirstWithFinally_WhenThrows_ExecutesFinallyBlock()
    {
        // Arrange
        bool finallyExecuted = false;

        // Act
        var result = await Result.TryAsync<int>(
            async () =>
            {
                await Task.Delay(1);
                throw new InvalidOperationException("Test");
            },
            @finally: async () =>
            {
                await Task.Delay(1);
                finallyExecuted = true;
            });

        // Assert
        Assert.True(result.IsFailure);
        Assert.IsType<InvalidOperationException>(result.Error);
        Assert.Equal("Test", result.Error.Message);
        Assert.True(finallyExecuted);
    }

    [Fact]
    public async Task TryAsync_ExceptionFirstWithFinally_PreservesExceptionInError()
    {
        // Arrange
        bool finallyExecuted = false;
        var expectedException = new TimeoutException("Operation timed out");

        // Act
        var result = await Result.TryAsync<int>(
            async () =>
            {
                await Task.Delay(1);
                throw expectedException;
            },
            @finally: async () =>
            {
                await Task.Delay(1);
                finallyExecuted = true;
            });

        // Assert
        Assert.True(result.IsFailure);
        Assert.Same(expectedException, result.Error);
        Assert.True(finallyExecuted);
    }

    [Fact]
    public async Task TryAsync_ExceptionFirstWithFinally_CanCombineWithTapErrorAsync()
    {
        // Arrange
        bool finallyExecuted = false;
        bool tapErrorExecuted = false;
        Exception? caughtException = null;

        // Act
        var result = await Result.TryAsync<int>(
            async () =>
            {
                await Task.Delay(1);
                throw new InvalidOperationException("Test");
            },
            @finally: async () =>
            {
                await Task.Delay(1);
                finallyExecuted = true;
            })
            .TapErrorAsync(async ex =>
            {
                await Task.Delay(1);
                tapErrorExecuted = true;
                caughtException = ex;
            });

        // Assert
        Assert.True(result.IsFailure);
        Assert.True(finallyExecuted);
        Assert.True(tapErrorExecuted);
        Assert.IsType<InvalidOperationException>(caughtException);
    }

    [Fact]
    public async Task TryAsync_ExceptionFirstWithFinally_CanCombineWithMapErrorAsync()
    {
        // Arrange
        bool finallyExecuted = false;

        // Act
        var result = await Result.TryAsync<int>(
            async () =>
            {
                await Task.Delay(1);
                throw new InvalidOperationException("Original");
            },
            @finally: async () =>
            {
                await Task.Delay(1);
                finallyExecuted = true;
            })
            .MapErrorAsync(async ex =>
            {
                await Task.Delay(1);
                return $"Transformed: {ex.Message}";
            });

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Transformed: Original", result.Error);
        Assert.True(finallyExecuted);
    }

    [Fact]
    public async Task TryAsync_ExceptionFirstWithFinally_FinallyExecutesInCorrectOrder()
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
    public async Task TryAsync_ExceptionFirstWithFinally_WithoutFinally_WorksAsExpected()
    {
        // Act
        var result = await Result.TryAsync(async () =>
        {
            await Task.Delay(1);
            return 42;
        });

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task TryAsync_ExceptionFirstWithFinally_RealWorld_AsyncLoggingPipeline()
    {
        // Arrange
        var logger = new AsyncLogger();

        // Act
        var result = await Result.TryAsync<int>(
            async () =>
            {
                await Task.Delay(1);
                throw new FileNotFoundException("file.txt");
            },
            @finally: async () => await logger.LogAttemptAsync())
            .TapErrorAsync(async ex => await logger.LogErrorAsync(ex))
            .MapErrorAsync(async ex =>
            {
                await Task.Delay(1);
                return ex switch
                {
                    FileNotFoundException => "File not found",
                    _ => "Unknown error"
                };
            });

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("File not found", result.Error);
        Assert.Equal(1, logger.LoggedAttempts);
        Assert.True(logger.ErrorLogged);
    }

    [Fact]
    public async Task TryAsync_ExceptionFirstWithFinally_RealWorld_AsyncResourceCleanup()
    {
        // Arrange
        var resource = new AsyncDisposableResource();

        // Act
        var result = await Result.TryAsync(
            async () =>
            {
                await resource.UseAsync();
                return await resource.GetDataAsync();
            },
            @finally: async () => await resource.CleanupAsync());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.True(resource.IsCleanedUp);
    }

    [Fact]
    public async Task TryAsync_ExceptionFirstWithFinally_RealWorld_PatternMatchingWithCleanup()
    {
        // Arrange
        var metrics = new AsyncMetricsTracker();
        var logger = new AsyncLogger();

        // Act
        var result = await Result.TryAsync<int>(
            async () =>
            {
                await Task.Delay(1);
                throw new HttpRequestException("Network error");
            },
            @finally: async () => await metrics.RecordAttemptAsync())
            .TapErrorAsync(async ex =>
            {
                // Pattern match on exception type
                switch (ex)
                {
                    case HttpRequestException http:
                        await logger.LogErrorAsync(http);
                        await metrics.RecordNetworkErrorAsync();
                        break;
                    case TimeoutException timeout:
                        await logger.LogErrorAsync(timeout);
                        await metrics.RecordTimeoutAsync();
                        break;
                    default:
                        await logger.LogErrorAsync(ex);
                        break;
                }
            })
            .MapErrorAsync(ex => ex switch
            {
                HttpRequestException => "Network error occurred",
                TimeoutException => "Operation timed out",
                _ => "An error occurred"
            });

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Network error occurred", result.Error);
        Assert.Equal(1, metrics.Attempts);
        Assert.Equal(1, metrics.NetworkErrors);
        Assert.True(logger.ErrorLogged);
    }
}