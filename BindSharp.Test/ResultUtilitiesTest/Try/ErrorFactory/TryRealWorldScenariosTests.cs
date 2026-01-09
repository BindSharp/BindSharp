using BindSharp.Extensions;

namespace BindSharp.Test.ResultUtilitiesTest.Try.ErrorFactory;

public sealed class TryRealWorldScenariosTests
{
    [Fact]
    public void Try_WithErrorFactory_ParseIntegerScenario()
    {
        // Act
        var successResult = Result.Try(
            () => int.Parse("123"),
            ex => $"Failed to parse number: {ex.Message}");

        var failureResult = Result.Try(
            () => int.Parse("abc"),
            ex => $"Failed to parse number: {ex.Message}");

        // Assert
        Assert.True(successResult.IsSuccess);
        Assert.Equal(123, successResult.Value);

        Assert.True(failureResult.IsFailure);
        Assert.Contains("Failed to parse number", failureResult.Error);
    }

    [Fact]
    public async Task TryAsync_WithErrorFactory_HttpRequestScenario()
    {
        // Simulate HTTP request that might fail
        async Task<string> FetchDataAsync(bool shouldSucceed)
        {
            await Task.Delay(1);
            if (!shouldSucceed)
                throw new HttpRequestException("HTTP 404: Not Found");
            return "Success data";
        }

        // Act
        var successResult = await Result.TryAsync(
            () => FetchDataAsync(true),
            ex => $"API call failed: {ex.Message}");

        var failureResult = await Result.TryAsync(
            () => FetchDataAsync(false),
            ex => $"API call failed: {ex.Message}");

        // Assert
        Assert.True(successResult.IsSuccess);
        Assert.Equal("Success data", successResult.Value);

        Assert.True(failureResult.IsFailure);
        Assert.Contains("API call failed", failureResult.Error);
        Assert.Contains("404", failureResult.Error);
    }

    [Fact]
    public void Try_WithErrorFactory_CanChainWithOtherResultOperations()
    {
        // Act
        var result = Result.Try(
                () => "42",
                ex => $"Parse error: {ex.Message}")
            .Map(int.Parse)
            .Map(x => x * 2)
            .Ensure(x => x > 0, "Must be positive");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(84, result.Value);
    }

    [Fact]
    public async Task TryAsync_WithErrorFactory_CanChainWithOtherAsyncOperations()
    {
        // Act
        var result = await Result.TryAsync(
                async () =>
                {
                    await Task.Delay(1);
                    return "42";
                },
                ex => $"Error: {ex.Message}")
            .MapAsync(int.Parse)
            .MapAsync(x => x * 2)
            .EnsureAsync(x => x > 0, "Must be positive");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(84, result.Value);
    }
}