using BindSharp.Extensions;
using BindSharp.Test.Helpers;

namespace BindSharp.Test.ResultUtilitiesTest.Try.ErrorFactory;

public sealed class SyncTryExceptionFirstWithFinallyTests
{
    [Fact]
    public void Try_ExceptionFirstWithFinally_WhenSucceeds_ExecutesFinallyBlock()
    {
        // Arrange
        bool finallyExecuted = false;

        // Act
        var result = Result.Try(
            () => 42,
            @finally: () => finallyExecuted = true);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.True(finallyExecuted);
    }

    [Fact]
    public void Try_ExceptionFirstWithFinally_WhenThrows_ExecutesFinallyBlock()
    {
        // Arrange
        bool finallyExecuted = false;

        // Act
        var result = Result.Try<int>(
            () => throw new InvalidOperationException("Test"),
            @finally: () => finallyExecuted = true);

        // Assert
        Assert.True(result.IsFailure);
        Assert.IsType<InvalidOperationException>(result.Error);
        Assert.Equal("Test", result.Error.Message);
        Assert.True(finallyExecuted);
    }

    [Fact]
    public void Try_ExceptionFirstWithFinally_PreservesExceptionInError()
    {
        // Arrange
        bool finallyExecuted = false;
        var expectedException = new ArgumentNullException("param", "Value cannot be null");

        // Act
        var result = Result.Try<int>(
            () => throw expectedException,
            @finally: () => finallyExecuted = true);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Same(expectedException, result.Error);
        Assert.True(finallyExecuted);
    }

    [Fact]
    public void Try_ExceptionFirstWithFinally_CanCombineWithTapError()
    {
        // Arrange
        bool finallyExecuted = false;
        bool tapErrorExecuted = false;
        Exception? caughtException = null;

        // Act
        var result = Result.Try<int>(
            () => throw new InvalidOperationException("Test"),
            @finally: () => finallyExecuted = true)
            .TapError(ex =>
            {
                tapErrorExecuted = true;
                caughtException = ex;
            });

        // Assert
        Assert.True(result.IsFailure);
        Assert.True(finallyExecuted);
        Assert.True(tapErrorExecuted);
        Assert.IsType<InvalidOperationException>(caughtException);
    }

    [Fact]
    public void Try_ExceptionFirstWithFinally_CanCombineWithMapError()
    {
        // Arrange
        bool finallyExecuted = false;

        // Act
        var result = Result.Try<int>(
            () => throw new InvalidOperationException("Original"),
            @finally: () => finallyExecuted = true)
            .MapError(ex => $"Transformed: {ex.Message}");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Transformed: Original", result.Error);
        Assert.True(finallyExecuted);
    }

    [Fact]
    public void Try_ExceptionFirstWithFinally_FinallyExecutesInCorrectOrder()
    {
        // Arrange
        var executionLog = new System.Collections.Generic.List<string>();

        // Act
        var result = Result.Try(
            () =>
            {
                executionLog.Add("operation");
                return 42;
            },
            @finally: () => executionLog.Add("finally"));

        executionLog.Add("after-return");

        // Assert
        Assert.Equal(new[] { "operation", "finally", "after-return" }, executionLog);
    }

    [Fact]
    public void Try_ExceptionFirstWithFinally_WithoutFinally_WorksAsExpected()
    {
        // Act
        var result = Result.Try(() => 42);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Try_ExceptionFirstWithFinally_RealWorld_LoggingPipeline()
    {
        // Arrange
        var logger = new Logger();

        // Act
        var result = Result.Try<int>(
            () => throw new FileNotFoundException("file.txt"),
            @finally: () => logger.RecordAttempt())
            .TapError(ex => logger.LogError(ex))
            .MapError(ex => ex switch
            {
                FileNotFoundException => "File not found",
                _ => "Unknown error"
            });

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("File not found", result.Error);
        Assert.Equal(1, logger.Attempts);
        Assert.True(logger.ErrorLogged);
    }

    [Fact]
    public void Try_ExceptionFirstWithFinally_RealWorld_ResourceCleanup()
    {
        // Arrange
        var resource = new DisposableResource();

        // Act
        var result = Result.Try(
            () =>
            {
                resource.Use();
                return resource.GetData();
            },
            @finally: () => resource.Cleanup());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.True(resource.IsCleanedUp);
    }
}