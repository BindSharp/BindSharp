namespace BindSharp.Test;

/// <summary>
/// Tests for AsyncFunctionalResult extension methods
/// </summary>
public class AsyncFunctionalResultTests
{
    #region MapAsync Tests - Task{Result} + Sync Function

    [Fact]
    public async Task MapAsync_TaskResultWithSyncMap_TransformsValue()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int, string>.Success(5));

        // Act
        var mapped = await resultTask.MapAsync(x => x * 2);

        // Assert
        Assert.True(mapped.IsSuccess);
        Assert.Equal(10, mapped.Value);
    }

    [Fact]
    public async Task MapAsync_TaskResultWithSyncMap_OnFailure_PropagatesError()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int, string>.Failure("Error"));

        // Act
        var mapped = await resultTask.MapAsync(x => x * 2);

        // Assert
        Assert.True(mapped.IsFailure);
        Assert.Equal("Error", mapped.Error);
    }

    #endregion

    #region MapAsync Tests - Result + Async Function

    [Fact]
    public async Task MapAsync_ResultWithAsyncMap_TransformsValue()
    {
        // Arrange
        var result = Result<int, string>.Success(5);

        // Act
        var mapped = await result.MapAsync(async x => 
        {
            await Task.Delay(1);
            return x * 2;
        });

        // Assert
        Assert.True(mapped.IsSuccess);
        Assert.Equal(10, mapped.Value);
    }

    [Fact]
    public async Task MapAsync_ResultWithAsyncMap_OnFailure_PropagatesError()
    {
        // Arrange
        var result = Result<int, string>.Failure("Error");

        // Act
        var mapped = await result.MapAsync(async x => 
        {
            await Task.Delay(1);
            return x * 2;
        });

        // Assert
        Assert.True(mapped.IsFailure);
        Assert.Equal("Error", mapped.Error);
    }

    #endregion

    #region MapAsync Tests - Task{Result} + Async Function

    [Fact]
    public async Task MapAsync_TaskResultWithAsyncMap_TransformsValue()
    {
        // Arrange
        var resultTask = GetValueAsync(5);

        // Act
        var mapped = await resultTask.MapAsync(async x => 
        {
            await Task.Delay(1);
            return x * 2;
        });

        // Assert
        Assert.True(mapped.IsSuccess);
        Assert.Equal(10, mapped.Value);
    }

    [Fact]
    public async Task MapAsync_ChainedAsyncTransformations_WorksCorrectly()
    {
        // Arrange
        var resultTask = GetValueAsync(5);

        // Act
        var mapped = await resultTask
            .MapAsync(async x => await MultiplyAsync(x, 2))  // 10
            .MapAsync(async x => await AddAsync(x, 5))       // 15
            .MapAsync(x => x.ToString());                     // "15"

        // Assert
        Assert.True(mapped.IsSuccess);
        Assert.Equal("15", mapped.Value);
    }

    #endregion

    #region BindAsync Tests - Task{Result} + Sync Bind

    [Fact]
    public async Task BindAsync_TaskResultWithSyncBind_ChainsOperation()
    {
        // Arrange
        var resultTask = GetValueAsync(5);

        // Act
        var bound = await resultTask.BindAsync(x => 
            x > 0 
                ? Result<int, string>.Success(x * 2) 
                : Result<int, string>.Failure("Must be positive")
        );

        // Assert
        Assert.True(bound.IsSuccess);
        Assert.Equal(10, bound.Value);
    }

    [Fact]
    public async Task BindAsync_TaskResultWithSyncBind_OnFailure_PropagatesError()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int, string>.Failure("Initial error"));

        // Act
        var bound = await resultTask.BindAsync(x => Result<int, string>.Success(x * 2));

        // Assert
        Assert.True(bound.IsFailure);
        Assert.Equal("Initial error", bound.Error);
    }

    #endregion

    #region BindAsync Tests - Result + Async Bind

    [Fact]
    public async Task BindAsync_ResultWithAsyncBind_ChainsAsyncOperation()
    {
        // Arrange
        var result = Result<int, string>.Success(5);

        // Act
        var bound = await result.BindAsync(async x => 
        {
            await Task.Delay(1);
            return x > 0 
                ? Result<int, string>.Success(x * 2) 
                : Result<int, string>.Failure("Must be positive");
        });

        // Assert
        Assert.True(bound.IsSuccess);
        Assert.Equal(10, bound.Value);
    }

    [Fact]
    public async Task BindAsync_ResultWithAsyncBind_WhenOperationFails_ReturnsError()
    {
        // Arrange
        var result = Result<int, string>.Success(-5);

        // Act
        var bound = await result.BindAsync(async x => 
        {
            await Task.Delay(1);
            return x > 0 
                ? Result<int, string>.Success(x * 2) 
                : Result<int, string>.Failure("Must be positive");
        });

        // Assert
        Assert.True(bound.IsFailure);
        Assert.Equal("Must be positive", bound.Error);
    }

    #endregion

    #region BindAsync Tests - Task{Result} + Async Bind

    [Fact]
    public async Task BindAsync_TaskResultWithAsyncBind_ChainsAsyncOperations()
    {
        // Arrange
        var resultTask = GetValueAsync(5);

        // Act
        var bound = await resultTask.BindAsync(async x => 
        {
            await Task.Delay(1);
            return Result<int, string>.Success(x * 2);
        });

        // Assert
        Assert.True(bound.IsSuccess);
        Assert.Equal(10, bound.Value);
    }

    [Fact]
    public async Task BindAsync_ComplexChain_WorksCorrectly()
    {
        // Arrange
        var email = "user@example.com";

        // Act
        var result = await GetEmailAsync(email)
            .BindAsync(async e => await ValidateEmailAsync(e))
            .BindAsync(async e => await CheckEmailAvailabilityAsync(e))
            .BindAsync(async e => await CreateUserAsync(e));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(email, result.Value.Email);
    }

    #endregion

    #region MapErrorAsync Tests

    [Fact]
    public async Task MapErrorAsync_TaskResultWithSyncMap_TransformsError()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int, string>.Failure("404"));

        // Act
        var mapped = await resultTask.MapErrorAsync(int.Parse);

        // Assert
        Assert.True(mapped.IsFailure);
        Assert.Equal(404, mapped.Error);
    }

    [Fact]
    public async Task MapErrorAsync_ResultWithAsyncMap_TransformsError()
    {
        // Arrange
        var result = Result<int, string>.Failure("404");

        // Act
        var mapped = await result.MapErrorAsync(async error => 
        {
            await Task.Delay(1);
            return int.Parse(error);
        });

        // Assert
        Assert.True(mapped.IsFailure);
        Assert.Equal(404, mapped.Error);
    }

    [Fact]
    public async Task MapErrorAsync_TaskResultWithAsyncMap_TransformsError()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int, string>.Failure("Error"));

        // Act
        var mapped = await resultTask.MapErrorAsync(async error => 
        {
            await Task.Delay(1);
            return new ApiError("API_ERROR", error);
        });

        // Assert
        Assert.True(mapped.IsFailure);
        Assert.Equal("API_ERROR", mapped.Error.Code);
        Assert.Equal("Error", mapped.Error.Message);
    }

    [Fact]
    public async Task MapErrorAsync_OnSuccess_PreservesValue()
    {
        // Arrange
        var resultTask = GetValueAsync(42);

        // Act
        var mapped = await resultTask.MapErrorAsync(async error => 
        {
            await Task.Delay(1);
            return new ApiError("ERROR", error);
        });

        // Assert
        Assert.True(mapped.IsSuccess);
        Assert.Equal(42, mapped.Value);
    }

    #endregion

    #region MatchAsync Tests - Various Overloads

    [Fact]
    public async Task MatchAsync_TaskResultWithSyncHandlers_WorksCorrectly()
    {
        // Arrange
        var resultTask = GetValueAsync(42);

        // Act
        var output = await resultTask.MatchAsync(
            success => $"Value: {success}",
            error => $"Error: {error}"
        );

        // Assert
        Assert.Equal("Value: 42", output);
    }

    [Fact]
    public async Task MatchAsync_ResultWithAsyncSuccessHandler_WorksCorrectly()
    {
        // Arrange
        var result = Result<int, string>.Success(42);

        // Act
        var output = await result.MatchAsync(
            async success => 
            {
                await Task.Delay(1);
                return $"Value: {success}";
            },
            error => $"Error: {error}"
        );

        // Assert
        Assert.Equal("Value: 42", output);
    }

    [Fact]
    public async Task MatchAsync_ResultWithAsyncFailureHandler_WorksCorrectly()
    {
        // Arrange
        var result = Result<int, string>.Failure("Something went wrong");

        // Act
        var output = await result.MatchAsync(
            success => $"Value: {success}",
            async error => 
            {
                await Task.Delay(1);
                return $"Error: {error}";
            }
        );

        // Assert
        Assert.Equal("Error: Something went wrong", output);
    }

    [Fact]
    public async Task MatchAsync_ResultWithBothAsyncHandlers_WorksCorrectly()
    {
        // Arrange
        var result = Result<int, string>.Success(42);

        // Act
        var output = await result.MatchAsync(
            async success => 
            {
                await Task.Delay(1);
                return $"Value: {success}";
            },
            async error => 
            {
                await Task.Delay(1);
                return $"Error: {error}";
            }
        );

        // Assert
        Assert.Equal("Value: 42", output);
    }

    [Fact]
    public async Task MatchAsync_TaskResultWithAsyncSuccessHandler_WorksCorrectly()
    {
        // Arrange
        var resultTask = GetValueAsync(42);

        // Act
        var output = await resultTask.MatchAsync(
            async success => 
            {
                await Task.Delay(1);
                return $"Value: {success}";
            },
            error => $"Error: {error}"
        );

        // Assert
        Assert.Equal("Value: 42", output);
    }

    [Fact]
    public async Task MatchAsync_TaskResultWithBothAsyncHandlers_WorksCorrectly()
    {
        // Arrange
        var resultTask = GetValueAsync(42);

        // Act
        var output = await resultTask.MatchAsync(
            async success => 
            {
                await Task.Delay(1);
                return $"Value: {success}";
            },
            async error => 
            {
                await Task.Delay(1);
                return $"Error: {error}";
            }
        );

        // Assert
        Assert.Equal("Value: 42", output);
    }

    #endregion

    #region Complete Workflow Tests

    [Fact]
    public async Task CompleteAsyncWorkflow_WithAllOperations_WorksCorrectly()
    {
        // Arrange
        var userId = 42;

        // Act
        var result = await GetUserIdAsync(userId)
            .MapAsync(async id => await FetchUserAsync(id))
            .BindAsync(async user => await ValidateUserAsync(user.Value))
            .MapAsync(async user => await EnrichUserDataAsync(user))
            .MapErrorAsync(async error => await LogErrorAsync(error))
            .MatchAsync(
                async user => await FormatSuccessAsync(user),
                error => $"Failed: {error}"
            );

        // Assert
        Assert.Contains("Success", result);
        Assert.Contains("User42", result);
    }

    [Fact]
    public async Task AsyncWorkflow_StopsAtFirstFailure()
    {
        // Arrange
        var userId = -1; // Invalid ID

        // Act
        var result = await GetUserIdAsync(userId)
            .BindAsync(async id => await FetchUserAsync(id))
            .BindAsync(async user => await ValidateUserAsync(user))
            .MapAsync(async user => await EnrichUserDataAsync(user));

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("User ID must be positive", result.Error);
    }

    [Fact]
    public async Task AsyncWorkflow_WithMixedSyncAndAsync_WorksCorrectly()
    {
        // Arrange
        var email = "user@example.com";

        // Act
        var result = await GetEmailAsync(email)
            .MapAsync(e => e.ToLower())  // Sync map
            .BindAsync(async e => await ValidateEmailAsync(e))  // Async bind
            .MapAsync(e => e.Split('@')[0])  // Sync map
            .BindAsync(async username => await CreateUserAsync(username + "@example.com"));  // Async bind

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("user@example.com", result.Value.Email);
    }

    #endregion

    #region Test Helpers

    private record User(string Email);
    private record UserData(string Email, string Name);
    private record ApiError(string Code, string Message);

    private static Task<Result<int, string>> GetValueAsync(int value) =>
        Task.FromResult(Result<int, string>.Success(value));

    private static async Task<int> MultiplyAsync(int value, int multiplier)
    {
        await Task.Delay(1);
        return value * multiplier;
    }

    private static async Task<int> AddAsync(int value, int addend)
    {
        await Task.Delay(1);
        return value + addend;
    }

    private static Task<Result<string, string>> GetEmailAsync(string email) =>
        Task.FromResult(Result<string, string>.Success(email));

    private static async Task<Result<string, string>> ValidateEmailAsync(string email)
    {
        await Task.Delay(1);
        return email.Contains("@")
            ? Result<string, string>.Success(email)
            : Result<string, string>.Failure("Invalid email format");
    }

    private static async Task<Result<string, string>> CheckEmailAvailabilityAsync(string email)
    {
        await Task.Delay(1);
        return Result<string, string>.Success(email);
    }

    private static async Task<Result<User, string>> CreateUserAsync(string email)
    {
        await Task.Delay(1);
        return Result<User, string>.Success(new User(email));
    }

    private static Task<Result<int, string>> GetUserIdAsync(int id) =>
        id > 0
            ? Task.FromResult(Result<int, string>.Success(id))
            : Task.FromResult(Result<int, string>.Failure("User ID must be positive"));

    private static async Task<Result<UserData, string>> FetchUserAsync(int id)
    {
        await Task.Delay(1);
        return Result<UserData, string>.Success(new UserData($"user{id}@example.com", $"User{id}"));
    }

    private static async Task<Result<UserData, string>> ValidateUserAsync(UserData user)
    {
        await Task.Delay(1);
        return !string.IsNullOrEmpty(user.Email)
            ? Result<UserData, string>.Success(user)
            : Result<UserData, string>.Failure("User email is required");
    }

    private static async Task<UserData> EnrichUserDataAsync(UserData user)
    {
        await Task.Delay(1);
        return user;
    }

    private static async Task<string> LogErrorAsync(string error)
    {
        await Task.Delay(1);
        return error;
    }

    private static async Task<string> FormatSuccessAsync(UserData user)
    {
        await Task.Delay(1);
        return $"Success: {user.Name} ({user.Email})";
    }

    #endregion
}