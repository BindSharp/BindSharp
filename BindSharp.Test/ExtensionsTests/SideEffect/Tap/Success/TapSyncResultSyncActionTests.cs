using BindSharp.Extensions;

namespace BindSharp.Test.ExtensionsTests.SideEffect.Tap.Success;

public sealed class TapSyncResultSyncActionTests
{
    [Fact]
    public void Tap_WhenResultIsSuccess_ExecutesAction()
    {
        // Arrange
        var result = Result<int, string>.Success(42);
        bool actionExecuted = false;

        // Act
        var output = result.Tap(x => actionExecuted = true);

        // Assert
        Assert.True(actionExecuted);
    }

    [Fact]
    public void Tap_WhenResultIsSuccess_PassesCorrectValueToAction()
    {
        // Arrange
        var result = Result<int, string>.Success(42);
        int capturedValue = 0;

        // Act
        result.Tap(x => capturedValue = x);

        // Assert
        Assert.Equal(42, capturedValue);
    }

    [Fact]
    public void Tap_WhenResultIsSuccess_ReturnsOriginalResult()
    {
        // Arrange
        var result = Result<int, string>.Success(42);

        // Act
        var output = result.Tap(x => { });

        // Assert
        Assert.True(output.IsSuccess);
        Assert.Equal(42, output.Value);
        Assert.Same(result, output);
    }

    [Fact]
    public void Tap_WhenResultIsFailure_DoesNotExecuteAction()
    {
        // Arrange
        var result = Result<int, string>.Failure("error");
        bool actionExecuted = false;

        // Act
        var output = result.Tap(x => actionExecuted = true);

        // Assert
        Assert.False(actionExecuted);
    }

    [Fact]
    public void Tap_WhenResultIsFailure_ReturnsOriginalError()
    {
        // Arrange
        var result = Result<int, string>.Failure("error");

        // Act
        var output = result.Tap(x => { });

        // Assert
        Assert.True(output.IsFailure);
        Assert.Equal("error", output.Error);
        Assert.Same(result, output);
    }
}