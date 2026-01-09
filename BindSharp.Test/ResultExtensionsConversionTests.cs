using BindSharp.Extensions;

namespace BindSharp.Test;

/// <summary>
/// Tests for Result conversion methods (ToResult and AsTask)
/// </summary>
public class ResultExtensionsConversionTests
{
    #region ToResult Tests - Success Cases

    [Fact]
    public void ToResult_WithNonNullValue_ReturnsSuccess()
    {
        // Arrange
        string value = "Hello";

        // Act
        var result = value.ToResult("Value is null");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Hello", result.Value);
    }

    [Fact]
    public void ToResult_WithNonNullComplexType_ReturnsSuccess()
    {
        // Arrange
        var person = new Person("John", 30);

        // Act
        var result = person.ToResult("Person not found");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("John", result.Value.Name);
        Assert.Equal(30, result.Value.Age);
    }

    [Theory]
    [InlineData("test")]
    [InlineData("")]
    [InlineData(" ")]
    public void ToResult_WithVariousStrings_ReturnsSuccess(string value)
    {
        // Act
        var result = value.ToResult("String is null");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(value, result.Value);
    }

    #endregion

    #region ToResult Tests - Failure Cases

    [Fact]
    public void ToResult_WithNullValue_ReturnsFailure()
    {
        // Arrange
        string? value = null;

        // Act
        var result = value.ToResult("Value is null");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Value is null", result.Error);
    }

    [Fact]
    public void ToResult_WithNullComplexType_ReturnsFailure()
    {
        // Arrange
        Person? person = null;

        // Act
        var result = person.ToResult("Person not found");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Person not found", result.Error);
    }

    #endregion

    #region ToResult Tests - Custom Error Types

    [Fact]
    public void ToResult_WithCustomErrorType_ReturnsTypedError()
    {
        // Arrange
        string? value = null;

        // Act
        var result = value.ToResult(new ValidationError("value", "Value cannot be null"));

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("value", result.Error.Field);
        Assert.Equal("Value cannot be null", result.Error.Message);
    }

    [Fact]
    public void ToResult_WithErrorEnum_ReturnsEnumError()
    {
        // Arrange
        User? user = null;

        // Act
        var result = user.ToResult(ErrorCode.NotFound);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.NotFound, result.Error);
    }

    #endregion

    #region ToResult Tests - Integration

    [Fact]
    public void ToResult_InPipeline_WorksCorrectly()
    {
        // Arrange
        string? cachedValue = "42";

        // Act
        var result = cachedValue
            .ToResult("Not found in cache")
            .Bind(s => Result.Try(
                () => int.Parse(s),
                ex => "Invalid number format"
            ))
            .Map(x => x * 2);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(84, result.Value);
    }

    [Fact]
    public void ToResult_InPipeline_WithNull_StopsEarly()
    {
        // Arrange
        string? cachedValue = null;

        // Act
        var result = cachedValue
            .ToResult("Not found in cache")
            .Bind(s => Result.Try(
                () => int.Parse(s),
                ex => "Invalid number format"
            ))
            .Map(x => x * 2);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Not found in cache", result.Error);
    }

    [Fact]
    public void ToResult_ChainedWithEnsure_WorksCorrectly()
    {
        // Arrange
        string? email = "user@example.com";

        // Act
        var result = email
            .ToResult("Email is required")
            .Ensure(e => e.Contains("@"), "Invalid email format")
            .Ensure(e => e.Length >= 5, "Email too short");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("user@example.com", result.Value);
    }

    #endregion

    #region Real-World ToResult Scenarios

    [Fact]
    public void ToResult_CacheScenario_WorksCorrectly()
    {
        // Arrange
        var cache = new FakeCache();
        cache.Set("user:42", "John Doe");

        // Act
        var result = cache.Get("user:42")
            .ToResult("User not found in cache")
            .Map(name => new User(name));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("John Doe", result.Value.Name);
    }

    [Fact]
    public void ToResult_CacheScenario_MissingKey_ReturnsFailure()
    {
        // Arrange
        var cache = new FakeCache();

        // Act
        var result = cache.Get("user:999")
            .ToResult("User not found in cache");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("User not found in cache", result.Error);
    }

    [Fact]
    public void ToResult_SessionScenario_WorksCorrectly()
    {
        // Arrange
        var session = new FakeSession();
        session.Set("current_user", "user@example.com");

        // Act
        var result = session.Get("current_user")
            .ToResult("No user in session")
            .Ensure(email => email.Contains("@"), "Invalid session data");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("user@example.com", result.Value);
    }

    [Fact]
    public void ToResult_DictionaryLookup_WorksCorrectly()
    {
        // Arrange
        var config = new Dictionary<string, string>
        {
            ["database:host"] = "localhost",
            ["database:port"] = "5432"
        };

        // Act
        var hostResult = config.GetValueOrDefault("database:host")
            .ToResult("Database host not configured");

        var portResult = config.GetValueOrDefault("database:port")
            .ToResult("Database port not configured");

        var missingResult = config.GetValueOrDefault("database:password")
            .ToResult("Database password not configured");

        // Assert
        Assert.True(hostResult.IsSuccess);
        Assert.Equal("localhost", hostResult.Value);
        Assert.True(portResult.IsSuccess);
        Assert.True(missingResult.IsFailure);
    }

    #endregion

    #region AsTask Tests

    [Fact]
    public async Task AsTask_WithSuccessfulResult_ReturnsTaskResult()
    {
        // Arrange
        var result = Result<int, string>.Success(42);

        // Act
        var taskResult = result.AsTask();
        var awaited = await taskResult;

        // Assert
        Assert.True(awaited.IsSuccess);
        Assert.Equal(42, awaited.Value);
    }

    [Fact]
    public async Task AsTask_WithFailedResult_ReturnsTaskResult()
    {
        // Arrange
        var result = Result<int, string>.Failure("Error");

        // Act
        var taskResult = result.AsTask();
        var awaited = await taskResult;

        // Assert
        Assert.True(awaited.IsFailure);
        Assert.Equal("Error", awaited.Error);
    }

    [Fact]
    public async Task AsTask_InAsyncPipeline_WorksCorrectly()
    {
        // Arrange
        var result = Result<int, string>.Success(42);

        // Act
        var final = await result
            .AsTask()
            .MapAsync(x => x * 2)
            .BindAsync(async x => 
            {
                await Task.Delay(1);
                return Result<int, string>.Success(x + 10);
            });

        // Assert
        Assert.True(final.IsSuccess);
        Assert.Equal(94, final.Value);
    }

    #endregion

    #region Real-World AsTask Scenarios

    [Fact]
    public async Task AsTask_FastPathCacheHit_WorksCorrectly()
    {
        // Arrange
        var cache = new FakeCache();
        cache.Set("user:42", "John Doe");

        // Act
        var result = await GetUserAsync(42, cache);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("John Doe", result.Value);
    }

    [Fact]
    public async Task AsTask_FastPathValidation_WorksCorrectly()
    {
        // Arrange
        var validInput = 42;
        var invalidInput = -1;

        // Act
        var validResult = await ProcessInputAsync(validInput);
        var invalidResult = await ProcessInputAsync(invalidInput);

        // Assert
        Assert.True(validResult.IsSuccess);
        Assert.Equal(84, validResult.Value);
        Assert.True(invalidResult.IsFailure);
        Assert.Equal("Input must be positive", invalidResult.Error);
    }

    [Fact]
    public async Task AsTask_ConditionalAsync_WorksCorrectly()
    {
        // Arrange
        var useCache = true;

        // Act
        var result = useCache
            ? Result<string, string>.Success("Cached value").AsTask()
            : FetchFromDatabaseAsync();

        var awaited = await result;

        // Assert
        Assert.True(awaited.IsSuccess);
        Assert.Equal("Cached value", awaited.Value);
    }

    #endregion

    #region Combined ToResult and AsTask

    [Fact]
    public async Task ToResult_AsTask_Combination_WorksCorrectly()
    {
        // Arrange
        var cache = new FakeCache();
        cache.Set("config", "value");

        // Act
        var result = await cache.Get("config")
            .ToResult("Config not found")
            .AsTask()
            .MapAsync(async config => 
            {
                await Task.Delay(1);
                return config.ToUpper();
            })
            .EnsureAsync(config => config.Length > 0, "Empty config");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("VALUE", result.Value);
    }

    [Fact]
    public async Task CompleteWorkflow_WithAllConversions_WorksCorrectly()
    {
        // Arrange
        var cache = new FakeCache();
        cache.Set("user_id", "42");

        // Act
        var result = await cache.Get("user_id")
            .ToResult("User ID not in cache")
            .Bind(s => Result.Try(
                () => int.Parse(s),
                ex => $"Invalid user ID: {ex.Message}"
            ))
            .Ensure(id => id > 0, "User ID must be positive")
            .AsTask()
            .BindAsync(async id => await FetchUserFromDbAsync(id))
            .EnsureNotNullAsync("User not found in database")
            .MapAsync(user => user.ToUpper());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("USER42", result.Value);
    }

    #endregion

    #region Test Helpers

    private record Person(string Name, int Age);
    private record User(string Name);
    private record ValidationError(string Field, string Message);

    private enum ErrorCode
    {
        NotFound,
        InvalidFormat
    }

    private class FakeCache
    {
        private readonly Dictionary<string, string> _cache = new();

        public void Set(string key, string value) => _cache[key] = value;

        public string? Get(string key) => _cache.GetValueOrDefault(key);
    }

    private class FakeSession
    {
        private readonly Dictionary<string, string> _session = new();

        public void Set(string key, string value) => _session[key] = value;

        public string? Get(string key) => _session.GetValueOrDefault(key);
    }

    private static async Task<Result<string, string>> GetUserAsync(int id, FakeCache cache)
    {
        // Fast path: check cache
        var cached = cache.Get($"user:{id}");
        if (cached != null)
            return Result<string, string>.Success(cached).AsTask().Result;

        // Slow path: fetch from database
        await Task.Delay(10);
        return Result<string, string>.Success($"User{id}");
    }

    private static Task<Result<int, string>> ProcessInputAsync(int input)
    {
        // Fast path: validation
        if (input <= 0)
            return Result<int, string>.Failure("Input must be positive").AsTask();

        // Slow path: process
        return Task.FromResult(Result<int, string>.Success(input * 2));
    }

    private static async Task<Result<string, string>> FetchFromDatabaseAsync()
    {
        await Task.Delay(10);
        return Result<string, string>.Success("Database value");
    }

    private static async Task<Result<string?, string>> FetchUserFromDbAsync(int id)
    {
        await Task.Delay(1);
        return Result<string?, string>.Success($"User{id}");
    }

    #endregion
}