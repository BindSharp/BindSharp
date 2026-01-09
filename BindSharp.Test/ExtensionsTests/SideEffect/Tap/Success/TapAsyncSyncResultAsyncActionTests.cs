using BindSharp.Extensions;

namespace BindSharp.Test.ExtensionsTests.SideEffect.Tap.Success;

public sealed class TapAsyncSyncResultAsyncActionTests
{
    [Fact]
    public async Task TapAsync_WithAsyncAction_WhenResultIsSuccess_ExecutesAction()
    {
        // Arrange
        var result = Result<int, string>.Success(42);
        var actionExecuted = false;

        // Act
        var output = await result.TapAsync(async x =>
        {
            await Task.Delay(1);
            actionExecuted = true;
        });

        // Assert
        Assert.True(actionExecuted);
    }

    [Fact]
    public async Task TapAsync_WithAsyncAction_WhenResultIsSuccess_PassesCorrectValueToAction()
    {
        // Arrange
        var result = Result<int, string>.Success(42);
        int capturedValue = 0;

        // Act
        await result.TapAsync(async x =>
        {
            await Task.Delay(1);
            capturedValue = x;
        });

        // Assert
        Assert.Equal(42, capturedValue);
    }

    [Fact]
    public async Task TapAsync_WithAsyncAction_WhenResultIsSuccess_ReturnsOriginalResult()
    {
        // Arrange
        var result = Result<int, string>.Success(42);

        // Act
        var output = await result.TapAsync(async x => await Task.Delay(1));

        // Assert
        Assert.True(output.IsSuccess);
        Assert.Equal(42, output.Value);
    }

    [Fact]
    public async Task TapAsync_WithAsyncAction_WhenResultIsFailure_DoesNotExecuteAction()
    {
        // Arrange
        var result = Result<int, string>.Failure("error");
        var actionExecuted = false;

        // Act
        var output = await result.TapAsync(async x =>
        {
            await Task.Delay(1);
            actionExecuted = true;
        });

        // Assert
        Assert.False(actionExecuted);
    }

    [Fact]
    public async Task TapAsync_WithAsyncAction_WhenResultIsFailure_ReturnsOriginalError()
    {
        // Arrange
        var result = Result<int, string>.Failure("error");

        // Act
        var output = await result.TapAsync(async x => await Task.Delay(1));

        // Assert
        Assert.True(output.IsFailure);
        Assert.Equal("error", output.Error);
    }
}