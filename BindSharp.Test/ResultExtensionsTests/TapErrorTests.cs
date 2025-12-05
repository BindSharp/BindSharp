using NSubstitute;

namespace BindSharp.Test.ResultExtensionsTests;

/// <summary>
/// Unit tests for TapError and TapErrorAsync methods.
/// Tests conditional branching functionality in functional pipelines.
/// </summary>
public sealed class TapErrorTests
{
    #region TapError (Synchronous)

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

    #endregion

    #region TapErrorAsync (Result<T, TError>)

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

    #endregion

    #region TapErrorAsync (Task<Result<T, TError>>)

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

    #endregion

    #region Real-World Scenarios

    [Fact]
    public async Task TapErrorAsync_LoggingScenario_LogsErrorButDoesNotTransform()
    {
        // Arrange
        var logger = new TestLogger();

        // Act
        var result = await GetUserAsync(0)
            .TapErrorAsync(error => logger.LogErrorAsync(error))
            .MapAsync(user => user.Name);

        // Assert
        Assert.Single(logger.LoggedErrors);
        Assert.Equal("User not found", logger.LoggedErrors[0]);
        Assert.True(result.IsFailure);
        Assert.Equal("User not found", result.Error);
    }

    [Fact]
    public async Task TapError_MixedWithTap_OnlyExecutesAppropriateAction()
    {
        // Arrange
        var successAction = Substitute.For<Action<int>>();
        var errorAction = Substitute.For<Action<string>>();

        // Act - Failure case
        var failureResult = Result<int, string>.Failure("Error")
            .Tap(successAction)
            .TapError(errorAction);

        // Assert
        successAction.DidNotReceive().Invoke(Arg.Any<int>());
        errorAction.Received(1).Invoke("Error");

        // Act - Success case
        var successResult = Result<int, string>.Success(42)
            .Tap(successAction)
            .TapError(errorAction);

        // Assert
        successAction.Received(1).Invoke(42);
        errorAction.Received(1).Invoke(Arg.Any<string>()); // Still only once from failure case
    }

    #endregion

    #region Helper Methods and Classes

    private static Task<Result<User, string>> GetUserAsync(int id)
    {
        return Task.FromResult(
            id > 0 
                ? Result<User, string>.Success(new User { Id = id, Name = "John" })
                : Result<User, string>.Failure("User not found")
        );
    }

    private class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class TestLogger
    {
        public List<string> LoggedErrors { get; } = new();

        public Task LogErrorAsync(string error)
        {
            LoggedErrors.Add(error);
            return Task.CompletedTask;
        }
    }

    #endregion
}