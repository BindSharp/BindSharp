using BindSharp.Extensions;

namespace BindSharp.Test.ExtensionsTests.SideEffect.Do;

public sealed class MixedSyncAsyncTests
{
    [Fact]
    public async Task DoAsync_WithAsyncSuccessAndSyncFailure_ExecutesCorrectly()
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
            error => { } // Sync failure handler
        );

        // Assert
        Assert.True(successCalled);
        Assert.Equal(result, returned);
    }

    [Fact]
    public async Task DoAsync_WithSyncSuccessAndAsyncFailure_ExecutesCorrectly()
    {
        // Arrange
        var result = Result<int, string>.Failure("Error");
        bool failureCalled = false;

        // Act
        var returned = await result.DoAsync(
            success => { }, // Sync success handler
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