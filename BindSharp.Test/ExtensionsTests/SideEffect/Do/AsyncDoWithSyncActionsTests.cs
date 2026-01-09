using BindSharp.Extensions;

namespace BindSharp.Test.ExtensionsTests.SideEffect.Do;

public sealed class AsyncDoWithSyncActionsTests
{
    [Fact]
    public async Task DoAsync_WithTaskResult_ExecutesOnSuccess()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int, string>.Success(42));
        bool successCalled = false;

        // Act
        var returned = await resultTask.DoAsync(
            success => successCalled = true,
            error => { }
        );

        // Assert
        Assert.True(successCalled);
        Assert.True(returned.IsSuccess);
    }

    [Fact]
    public async Task DoAsync_WithTaskResult_ExecutesOnFailure()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int, string>.Failure("Error"));
        bool failureCalled = false;

        // Act
        var returned = await resultTask.DoAsync(
            success => { },
            error => failureCalled = true
        );

        // Assert
        Assert.True(failureCalled);
        Assert.True(returned.IsFailure);
    }
}