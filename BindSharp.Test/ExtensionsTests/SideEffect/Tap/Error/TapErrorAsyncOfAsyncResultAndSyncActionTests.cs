using BindSharp.Extensions;

namespace BindSharp.Test.ExtensionsTests.SideEffect.Tap.Error;

public sealed class TapErrorAsyncOfAsyncResultAndSyncActionTests
{
    [Fact]
    public async Task TapErrorAsync_WithAsyncResultAndSyncAction_ExecutesActionOnFailure()
    {
        // Arrange
        string? capturedError = null;

        // Act
        var result = await Task.FromResult(Result<int, string>.Failure("test error"))
            .TapErrorAsync(error => capturedError = error);

        // Assert
        Assert.Equal("test error", capturedError);
        Assert.True(result.IsFailure);
        Assert.Equal("test error", result.Error);
    }

    [Fact]
    public async Task TapErrorAsync_WithAsyncResultAndSyncAction_DoesNotExecuteOnSuccess()
    {
        // Arrange
        var actionExecuted = false;

        // Act
        var result = await Task.FromResult(Result<int, string>.Success(42))
            .TapErrorAsync(error => actionExecuted = true);

        // Assert
        Assert.False(actionExecuted);
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task TapErrorAsync_WithAsyncResultAndSyncAction_ReturnsOriginalResult()
    {
        // Arrange
        var original = Result<int, string>.Failure("error");

        // Act
        var result = await Task.FromResult(original)
            .TapErrorAsync(error => { }); // Empty action

        // Assert
        Assert.Equal(original, result);
    }

    [Fact]
    public async Task TapErrorAsync_WithAsyncResultAndSyncAction_WorksInPipeline()
    {
        // Arrange
        string? tappedError = null;

        // Act
        var result = await Result.TryAsync<int>(async () =>
            {
                await Task.Delay(1);
                throw new InvalidOperationException("operation failed");
            })
            .TapErrorAsync(ex => tappedError = ex.Message) // Sync tap
            .MapErrorAsync(ex => $"Error: {ex.Message}"); // Sync map

        // Assert
        Assert.Equal("operation failed", tappedError);
        Assert.True(result.IsFailure);
        Assert.Equal("Error: operation failed", result.Error);
    }

    [Fact]
    public async Task TapErrorAsync_WithAsyncResultAndSyncAction_AllowsMultipleTaps()
    {
        // Arrange
        string? firstTap = null;
        string? secondTap = null;

        // Act
        var result = await Task.FromResult(Result<int, string>.Failure("original error"))
            .TapErrorAsync(error => firstTap = error)
            .TapErrorAsync(error => secondTap = $"Processed: {error}");

        // Assert
        Assert.Equal("original error", firstTap);
        Assert.Equal("Processed: original error", secondTap);
        Assert.True(result.IsFailure);
        Assert.Equal("original error", result.Error); // Original error unchanged
    }

    [Fact]
    public async Task TapErrorAsync_WithAsyncResultAndSyncAction_WorksWithExceptions()
    {
        // Arrange
        Exception? capturedEx = null;

        // Act
        var result = await Result.TryAsync<int>(async () =>
            {
                await Task.Delay(1);
                throw new ArgumentException("Invalid arg");
            })
            .TapErrorAsync(ex => capturedEx = ex); // Sync tap - this is what we fixed!

        // Assert
        Assert.NotNull(capturedEx);
        Assert.IsType<ArgumentException>(capturedEx);
        Assert.Equal("Invalid arg", capturedEx.Message);
    }
}