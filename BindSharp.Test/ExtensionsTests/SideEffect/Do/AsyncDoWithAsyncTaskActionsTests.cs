using BindSharp.Extensions;

namespace BindSharp.Test.ExtensionsTests.SideEffect.Do;

public sealed class AsyncDoWithAsyncTaskActionsTests
{
    [Fact]
    public async Task DoAsync_WithTaskResultAndAsyncActions_ExecutesOnSuccess()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int, string>.Success(42));
        var successCalled = false;

        // Act
        var returned = await resultTask.DoAsync(
            async success => {
                await Task.Delay(10);
                successCalled = true;
            },
            async error => await Task.CompletedTask
        );

        // Assert
        Assert.True(successCalled);
        Assert.True(returned.IsSuccess);
    }

    [Fact]
    public async Task DoAsync_PreservesResultForAsyncComposition()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int, string>.Success(10));

        // Act
        var final = await resultTask
            .DoAsync(
                async success => await Task.Delay(10),
                async error => await Task.CompletedTask
            )
            .MapAsync(x => x * 2)
            .MapAsync(async x => {
                await Task.Delay(10);
                return x.ToString();
            });

        // Assert
        Assert.True(final.IsSuccess);
        Assert.Equal("20", final.Value);
    }
}