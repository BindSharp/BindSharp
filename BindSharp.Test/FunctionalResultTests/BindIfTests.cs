namespace BindSharp.Test.FunctionalResultTests;

/// <summary>
/// Unit tests for BindIf and BindIfAsync methods.
/// Tests conditional branching functionality in functional pipelines.
/// </summary>
public class BindIfTests
{
    #region Test Helpers

    private static Result<int, string> Success(int value) => Result<int, string>.Success(value);
    private static Result<int, string> Failure(string error) => Result<int, string>.Failure(error);
    
    private static Task<Result<int, string>> SuccessAsync(int value) => 
        Task.FromResult(Result<int, string>.Success(value));
    
    private static Task<Result<int, string>> FailureAsync(string error) => 
        Task.FromResult(Result<int, string>.Failure(error));

    #endregion

    #region BindIf (Synchronous)

    [Fact]
    public void BindIf_WhenPredicateTrue_AppliesContinuation()
    {
        // Arrange
        var result = Success(10);

        // Act
        var output = result.BindIf(
            x => x > 5,
            x => Success(x * 2));

        // Assert
        Assert.True(output.IsSuccess);
        Assert.Equal(20, output.Value); // Continuation applied: 10 * 2
    }

    [Fact]
    public void BindIf_WhenPredicateFalse_ReturnsOriginalResult()
    {
        // Arrange
        var result = Success(3);
        var continuationCalled = false;

        // Act
        var output = result.BindIf(
            x => x > 5,
            x => {
                continuationCalled = true;
                return Success(x * 2);
            });

        // Assert
        Assert.True(output.IsSuccess);
        Assert.Equal(3, output.Value); // Original value unchanged
        Assert.False(continuationCalled);
    }

    [Fact]
    public void BindIf_WhenResultIsFailure_PropagatesError()
    {
        // Arrange
        var result = Failure("Initial error");
        var predicateCalled = false;
        var continuationCalled = false;

        // Act
        var output = result.BindIf(
            x => {
                predicateCalled = true;
                return true;
            },
            x => {
                continuationCalled = true;
                return Success(x);
            });

        // Assert
        Assert.True(output.IsFailure);
        Assert.Equal("Initial error", output.Error);
        Assert.False(predicateCalled);
        Assert.False(continuationCalled);
    }

    [Fact]
    public void BindIf_WhenContinuationFails_ReturnsFailure()
    {
        // Arrange
        var result = Success(10);

        // Act
        var output = result.BindIf(
            x => x > 5,  // TRUE
            x => Failure("Continuation error"));

        // Assert
        Assert.True(output.IsFailure);
        Assert.Equal("Continuation error", output.Error);
    }

    #endregion

    #region BindIfAsync - Task<Result> + Sync Predicate + Sync Continuation

    [Fact]
    public async Task BindIfAsync_TaskResult_SyncPredicate_SyncContinuation_WhenPredicateTrue_AppliesContinuation()
    {
        // Arrange
        var result = SuccessAsync(10);

        // Act
        var output = await result.BindIfAsync(
            x => x > 5,
            x => Success(x * 2));

        // Assert
        Assert.True(output.IsSuccess);
        Assert.Equal(20, output.Value);
    }

    [Fact]
    public async Task BindIfAsync_TaskResult_SyncPredicate_SyncContinuation_WhenPredicateFalse_ReturnsOriginal()
    {
        // Arrange
        var result = SuccessAsync(3);
        var continuationCalled = false;

        // Act
        var output = await result.BindIfAsync(
            x => x > 5,
            x => {
                continuationCalled = true;
                return Success(x * 2);
            });

        // Assert
        Assert.True(output.IsSuccess);
        Assert.Equal(3, output.Value);
        Assert.False(continuationCalled);
    }

    [Fact]
    public async Task BindIfAsync_TaskResult_SyncPredicate_SyncContinuation_WhenResultIsFailure_PropagatesError()
    {
        // Arrange
        var result = FailureAsync("Error");

        // Act
        var output = await result.BindIfAsync(
            x => x > 5,
            x => Success(x * 2));

        // Assert
        Assert.True(output.IsFailure);
        Assert.Equal("Error", output.Error);
    }

    #endregion

    #region BindIfAsync - Result + Sync Predicate + Async Continuation

    [Fact]
    public async Task BindIfAsync_Result_SyncPredicate_AsyncContinuation_WhenPredicateTrue_AppliesContinuation()
    {
        // Arrange
        var result = Success(10);

        // Act
        var output = await result.BindIfAsync(
            x => x > 5,
            async x => {
                await Task.Delay(1);
                return Success(x * 2);
            });

        // Assert
        Assert.True(output.IsSuccess);
        Assert.Equal(20, output.Value);
    }

    [Fact]
    public async Task BindIfAsync_Result_SyncPredicate_AsyncContinuation_WhenPredicateFalse_ReturnsOriginal()
    {
        // Arrange
        var result = Success(3);
        var continuationCalled = false;

        // Act
        var output = await result.BindIfAsync(
            x => x > 5,
            async x => {
                continuationCalled = true;
                await Task.Delay(1);
                return Success(x * 2);
            });

        // Assert
        Assert.True(output.IsSuccess);
        Assert.Equal(3, output.Value);
        Assert.False(continuationCalled);
    }

    [Fact]
    public async Task BindIfAsync_Result_SyncPredicate_AsyncContinuation_WhenResultIsFailure_PropagatesError()
    {
        // Arrange
        var result = Failure("Error");

        // Act
        var output = await result.BindIfAsync(
            x => x > 5,
            async x => {
                await Task.Delay(1);
                return Success(x * 2);
            });

        // Assert
        Assert.True(output.IsFailure);
        Assert.Equal("Error", output.Error);
    }

    #endregion

    #region BindIfAsync - Task<Result> + Sync Predicate + Async Continuation

    [Fact]
    public async Task BindIfAsync_TaskResult_SyncPredicate_AsyncContinuation_WhenPredicateTrue_AppliesContinuation()
    {
        // Arrange
        var result = SuccessAsync(10);

        // Act
        var output = await result.BindIfAsync(
            x => x > 5,
            async x => {
                await Task.Delay(1);
                return Success(x * 2);
            });

        // Assert
        Assert.True(output.IsSuccess);
        Assert.Equal(20, output.Value);
    }

    [Fact]
    public async Task BindIfAsync_TaskResult_SyncPredicate_AsyncContinuation_WhenPredicateFalse_ReturnsOriginal()
    {
        // Arrange
        var result = SuccessAsync(3);
        var continuationCalled = false;

        // Act
        var output = await result.BindIfAsync(
            x => x > 5,
            async x => {
                continuationCalled = true;
                await Task.Delay(1);
                return Success(x * 2);
            });

        // Assert
        Assert.True(output.IsSuccess);
        Assert.Equal(3, output.Value);
        Assert.False(continuationCalled);
    }

    [Fact]
    public async Task BindIfAsync_TaskResult_SyncPredicate_AsyncContinuation_WhenResultIsFailure_PropagatesError()
    {
        // Arrange
        var result = FailureAsync("Error");

        // Act
        var output = await result.BindIfAsync(
            x => x > 5,
            async x => {
                await Task.Delay(1);
                return Success(x * 2);
            });

        // Assert
        Assert.True(output.IsFailure);
        Assert.Equal("Error", output.Error);
    }

    #endregion

    #region BindIfAsync - Result + Async Predicate + Sync Continuation

    [Fact]
    public async Task BindIfAsync_Result_AsyncPredicate_SyncContinuation_WhenPredicateTrue_AppliesContinuation()
    {
        // Arrange
        var result = Success(10);

        // Act
        var output = await result.BindIfAsync(
            async x => {
                await Task.Delay(1);
                return x > 5;
            },
            x => Success(x * 2));

        // Assert
        Assert.True(output.IsSuccess);
        Assert.Equal(20, output.Value);
    }

    [Fact]
    public async Task BindIfAsync_Result_AsyncPredicate_SyncContinuation_WhenPredicateFalse_ReturnsOriginal()
    {
        // Arrange
        var result = Success(3);
        var continuationCalled = false;

        // Act
        var output = await result.BindIfAsync(
            async x => {
                await Task.Delay(1);
                return x > 5;
            },
            x => {
                continuationCalled = true;
                return Success(x * 2);
            });

        // Assert
        Assert.True(output.IsSuccess);
        Assert.Equal(3, output.Value);
        Assert.False(continuationCalled);
    }

    [Fact]
    public async Task BindIfAsync_Result_AsyncPredicate_SyncContinuation_WhenResultIsFailure_PropagatesError()
    {
        // Arrange
        var result = Failure("Error");
        var predicateCalled = false;

        // Act
        var output = await result.BindIfAsync(
            async x => {
                predicateCalled = true;
                await Task.Delay(1);
                return true;
            },
            x => Success(x * 2));

        // Assert
        Assert.True(output.IsFailure);
        Assert.Equal("Error", output.Error);
        Assert.False(predicateCalled);
    }

    #endregion

    #region BindIfAsync - Task<Result> + Async Predicate + Sync Continuation

    [Fact]
    public async Task BindIfAsync_TaskResult_AsyncPredicate_SyncContinuation_WhenPredicateTrue_AppliesContinuation()
    {
        // Arrange
        var result = SuccessAsync(10);

        // Act
        var output = await result.BindIfAsync(
            async x => {
                await Task.Delay(1);
                return x > 5;
            },
            x => Success(x * 2));

        // Assert
        Assert.True(output.IsSuccess);
        Assert.Equal(20, output.Value);
    }

    [Fact]
    public async Task BindIfAsync_TaskResult_AsyncPredicate_SyncContinuation_WhenPredicateFalse_ReturnsOriginal()
    {
        // Arrange
        var result = SuccessAsync(3);
        var continuationCalled = false;

        // Act
        var output = await result.BindIfAsync(
            async x => {
                await Task.Delay(1);
                return x > 5;
            },
            x => {
                continuationCalled = true;
                return Success(x * 2);
            });

        // Assert
        Assert.True(output.IsSuccess);
        Assert.Equal(3, output.Value);
        Assert.False(continuationCalled);
    }

    [Fact]
    public async Task BindIfAsync_TaskResult_AsyncPredicate_SyncContinuation_WhenResultIsFailure_PropagatesError()
    {
        // Arrange
        var result = FailureAsync("Error");
        var predicateCalled = false;

        // Act
        var output = await result.BindIfAsync(
            async x => {
                predicateCalled = true;
                await Task.Delay(1);
                return true;
            },
            x => Success(x * 2));

        // Assert
        Assert.True(output.IsFailure);
        Assert.Equal("Error", output.Error);
        Assert.False(predicateCalled);
    }

    #endregion

    #region BindIfAsync - Result + Async Predicate + Async Continuation

    [Fact]
    public async Task BindIfAsync_Result_AsyncPredicate_AsyncContinuation_WhenPredicateTrue_AppliesContinuation()
    {
        // Arrange
        var result = Success(10);

        // Act
        var output = await result.BindIfAsync(
            async x => {
                await Task.Delay(1);
                return x > 5;
            },
            async x => {
                await Task.Delay(1);
                return Success(x * 2);
            });

        // Assert
        Assert.True(output.IsSuccess);
        Assert.Equal(20, output.Value);
    }

    [Fact]
    public async Task BindIfAsync_Result_AsyncPredicate_AsyncContinuation_WhenPredicateFalse_ReturnsOriginal()
    {
        // Arrange
        var result = Success(3);
        var continuationCalled = false;

        // Act
        var output = await result.BindIfAsync(
            async x => {
                await Task.Delay(1);
                return x > 5;
            },
            async x => {
                continuationCalled = true;
                await Task.Delay(1);
                return Success(x * 2);
            });

        // Assert
        Assert.True(output.IsSuccess);
        Assert.Equal(3, output.Value);
        Assert.False(continuationCalled);
    }

    [Fact]
    public async Task BindIfAsync_Result_AsyncPredicate_AsyncContinuation_WhenResultIsFailure_PropagatesError()
    {
        // Arrange
        var result = Failure("Error");
        var predicateCalled = false;

        // Act
        var output = await result.BindIfAsync(
            async x => {
                predicateCalled = true;
                await Task.Delay(1);
                return true;
            },
            async x => {
                await Task.Delay(1);
                return Success(x * 2);
            });

        // Assert
        Assert.True(output.IsFailure);
        Assert.Equal("Error", output.Error);
        Assert.False(predicateCalled);
    }

    #endregion

    #region BindIfAsync - Task<Result> + Async Predicate + Async Continuation

    [Fact]
    public async Task BindIfAsync_TaskResult_AsyncPredicate_AsyncContinuation_WhenPredicateTrue_AppliesContinuation()
    {
        // Arrange
        var result = SuccessAsync(10);

        // Act
        var output = await result.BindIfAsync(
            async x => {
                await Task.Delay(1);
                return x > 5;
            },
            async x => {
                await Task.Delay(1);
                return Success(x * 2);
            });

        // Assert
        Assert.True(output.IsSuccess);
        Assert.Equal(20, output.Value);
    }

    [Fact]
    public async Task BindIfAsync_TaskResult_AsyncPredicate_AsyncContinuation_WhenPredicateFalse_ReturnsOriginal()
    {
        // Arrange
        var result = SuccessAsync(3);
        var continuationCalled = false;

        // Act
        var output = await result.BindIfAsync(
            async x => {
                await Task.Delay(1);
                return x > 5;
            },
            async x => {
                continuationCalled = true;
                await Task.Delay(1);
                return Success(x * 2);
            });

        // Assert
        Assert.True(output.IsSuccess);
        Assert.Equal(3, output.Value);
        Assert.False(continuationCalled);
    }

    [Fact]
    public async Task BindIfAsync_TaskResult_AsyncPredicate_AsyncContinuation_WhenResultIsFailure_PropagatesError()
    {
        // Arrange
        var result = FailureAsync("Error");
        var predicateCalled = false;

        // Act
        var output = await result.BindIfAsync(
            async x => {
                predicateCalled = true;
                await Task.Delay(1);
                return true;
            },
            async x => {
                await Task.Delay(1);
                return Success(x * 2);
            });

        // Assert
        Assert.True(output.IsFailure);
        Assert.Equal("Error", output.Error);
        Assert.False(predicateCalled);
    }

    #endregion

    #region Real-World Scenario Tests

    [Fact]
    public void BindIf_JsonExtractionScenario_WhenAlreadyJson_ReturnsAsIs()
    {
        // Arrange
        var payload = "{\"name\":\"test\"}";

        // Act - If already JSON (TRUE), skip extraction
        var result = Result<string, string>.Success(payload)
            .Map(p => p.TrimStart())
            .BindIf(
                p => !(p.StartsWith("{") || p.StartsWith("[")),  // If NOT JSON
                p => ExtractJsonFromPrefix(p));                   // Then extract

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("{\"name\":\"test\"}", result.Value);
    }

    [Fact]
    public void BindIf_JsonExtractionScenario_WhenPrefixed_ExtractsJson()
    {
        // Arrange
        var payload = "request:{\"name\":\"test\"}";

        // Act - If NOT JSON (TRUE), extract it
        var result = Result<string, string>.Success(payload)
            .Map(p => p.TrimStart())
            .BindIf(
                p => !(p.StartsWith("{") || p.StartsWith("[")),  // If NOT JSON
                p => ExtractJsonFromPrefix(p));                   // Then extract

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("{\"name\":\"test\"}", result.Value);
    }

    [Fact]
    public async Task BindIfAsync_UserEnrichmentScenario_WhenUserComplete_SkipsEnrichment()
    {
        // Arrange
        var user = new TestUser { Id = 1, Name = "John", IsComplete = true };

        // Act - If NOT complete (FALSE), skip enrichment
        var result = await Result<TestUser, string>.Success(user)
            .BindIfAsync(
                u => !u.IsComplete,  // If incomplete
                async u => await EnrichUserAsync(u));  // Then enrich

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("John", result.Value.Name);
        Assert.Null(result.Value.Email); // Not enriched
    }

    [Fact]
    public async Task BindIfAsync_UserEnrichmentScenario_WhenUserIncomplete_EnrichesUser()
    {
        // Arrange
        var user = new TestUser { Id = 1, Name = "John", IsComplete = false };

        // Act - If incomplete (TRUE), enrich
        var result = await Result<TestUser, string>.Success(user)
            .BindIfAsync(
                u => !u.IsComplete,  // If incomplete
                async u => await EnrichUserAsync(u));  // Then enrich

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("John", result.Value.Name);
        Assert.Equal("john@example.com", result.Value.Email); // Enriched!
    }

    [Fact]
    public async Task BindIfAsync_ConditionalProcessing_ProcessesWhenNeeded()
    {
        // Arrange
        var data = Success(5);

        // Act - If needs processing (TRUE), process it
        var result = await data.BindIfAsync(
            d => d < 10,  // Needs processing
            async d => {
                await Task.Delay(1);
                return Success(d * 10);  // Process
            });

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(50, result.Value);  // Processed: 5 * 10
    }

    [Fact]
    public async Task BindIfAsync_ConditionalProcessing_SkipsWhenNotNeeded()
    {
        // Arrange
        var data = Success(15);

        // Act - If needs processing (FALSE), skip
        var result = await data.BindIfAsync(
            d => d < 10,  // Doesn't need processing
            async d => {
                await Task.Delay(1);
                return Success(d * 10);  // Would process
            });

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(15, result.Value);  // Unchanged
    }

    #endregion

    #region Test Helper Methods

    private static Result<string, string> ExtractJsonFromPrefix(string payload)
    {
        var firstColon = payload.IndexOf(':');
        return firstColon >= 0 
            ? Result<string, string>.Success(payload[(firstColon + 1)..])
            : Result<string, string>.Failure("No colon found");
    }

    private static async Task<Result<TestUser, string>> EnrichUserAsync(TestUser user)
    {
        await Task.Delay(1);
        user.Email = "john@example.com";
        return Result<TestUser, string>.Success(user);
    }

    private class TestUser
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public bool IsComplete { get; set; }
    }

    #endregion
}