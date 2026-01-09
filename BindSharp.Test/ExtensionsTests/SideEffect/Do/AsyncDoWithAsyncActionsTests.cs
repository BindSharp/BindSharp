using BindSharp.Extensions;

namespace BindSharp.Test.ExtensionsTests.SideEffect.Do;

public sealed class AsyncDoWithAsyncActionsTests
{
    [Fact]
    public async Task DoAsync_WithAsyncActions_ExecutesOnSuccess()
    {
        // Arrange
        var result = Result<int, string>.Success(42);
        bool successCalled = false;

        // Act
        var returned = await result.DoAsync(
            async success => {
                await Task.Delay(10);
                successCalled = true;
            },
            async error => await Task.CompletedTask
        );

        // Assert
        Assert.True(successCalled);
        Assert.Equal(result, returned);
    }

    [Fact]
    public async Task DoAsync_WithAsyncActions_ExecutesOnFailure()
    {
        // Arrange
        var result = Result<int, string>.Failure("Error");
        bool failureCalled = false;

        // Act
        var returned = await result.DoAsync(
            async success => await Task.CompletedTask,
            async error => {
                await Task.Delay(10);
                failureCalled = true;
            }
        );

        // Assert
        Assert.True(failureCalled);
        Assert.Equal(result, returned);
    }
}