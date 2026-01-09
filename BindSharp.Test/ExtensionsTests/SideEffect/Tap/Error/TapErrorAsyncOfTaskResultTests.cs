using BindSharp.Extensions;
using NSubstitute;

namespace BindSharp.Test.ExtensionsTests.SideEffect.Tap.Error;

public sealed class TapErrorAsyncOfTaskResultTests
{
    [Fact]
    public async Task TapErrorAsync_WithTaskResult_WhenFailure_ExecutesAsyncAction()
    {
        // Arrange
        var action = Substitute.For<Func<string, Task>>();
        action.Invoke(Arg.Any<string>()).Returns(Task.CompletedTask);
        var resultTask = Task.FromResult(Result<int, string>.Failure("Task error"));

        // Act
        var actualResult = await resultTask.TapErrorAsync(action);

        // Assert
        await action.Received(1).Invoke("Task error");
        Assert.True(actualResult.IsFailure);
    }

    [Fact]
    public async Task TapErrorAsync_WithTaskResult_WhenSuccess_DoesNotExecuteAsyncAction()
    {
        // Arrange
        var action = Substitute.For<Func<string, Task>>();
        var resultTask = Task.FromResult(Result<int, string>.Success(42));

        // Act
        var actualResult = await resultTask.TapErrorAsync(action);

        // Assert
        await action.DidNotReceive().Invoke(Arg.Any<string>());
        Assert.True(actualResult.IsSuccess);
    }

    [Fact]
    public async Task TapErrorAsync_WithTaskResult_WhenFailure_ReturnsOriginalResult()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int, string>.Failure("Original task error"));

        // Act
        var actualResult = await resultTask.TapErrorAsync(async err => await Task.CompletedTask);

        // Assert
        Assert.True(actualResult.IsFailure);
        Assert.Equal("Original task error", actualResult.Error);
    }

    [Fact]
    public async Task TapErrorAsync_WithTaskResult_CanBeChainedInPipeline()
    {
        // Arrange
        var firstAction = Substitute.For<Func<string, Task>>();
        var secondAction = Substitute.For<Func<string, Task>>();
        firstAction.Invoke(Arg.Any<string>()).Returns(Task.CompletedTask);
        secondAction.Invoke(Arg.Any<string>()).Returns(Task.CompletedTask);

        // Act
        var result = await Task.FromResult(Result<int, string>.Failure("Pipeline error"))
            .TapErrorAsync(firstAction)
            .TapErrorAsync(secondAction);

        // Assert
        await firstAction.Received(1).Invoke("Pipeline error");
        await secondAction.Received(1).Invoke("Pipeline error");
        Assert.True(result.IsFailure);
    }
}