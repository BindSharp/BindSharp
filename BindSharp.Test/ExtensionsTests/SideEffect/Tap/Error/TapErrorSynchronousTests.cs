using BindSharp.Extensions;
using NSubstitute;

namespace BindSharp.Test.ExtensionsTests.SideEffect.Tap.Error;

public sealed class TapErrorSynchronousTests
{
    [Fact]
    public void TapError_WhenFailure_ExecutesAction()
    {
        // Arrange
        var action = Substitute.For<Action<string>>();
        var result = Result<int, string>.Failure("Error occurred");

        // Act
        var actualResult = result.TapError(action);

        // Assert
        action.Received(1).Invoke("Error occurred");
        Assert.Equal(result, actualResult);
    }

    [Fact]
    public void TapError_WhenSuccess_DoesNotExecuteAction()
    {
        // Arrange
        var action = Substitute.For<Action<string>>();
        var result = Result<int, string>.Success(42);

        // Act
        var actualResult = result.TapError(action);

        // Assert
        action.DidNotReceive().Invoke(Arg.Any<string>());
        Assert.Equal(result, actualResult);
    }

    [Fact]
    public void TapError_WhenFailure_ReturnsOriginalResult()
    {
        // Arrange
        var result = Result<int, string>.Failure("Original error");

        // Act
        var actualResult = result.TapError(err => { /* side effect */ });

        // Assert
        Assert.True(actualResult.IsFailure);
        Assert.Equal("Original error", actualResult.Error);
    }

    [Fact]
    public void TapError_CanBeChained()
    {
        // Arrange
        var firstAction = Substitute.For<Action<string>>();
        var secondAction = Substitute.For<Action<string>>();
        var result = Result<int, string>.Failure("Error");

        // Act
        var actualResult = result
            .TapError(firstAction)
            .TapError(secondAction);

        // Assert
        firstAction.Received(1).Invoke("Error");
        secondAction.Received(1).Invoke("Error");
        Assert.True(actualResult.IsFailure);
    }
}