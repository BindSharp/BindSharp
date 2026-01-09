using BindSharp.Extensions;

namespace BindSharp.Test.ExtensionsTests.SideEffect.Tap.Error;

public sealed class IntegrationAndMixedPipelinesTests
{
    [Fact]
    public async Task MixedPipeline_WithSyncAndAsyncTaps_WorksCorrectly()
    {
        // Arrange
        int successTap = 0;
        string? successPathErrorTap = null;
        string? failurePathErrorTap = null;

        // Act - Success path
        var successResult = await Task.FromResult(Result<int, string>.Success(42))
            .TapAsync(value => successTap = value)              // Sync success tap
            .TapErrorAsync(error => successPathErrorTap = error)           // Sync error tap (won't execute)
            .MapAsync(value => value * 2);

        // Act - Failure path
        var failureResult = await Task.FromResult(Result<int, string>.Failure("error"))
            .TapAsync(value => successTap = value * 10)         // Sync success tap (won't execute)
            .TapErrorAsync(error => failurePathErrorTap = error);          // Sync error tap

        // Assert
        Assert.Equal(42, successTap);
        Assert.Null(successPathErrorTap); // From success path
        Assert.NotNull(failurePathErrorTap); // From error path
        
        Assert.True(successResult.IsSuccess);
        Assert.Equal(84, successResult.Value);
        
        Assert.True(failureResult.IsFailure);
        Assert.Equal("error", failureResult.Error);
    }

    [Fact]
    public async Task MixedPipeline_CombiningSyncAndAsyncTaps_WorksCorrectly()
    {
        // Arrange
        int syncTap = 0;
        int asyncTap = 0;

        // Act
        var result = await Task.FromResult(Result<int, string>.Success(100))
            .TapAsync(value => syncTap = value)                 // Sync tap
            .TapAsync(async value =>                            // Async tap
            {
                await Task.Delay(1);
                asyncTap = value * 2;
            })
            .TapAsync(value => { });                            // Another sync tap

        // Assert
        Assert.Equal(100, syncTap);
        Assert.Equal(200, asyncTap);
        Assert.True(result.IsSuccess);
        Assert.Equal(100, result.Value);
    }

    [Fact]
    public async Task RealWorldScenario_WithTryAndSyncLogging_WorksCleanly()
    {
        // Arrange
        Exception? loggedEx = null;
        var operationExecuted = false;

        // Act - simulating the original issue
        var result = await Result.TryAsync<int>(async () =>
            {
                await Task.Delay(1);
                throw new InvalidOperationException("Database connection failed");
            })
            .TapErrorAsync(ex => loggedEx = ex)                 // Simple sync logging
            .MapErrorAsync(ex => $"Error: {ex.Message}");

        // Assert
        Assert.NotNull(loggedEx);
        Assert.IsType<InvalidOperationException>(loggedEx);
        Assert.Equal("Database connection failed", loggedEx.Message);
        Assert.True(result.IsFailure);
        Assert.Equal("Error: Database connection failed", result.Error);
    }

    [Fact]
    public async Task ComplexPipeline_WithAllTapVariants_WorksCorrectly()
    {
        // Arrange
        int syncSuccessTap = 0;
        int asyncSuccessTap = 0;
        string? syncErrorTap = null;
        string? asyncErrorTap = null;

        // Act - Success case
        await Task.FromResult(Result<int, string>.Success(50))
            .TapAsync(v => syncSuccessTap = v)                  // New: Task<Result> + sync action
            .TapAsync(async v =>                                // Existing: Task<Result> + async action
            {
                await Task.Delay(1);
                asyncSuccessTap = v * 2;
            });

        // Act - Failure case
        await Task.FromResult(Result<int, string>.Failure("failed"))
            .TapErrorAsync(e => syncErrorTap = e)               // New: Task<Result> + sync action
            .TapErrorAsync(async e =>                           // Existing: Task<Result> + async action
            {
                await Task.Delay(1);
                asyncErrorTap = $"Async: {e}";
            });

        // Assert
        Assert.Equal(50, syncSuccessTap);
        Assert.Equal(100, asyncSuccessTap);
        Assert.Equal("failed", syncErrorTap);
        Assert.Equal("Async: failed", asyncErrorTap);
    }
}