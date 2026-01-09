using BindSharp.Extensions;
using NSubstitute;

namespace BindSharp.Test.ExtensionsTests.SideEffect.Tap.Error;

public sealed class RealWorldScenariosTests
{
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
}