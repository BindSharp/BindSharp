namespace BindSharp.Test.ResultUtilitiesTest.Try.ErrorFactory;

public sealed class SynchronousTryTests
{
    [Fact]
    public void Try_WithErrorFactory_WhenOperationSucceeds_ReturnsSuccess()
    {
        // Arrange
        const int expected = 42;

        // Act
        var result = Result.Try(
            () => expected,
            ex => $"Error: {ex.Message}");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public void Try_WithErrorFactory_WhenOperationThrows_ReturnsFailureWithTransformedError()
    {
        // Arrange
        var exception = new InvalidOperationException("Original message");

        // Act
        var result = Result.Try<bool, string>(
            () => throw exception,
            ex => $"Custom error: {ex.Message}");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Custom error: Original message", result.Error);
    }

    [Fact]
    public void Try_WithErrorFactory_PassesCorrectExceptionToFactory()
    {
        // Arrange
        var expectedException = new ArgumentNullException("paramName", "Value cannot be null");
        Exception? capturedEx = null;

        // Act
        var result = Result.Try<bool, string>(
            () => throw expectedException,
            ex =>
            {
                capturedEx = ex;
                return "error";
            });

        // Assert
        Assert.Same(expectedException, capturedEx);
        Assert.IsType<ArgumentNullException>(capturedEx);
    }

    [Fact]
    public void Try_WithErrorFactory_CanTransformToDifferentErrorType()
    {
        // Act
        var result = Result.Try(
            () => int.Parse("invalid"),
            ex => ex.GetType().Name);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("FormatException", result.Error);
    }

    [Fact]
    public void Try_WithErrorFactory_CanReturnComplexErrorObject()
    {
        // Arrange
        var errorInfo = new { Code = 500, Message = "Internal error" };

        // Act
        var result = Result.Try<bool, Response>(
            () => throw new InvalidOperationException("Test"),
            ex => new Response { Code = 500, Message = ex.Message });

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(500, result.Error.Code);
        Assert.Equal("Test", result.Error.Message);
    }

    [Fact]
    public void Try_WithErrorFactory_WhenSuccessful_DoesNotCallErrorFactory()
    {
        // Arrange
        bool errorFactoryCalled = false;

        // Act
        var result = Result.Try(
            () => 42,
            ex =>
            {
                errorFactoryCalled = true;
                return "error";
            });

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(errorFactoryCalled);
    }

    [Fact]
    public void Try_WithErrorFactory_CanPatternMatchOnExceptionType()
    {
        // Act
        var result1 = Result.Try<bool, string>(
            () => throw new FileNotFoundException("Missing file"),
            ex => ex switch
            {
                FileNotFoundException => "FILE_NOT_FOUND",
                UnauthorizedAccessException => "UNAUTHORIZED",
                _ => "UNKNOWN_ERROR"
            });

        var result2 = Result.Try<bool, string>(
            () => throw new UnauthorizedAccessException("No access"),
            ex => ex switch
            {
                FileNotFoundException => "FILE_NOT_FOUND",
                UnauthorizedAccessException => "UNAUTHORIZED",
                _ => "UNKNOWN_ERROR"
            });

        // Assert
        Assert.Equal("FILE_NOT_FOUND", result1.Error);
        Assert.Equal("UNAUTHORIZED", result2.Error);
    }

    [Fact]
    public void Try_WithErrorFactory_CanAccessExceptionProperties()
    {
        // Act
        var result = Result.Try<bool, string>(
            () => throw new FileNotFoundException("The file was not found", "config.json"),
            ex =>
            {
                if (ex is FileNotFoundException fnf)
                    return $"Cannot find {fnf.FileName}: {fnf.Message}";
                return ex.Message;
            });

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("config.json", result.Error);
        Assert.Contains("The file was not found", result.Error);
    }
    
    private class Response
    {
        public int Code { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}