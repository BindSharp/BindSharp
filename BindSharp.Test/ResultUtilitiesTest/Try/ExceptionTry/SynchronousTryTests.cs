using BindSharp.Extensions;

namespace BindSharp.Test.ResultUtilitiesTest.Try.ExceptionTry;

public sealed class SynchronousTryTests
{
    [Fact]
    public void Try_WithSuccessfulOperation_ReturnsSuccess()
    {
        // Arrange
        const int expected = 42;

        // Act
        var result = Result.Try(() => expected);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public void Try_WithFailingOperation_ReturnsFailureWithException()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Test error");

        // Act
        var result = Result.Try<int>(() => throw expectedException);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Same(expectedException, result.Error);
    }

    [Fact]
    public void Try_WithFailingOperation_PreservesExceptionType()
    {
        // Act
        var result = Result.Try<string>(() => 
            throw new FileNotFoundException("File not found", "test.txt"));

        // Assert
        Assert.True(result.IsFailure);
        Assert.IsType<FileNotFoundException>(result.Error);
        var fileNotFound = (FileNotFoundException)result.Error;
        Assert.Equal("test.txt", fileNotFound.FileName);
    }

    [Fact]
    public void Try_WithTapError_AllowsLoggingException()
    {
        // Arrange
        Exception? capturedEx = null;
        var expectedException = new InvalidOperationException("Test");

        // Act
        var result = Result.Try<int>(() => throw expectedException)
            .TapError(ex => capturedEx = ex);

        // Assert
        Assert.Same(expectedException, capturedEx);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Try_WithMapError_AllowsTransformingToCustomError()
    {
        // Act
        var result = Result.Try<int>(() => 
                throw new InvalidOperationException("Original"))
            .MapError(ex => $"Custom: {ex.Message}");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Custom: Original", result.Error);
    }

    [Fact]
    public void Try_WithTapErrorAndMapError_AllowsFullPipeline()
    {
        // Arrange
        Exception? loggedEx = null;
        var expectedException = new FileNotFoundException("Missing file");

        // Act
        var result = Result.Try<string>(() => throw expectedException)
            .TapError(ex => loggedEx = ex)
            .MapError(ex => ex switch
            {
                FileNotFoundException => "File not found",
                UnauthorizedAccessException => "Permission denied",
                _ => "Unknown error"
            });

        // Assert
        Assert.Same(expectedException, loggedEx);
        Assert.True(result.IsFailure);
        Assert.Equal("File not found", result.Error);
    }

    [Fact]
    public void Try_WithPatternMatching_AllowsSpecificExceptionHandling()
    {
        // Arrange
        string? handledMessage = null;

        // Act
        var result = Result.Try<int>(() => 
                throw new UnauthorizedAccessException("Access denied"))
            .TapError(ex =>
            {
                handledMessage = ex switch
                {
                    FileNotFoundException fnf => $"Missing: {fnf.FileName}",
                    UnauthorizedAccessException uae => $"Denied: {uae.Message}",
                    _ => "Other error"
                };
            });

        // Assert
        Assert.Equal("Denied: Access denied", handledMessage);
        Assert.True(result.IsFailure);
    }
}