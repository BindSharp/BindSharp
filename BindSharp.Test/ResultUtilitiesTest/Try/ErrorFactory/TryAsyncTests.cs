namespace BindSharp.Test.ResultUtilitiesTest.Try.ErrorFactory;

public sealed class TryAsyncTests
{
    [Fact]
    public async Task TryAsync_WithErrorFactory_WhenOperationSucceeds_ReturnsSuccess()
    {
        // Arrange
        var expected = "success";

        // Act
        var result = await Result.TryAsync(
            async () =>
            {
                await Task.Delay(1);
                return expected;
            },
            ex => $"Error: {ex.Message}");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public async Task TryAsync_WithErrorFactory_WhenOperationThrows_ReturnsFailureWithTransformedError()
    {
        // Arrange
        var exception = new InvalidOperationException("Async error");

        // Act
        var result = await Result.TryAsync<bool, string>(
            async () =>
            {
                await Task.Delay(1);
                throw exception;
            },
            ex => $"Custom error: {ex.Message}");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Custom error: Async error", result.Error);
    }

    [Fact]
    public async Task TryAsync_WithErrorFactory_PassesCorrectExceptionToFactory()
    {
        // Arrange
        var expectedException = new TimeoutException("Operation timed out");
        Exception? capturedEx = null;

        // Act
        var result = await Result.TryAsync<bool, string>(
            async () =>
            {
                await Task.Delay(1);
                throw expectedException;
            },
            ex =>
            {
                capturedEx = ex;
                return "timeout";
            });

        // Assert
        Assert.Same(expectedException, capturedEx);
        Assert.IsType<TimeoutException>(capturedEx);
    }

    [Fact]
    public async Task TryAsync_WithErrorFactory_CanTransformToDifferentErrorType()
    {
        // Act
        var result = await Result.TryAsync(
            async () =>
            {
                await Task.Delay(1);
                return int.Parse("invalid");
            },
            ex => new { ErrorType = ex.GetType().Name, Timestamp = DateTime.UtcNow });

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("FormatException", result.Error.ErrorType);
    }

    [Fact]
    public async Task TryAsync_WithErrorFactory_WhenSuccessful_DoesNotCallErrorFactory()
    {
        // Arrange
        bool errorFactoryCalled = false;

        // Act
        var result = await Result.TryAsync(
            async () =>
            {
                await Task.Delay(1);
                return 42;
            },
            ex =>
            {
                errorFactoryCalled = true;
                return "error";
            });

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(errorFactoryCalled);
    }

    [Fact]
    public async Task TryAsync_WithErrorFactory_CanPatternMatchOnExceptionType()
    {
        // Act
        var result1 = await Result.TryAsync<bool, string>(
            async () =>
            {
                await Task.Delay(1);
                throw new TaskCanceledException("Task was cancelled");
            },
            ex => ex switch
            {
                TaskCanceledException => "CANCELLED",
                TimeoutException => "TIMEOUT",
                HttpRequestException => "HTTP_ERROR",
                _ => "UNKNOWN"
            });

        var result2 = await Result.TryAsync<bool, string>(
            async () =>
            {
                await Task.Delay(1);
                throw new TimeoutException("Timed out");
            },
            ex => ex switch
            {
                TaskCanceledException => "CANCELLED",
                TimeoutException => "TIMEOUT",
                HttpRequestException => "HTTP_ERROR",
                _ => "UNKNOWN"
            });

        // Assert
        Assert.Equal("CANCELLED", result1.Error);
        Assert.Equal("TIMEOUT", result2.Error);
    }

    [Fact]
    public async Task TryAsync_WithErrorFactory_CanAccessExceptionProperties()
    {
        // Act
        var result = await Result.TryAsync<bool, string>(
            async () =>
            {
                await Task.Delay(1);
                throw new ArgumentException("Invalid value", "userId");
            },
            ex =>
            {
                if (ex is ArgumentException argEx)
                    return $"Validation error on '{argEx.ParamName}': {argEx.Message}";
                return ex.Message;
            });

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("userId", result.Error);
        Assert.Contains("Invalid value", result.Error);
    }

    [Fact]
    public async Task TryAsync_WithErrorFactory_HandlesExceptionsThrownBeforeAwait()
    {
        // Arrange & Act
        var result = await Result.TryAsync(
            () =>
            {
                // Exception thrown synchronously before any await
                throw new InvalidOperationException("Sync exception in async method");
#pragma warning disable CS0162 // Unreachable code detected
                return Task.FromResult(42);
#pragma warning restore CS0162
            },
            ex => $"Caught: {ex.Message}");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Caught: Sync exception in async method", result.Error);
    }
}