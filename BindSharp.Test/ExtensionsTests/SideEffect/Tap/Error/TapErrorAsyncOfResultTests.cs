using BindSharp.Extensions;
using NSubstitute;

namespace BindSharp.Test.ExtensionsTests.SideEffect.Tap.Error;

public sealed class TapErrorAsyncOfResultTests
{
    [Fact]
    public async Task TapErrorAsync_WithResult_WhenFailure_ExecutesAsyncAction()
    {
        // Arrange
        var action = Substitute.For<Func<string, Task>>();
        action.Invoke(Arg.Any<string>()).Returns(Task.CompletedTask);
        var result = Result<int, string>.Failure("Async error");

        // Act
        var actualResult = await result.TapErrorAsync(action);

        // Assert
        await action.Received(1).Invoke("Async error");
        Assert.Equal(result, actualResult);
    }

    [Fact]
    public async Task TapErrorAsync_WithResult_WhenSuccess_DoesNotExecuteAsyncAction()
    {
        // Arrange
        var action = Substitute.For<Func<string, Task>>();
        var result = Result<int, string>.Success(42);

        // Act
        var actualResult = await result.TapErrorAsync(action);

        // Assert
        await action.DidNotReceive().Invoke(Arg.Any<string>());
        Assert.Equal(result, actualResult);
    }

    [Fact]
    public async Task TapErrorAsync_WithResult_WhenFailure_ReturnsOriginalResult()
    {
        // Arrange
        var result = Result<int, string>.Failure("Original async error");

        // Act
        var actualResult = await result.TapErrorAsync(async err => await Task.CompletedTask);

        // Assert
        Assert.True(actualResult.IsFailure);
        Assert.Equal("Original async error", actualResult.Error);
    }
}