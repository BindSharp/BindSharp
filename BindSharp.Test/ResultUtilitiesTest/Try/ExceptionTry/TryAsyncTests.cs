using BindSharp.Extensions;

namespace BindSharp.Test.ResultUtilitiesTest.Try.ExceptionTry;

public sealed class TryAsyncTests
{
    [Fact]
    public async Task TryAsync_WithSuccessfulOperation_ReturnsSuccess()
    {
        // Arrange
        var expected = "success";

        // Act
        var result = await Result.TryAsync(async () =>
        {
            await Task.Delay(1);
            return expected;
        });

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public async Task TryAsync_WithFailingOperation_ReturnsFailureWithException()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Async error");

        // Act
        var result = await Result.TryAsync<int>(async () =>
        {
            await Task.Delay(1);
            throw expectedException;
        });

        // Assert
        Assert.True(result.IsFailure);
        Assert.Same(expectedException, result.Error);
    }

    [Fact]
    public async Task TryAsync_WithFailingOperation_PreservesExceptionType()
    {
        // Act
        var result = await Result.TryAsync<string>(async () =>
        {
            await Task.Delay(1);
            throw new TimeoutException("Operation timed out");
        });

        // Assert
        Assert.True(result.IsFailure);
        Assert.IsType<TimeoutException>(result.Error);
        Assert.Equal("Operation timed out", result.Error.Message);
    }

    [Fact]
    public async Task TryAsync_WithTapErrorAsync_AllowsAsyncLogging()
    {
        // Arrange
        Exception? capturedEx = null;
        var expectedException = new InvalidOperationException("Test");

        // Act
        var result = await Result.TryAsync<int>(async () =>
            {
                await Task.Delay(1);
                throw expectedException;
            })
            .TapErrorAsync(async ex =>
            {
                await Task.Delay(1); // Simulate async logging
                capturedEx = ex;
            });

        // Assert
        Assert.Same(expectedException, capturedEx);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task TryAsync_WithMapErrorAsync_AllowsAsyncTransformation()
    {
        // Act
        var result = await Result.TryAsync<int>(async () =>
            {
                await Task.Delay(1);
                throw new InvalidOperationException("Original");
            })
            .MapErrorAsync(async ex =>
            {
                await Task.Delay(1); // Simulate async processing
                return $"Processed: {ex.Message}";
            });

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Processed: Original", result.Error);
    }

    [Fact]
    public async Task TryAsync_WithFullAsyncPipeline_WorksCorrectly()
    {
        // Arrange
        Exception? loggedEx = null;
        var expectedException = new FileNotFoundException("Config.json");

        // Act
        var result = await Result.TryAsync<string>(async () =>
            {
                await Task.Delay(1);
                throw expectedException;
            })
            .TapErrorAsync(async ex =>
            {
                await Task.Delay(1);
                loggedEx = ex;
            })
            .MapErrorAsync(async ex =>
            {
                await Task.Delay(1);
                return ex switch
                {
                    FileNotFoundException => "Configuration file missing",
                    TimeoutException => "Operation timed out",
                    _ => "Unknown error occurred"
                };
            });

        // Assert
        Assert.Same(expectedException, loggedEx);
        Assert.True(result.IsFailure);
        Assert.Equal("Configuration file missing", result.Error);
    }

    [Fact]
    public async Task TryAsync_WithSyncTapError_AllowsMixedPipeline()
    {
        // Arrange
        Exception? capturedEx = null;

        // Act
        var result = await Result.TryAsync<int>(async () =>
            {
                await Task.Delay(1);
                throw new ArgumentException("Invalid arg");
            })
            .TapErrorAsync(ex => capturedEx = ex) // Sync tap
            .MapErrorAsync(ex => ex.Message); // Sync map

        // Assert
        Assert.NotNull(capturedEx);
        Assert.IsType<ArgumentException>(capturedEx);
        Assert.Equal("Invalid arg", result.Error);
    }
}