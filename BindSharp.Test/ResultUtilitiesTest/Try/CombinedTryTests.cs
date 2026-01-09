namespace BindSharp.Test.ResultUtilitiesTest.Try;

public sealed class CombinedTryTests
{
    [Fact]
    public void Try_BothOverloads_CanCoexist()
    {
        // The exception-first overload
        var result1 = Result.Try(() => 42);
        
        // The error-factory overload
        var result2 = Result.Try(
            () => 42,
            ex => $"Error: {ex.Message}");

        // Assert - both should succeed
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.Equal(42, result1.Value);
        Assert.Equal(42, result2.Value);
    }

    [Fact]
    public async Task TryAsync_BothOverloads_CanCoexist()
    {
        // The exception-first overload
        var result1 = await Result.TryAsync(async () =>
        {
            await Task.Delay(1);
            return "test";
        });

        // The error-factory overload
        var result2 = await Result.TryAsync(
            async () =>
            {
                await Task.Delay(1);
                return "test";
            },
            ex => $"Error: {ex.Message}");

        // Assert - both should succeed
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.Equal("test", result1.Value);
        Assert.Equal("test", result2.Value);
    }
}