using BindSharp.Extensions;

namespace BindSharp.Test.ExtensionsTests.SideEffect.Tap.Success;

public sealed class TapAsyncAsyncResultAsyncActionTests
{
    [Fact]
    public async Task TapAsync_WithAsyncResultAndAsyncAction_WhenResultIsSuccess_ExecutesAction()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int, string>.Success(42));
        var actionExecuted = false;

        // Act
        var output = await resultTask.TapAsync(async x =>
        {
            await Task.Delay(1);
            actionExecuted = true;
        });

        // Assert
        Assert.True(actionExecuted);
    }

    [Fact]
    public async Task TapAsync_WithAsyncResultAndAsyncAction_WhenResultIsSuccess_PassesCorrectValueToAction()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int, string>.Success(42));
        int capturedValue = 0;

        // Act
        await resultTask.TapAsync(async x =>
        {
            await Task.Delay(1);
            capturedValue = x;
        });

        // Assert
        Assert.Equal(42, capturedValue);
    }

    [Fact]
    public async Task TapAsync_WithAsyncResultAndAsyncAction_WhenResultIsSuccess_ReturnsOriginalResult()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int, string>.Success(42));

        // Act
        var output = await resultTask.TapAsync(async x => await Task.Delay(1));

        // Assert
        Assert.True(output.IsSuccess);
        Assert.Equal(42, output.Value);
    }

    [Fact]
    public async Task TapAsync_WithAsyncResultAndAsyncAction_WhenResultIsFailure_DoesNotExecuteAction()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int, string>.Failure("error"));
        var actionExecuted = false;

        // Act
        var output = await resultTask.TapAsync(async x =>
        {
            await Task.Delay(1);
            actionExecuted = true;
        });

        // Assert
        Assert.False(actionExecuted);
    }

    [Fact]
    public async Task TapAsync_WithAsyncResultAndAsyncAction_WhenResultIsFailure_ReturnsOriginalError()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int, string>.Failure("error"));

        // Act
        var output = await resultTask.TapAsync(async x => await Task.Delay(1));

        // Assert
        Assert.True(output.IsFailure);
        Assert.Equal("error", output.Error);
    }
}