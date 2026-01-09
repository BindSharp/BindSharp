using BindSharp.Extensions;

namespace BindSharp.Test.ExtensionsTests.SideEffect.Tap.Success;

public sealed class TapInPipelinesTests
{
    [Fact]
    public void Tap_CanBeChainedInPipeline()
    {
        // Arrange
        var result = Result<int, string>.Success(10);
        var step1Value = 0;
        var step2Value = 0;

        // Act
        var output = result
            .Tap(x => step1Value = x)
            .Map(x => x * 2)
            .Tap(x => step2Value = x)
            .Map(x => x + 5);

        // Assert
        Assert.Equal(10, step1Value);
        Assert.Equal(20, step2Value);
        Assert.True(output.IsSuccess);
        Assert.Equal(25, output.Value);
    }

    [Fact]
    public async Task TapAsync_CanBeChainedInAsyncPipeline()
    {
        // Arrange
        var result = Task.FromResult(Result<int, string>.Success(10));
        var step1Value = 0;
        var step2Value = 0;

        // Act
        var output = await result
            .TapAsync(x => step1Value = x)
            .MapAsync(x => x * 2)
            .TapAsync(async x =>
            {
                await Task.Delay(1);
                step2Value = x;
            })
            .MapAsync(x => x + 5);

        // Assert
        Assert.Equal(10, step1Value);
        Assert.Equal(20, step2Value);
        Assert.True(output.IsSuccess);
        Assert.Equal(25, output.Value);
    }
}