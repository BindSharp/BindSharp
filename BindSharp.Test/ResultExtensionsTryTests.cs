using BindSharp;

namespace BindSharp.Test;

/// <summary>
/// Tests for ResultExtensions Try and TryAsync methods
/// </summary>
public class ResultExtensionsTryTests
{
    #region Try Tests - Success Cases

    [Fact]
    public void Try_WithSuccessfulOperation_ReturnsSuccess()
    {
        // Arrange & Act
        var result = ResultExtensions.Try(
            () => 42,
            ex => $"Error: {ex.Message}"
        );

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Try_WithComplexOperation_ReturnsSuccess()
    {
        // Arrange
        var input = "42";

        // Act
        var result = ResultExtensions.Try(
            () => int.Parse(input),
            ex => $"Parse failed: {ex.Message}"
        );

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Try_WithObjectCreation_ReturnsSuccess()
    {
        // Arrange & Act
        var result = ResultExtensions.Try(
            () => new Person("John", 30),
            ex => $"Failed: {ex.Message}"
        );

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("John", result.Value.Name);
        Assert.Equal(30, result.Value.Age);
    }

    #endregion

    #region Try Tests - Exception Cases

    [Fact]
    public void Try_WhenOperationThrows_ReturnsFailure()
    {
        // Arrange & Act
        var result = ResultExtensions.Try(
            () => int.Parse("invalid"),
            ex => $"Parse failed: {ex.Message}"
        );

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Parse failed", result.Error);
    }

    [Fact]
    public void Try_WhenOperationThrowsSpecificException_CapturesIt()
    {
        // Arrange & Act
        var result = ResultExtensions.Try<string, string>(
            () => throw new InvalidOperationException("Test error"),
            ex => $"Operation failed: {ex.Message}"
        );

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Operation failed: Test error", result.Error);
    }

    [Fact]
    public void Try_WhenDivisionByZero_ReturnsFailure()
    {
        // Arrange & Act
        var result = ResultExtensions.Try<int, string>(
            () => GetInteger() / GetZero(),
            ex => $"Math error: {ex.Message}"
        );

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Math error", result.Error);
    }

    [Fact]
    public void Try_WhenNullReferenceException_ReturnsFailure()
    {
        // Arrange
        string? nullString = null;

        // Act
        var result = ResultExtensions.Try(
            () => nullString!.Length,
            ex => $"Null reference: {ex.Message}"
        );

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Null reference", result.Error);
    }

    #endregion

    #region Try Tests - Custom Error Types

    [Fact]
    public void Try_WithCustomErrorType_ReturnsTypedError()
    {
        // Arrange & Act
        var result = ResultExtensions.Try(
            () => int.Parse("invalid"),
            ex => new ApiError("PARSE_ERROR", ex.Message, ex)
        );

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("PARSE_ERROR", result.Error.Code);
        Assert.NotNull(result.Error.InnerException);
    }

    [Fact]
    public void Try_WithErrorEnum_ReturnsEnumError()
    {
        // Arrange & Act
        var result = ResultExtensions.Try(
            () => int.Parse("invalid"),
            ex => ErrorCode.ParseFailed
        );

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.ParseFailed, result.Error);
    }

    [Fact]
    public void Try_WithErrorFactory_TransformsException()
    {
        // Arrange
        Func<Exception, ValidationError> errorFactory = ex => 
            new ValidationError("input", $"Validation failed: {ex.Message}");

        // Act
        var result = ResultExtensions.Try(
            () => int.Parse("invalid"),
            errorFactory
        );

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("input", result.Error.Field);
        Assert.Contains("Validation failed", result.Error.Message);
    }

    #endregion

    #region TryAsync Tests - Success Cases

    [Fact]
    public async Task TryAsync_WithSuccessfulOperation_ReturnsSuccess()
    {
        // Arrange & Act
        var result = await ResultExtensions.TryAsync(
            async () => 
            {
                await Task.Delay(1);
                return 42;
            },
            ex => $"Error: {ex.Message}"
        );

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task TryAsync_WithAsyncOperation_ReturnsSuccess()
    {
        // Arrange
        var input = "42";

        // Act
        var result = await ResultExtensions.TryAsync(
            async () => await ParseIntAsync(input),
            ex => $"Parse failed: {ex.Message}"
        );

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task TryAsync_WithHttpCall_ReturnsSuccess()
    {
        // Arrange & Act
        var result = await ResultExtensions.TryAsync(
            async () => await FetchDataAsync("test"),
            ex => $"HTTP failed: {ex.Message}"
        );

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Data: test", result.Value);
    }

    #endregion

    #region TryAsync Tests - Exception Cases

    [Fact]
    public async Task TryAsync_WhenOperationThrows_ReturnsFailure()
    {
        // Arrange & Act
        var result = await ResultExtensions.TryAsync(
            async () => 
            {
                await Task.Delay(1);
                return int.Parse("invalid");
            },
            ex => $"Parse failed: {ex.Message}"
        );

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Parse failed", result.Error);
    }

    [Fact]
    public async Task TryAsync_WhenAsyncOperationThrows_ReturnsFailure()
    {
        // Arrange & Act
        var result = await ResultExtensions.TryAsync(
            async () => await ThrowExceptionAsync(),
            ex => $"Operation failed: {ex.Message}"
        );

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Operation failed: Async operation failed", result.Error);
    }

    [Fact]
    public async Task TryAsync_WhenTaskCanceled_ReturnsFailure()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await ResultExtensions.TryAsync(
            async () => 
            {
                await Task.Delay(1000, cts.Token);
                return 42;
            },
            ex => $"Canceled: {ex.GetType().Name}"
        );

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Canceled", result.Error);
    }

    #endregion

    #region TryAsync Tests - Custom Error Types

    [Fact]
    public async Task TryAsync_WithCustomErrorType_ReturnsTypedError()
    {
        // Arrange & Act
        var result = await ResultExtensions.TryAsync(
            async () => 
            {
                await Task.Delay(1);
                return int.Parse("invalid");
            },
            ex => new ApiError("ASYNC_PARSE_ERROR", ex.Message, ex)
        );

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("ASYNC_PARSE_ERROR", result.Error.Code);
        Assert.NotNull(result.Error.InnerException);
    }

    [Fact]
    public async Task TryAsync_WithErrorEnum_ReturnsEnumError()
    {
        // Arrange & Act
        var result = await ResultExtensions.TryAsync(
            async () => await ThrowExceptionAsync(),
            ex => ErrorCode.OperationFailed
        );

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.OperationFailed, result.Error);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Try_InPipeline_WorksCorrectly()
    {
        // Arrange
        var input = "42";

        // Act
        var result = ResultExtensions.Try(
                () => int.Parse(input),
                ex => $"Parse failed: {ex.Message}"
            )
            .Map(x => x * 2)
            .Bind(x => x > 0 
                ? Result<int, string>.Success(x) 
                : Result<int, string>.Failure("Must be positive"));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(84, result.Value);
    }

    [Fact]
    public async Task TryAsync_InPipeline_WorksCorrectly()
    {
        // Arrange
        var input = "42";

        // Act
        var result = await ResultExtensions.TryAsync(
                async () => await ParseIntAsync(input),
                ex => $"Parse failed: {ex.Message}"
            )
            .MapAsync(async x => await MultiplyAsync(x, 2))
            .BindAsync(async x => x > 0 
                ? Result<int, string>.Success(x) 
                : Result<int, string>.Failure("Must be positive"));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(84, result.Value);
    }

    [Fact]
    public void Try_WithMultipleExceptionTypes_CatchesAll()
    {
        // Arrange & Act
        var parseResult = ResultExtensions.Try(
            () => int.Parse("invalid"),
            ex => $"Error: {ex.GetType().Name}"
        );

        var divideResult = ResultExtensions.Try(
            () => GetInteger() / GetZero(),
            ex => $"Error: {ex.GetType().Name}"
        );

        var nullRefResult = ResultExtensions.Try(
            () => ((string)null!).Length,
            ex => $"Error: {ex.GetType().Name}"
        );

        // Assert
        Assert.True(parseResult.IsFailure);
        Assert.Contains("FormatException", parseResult.Error);

        Assert.True(divideResult.IsFailure);
        Assert.Contains("DivideByZeroException", divideResult.Error);

        Assert.True(nullRefResult.IsFailure);
        Assert.Contains("NullReferenceException", nullRefResult.Error);
    }

    #endregion

    #region Real-World Scenarios

    [Fact]
    public async Task TryAsync_ApiCall_ReturnsSuccess()
    {
        // Arrange
        var url = "https://api.example.com/users/42";

        // Act
        var result = await ResultExtensions.TryAsync(
            async () => await FetchDataAsync(url),
            ex => $"API call failed: {ex.Message}"
        );

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains("Data", result.Value);
    }

    [Fact]
    public void Try_JsonDeserialization_HandlesInvalidJson()
    {
        // Arrange
        var invalidJson = "{invalid json}";

        // Act
        var result = ResultExtensions.Try(
            () => System.Text.Json.JsonSerializer.Deserialize<Person>(invalidJson),
            ex => new ApiError("JSON_PARSE_ERROR", "Invalid JSON format", ex)
        );

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("JSON_PARSE_ERROR", result.Error.Code);
    }

    [Fact]
    public void Try_FileOperation_HandlesException()
    {
        // Arrange
        var invalidPath = "C:\\nonexistent\\file.txt";

        // Act
        var result = ResultExtensions.Try(
            () => System.IO.File.ReadAllText(invalidPath),
            ex => $"File read failed: {ex.Message}"
        );

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("File read failed", result.Error);
    }

    #endregion

    #region Test Helpers

    private record Person(string Name, int Age);
    private record ApiError(string Code, string Message, Exception? InnerException = null);
    private record ValidationError(string Field, string Message);

    private enum ErrorCode
    {
        ParseFailed,
        OperationFailed
    }

    private static Task<int> ParseIntAsync(string input)
    {
        return Task.FromResult(int.Parse(input));
    }

    private static Task<int> MultiplyAsync(int value, int multiplier)
    {
        return Task.FromResult(value * multiplier);
    }

    private static Task<string> FetchDataAsync(string url)
    {
        return Task.FromResult($"Data: {url}");
    }

    private static Task<string> FetchUserAsync(int id)
    {
        return Task.FromResult($"User{id}");
    }

    private static Task LogUserAccessAsync(string user)
    {
        return Task.CompletedTask;
    }

    private static Task<int> ThrowExceptionAsync()
    {
        throw new InvalidOperationException("Async operation failed");
    }

    private static int GetInteger()
    {
        return 42;
    }

    public static int GetZero()
    {
        return 0;
    }

    #endregion
}