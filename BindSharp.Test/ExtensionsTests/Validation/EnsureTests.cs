using BindSharp.Extensions;

namespace BindSharp.Test.ExtensionsTests.Validation;

/// <summary>
/// Tests for Ensure / EnsureAsync, in particular that an upstream failure is never replaced.
/// </summary>
public class EnsureTests
{
    [Fact]
    public void Ensure_SuccessAndPredicateTrue_ReturnsOriginalResult()
    {
        var result = Result<int, string>.Success(5).Ensure(x => x > 0, "must be positive");

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value);
    }

    [Fact]
    public void Ensure_SuccessAndPredicateFalse_ReturnsProvidedError()
    {
        var result = Result<int, string>.Success(5).Ensure(x => x > 10, "too small");

        Assert.True(result.IsFailure);
        Assert.Equal("too small", result.Error);
    }

    [Fact]
    public void Ensure_UpstreamFailure_PreservesOriginalError()
    {
        var result = Result<int, string>.Failure("original").Ensure(x => x > 0, "validation error");

        Assert.True(result.IsFailure);
        Assert.Equal("original", result.Error);
    }

    [Fact]
    public void Ensure_UpstreamFailure_DoesNotEvaluatePredicate()
    {
        var evaluated = false;

        Result<int, string>.Failure("original").Ensure(_ => { evaluated = true; return true; }, "validation error");

        Assert.False(evaluated);
    }

    [Fact]
    public void Ensure_ChainedAfterFailure_KeepsFirstError()
    {
        var result = Result<int, string>.Failure("original")
            .Ensure(x => x > 0, "first")
            .Ensure(x => x < 100, "second");

        Assert.Equal("original", result.Error);
    }

    [Fact]
    public async Task EnsureAsync_SuccessAndPredicateTrue_ReturnsOriginalResult()
    {
        var result = await Task.FromResult(Result<int, string>.Success(5)).EnsureAsync(x => x > 0, "must be positive");

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value);
    }

    [Fact]
    public async Task EnsureAsync_SuccessAndPredicateFalse_ReturnsProvidedError()
    {
        var result = await Task.FromResult(Result<int, string>.Success(5)).EnsureAsync(x => x > 10, "too small");

        Assert.True(result.IsFailure);
        Assert.Equal("too small", result.Error);
    }

    [Fact]
    public async Task EnsureAsync_UpstreamFailure_PreservesOriginalError()
    {
        var result = await Task.FromResult(Result<int, string>.Failure("original"))
            .EnsureAsync(x => x > 0, "validation error");

        Assert.True(result.IsFailure);
        Assert.Equal("original", result.Error);
    }

    [Fact]
    public async Task EnsureAsync_AfterMapErrorAsync_KeepsMappedError()
    {
        // Regression: a repository-style failure mapped to a domain error must not be replaced by the
        // validation error of a later EnsureAsync.
        var result = await Task.FromResult(Result<int, string>.Failure("db down"))
            .MapErrorAsync(e => $"DataAccess: {e}")
            .EnsureAsync(affected => affected > 0, "InvalidTransition");

        Assert.Equal("DataAccess: db down", result.Error);
    }
}
