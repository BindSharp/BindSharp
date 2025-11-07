namespace BindSharp.Test;

/// <summary>
/// Tests for FunctionalResult extension methods (Map, Bind, MapError, Match)
/// </summary>
public class FunctionalResultTests
{
    #region Map Tests

    [Fact]
    public void Map_OnSuccessfulResult_TransformsValue()
    {
        // Arrange
        var result = Result<int, string>.Success(5);

        // Act
        var mapped = result.Map(x => x * 2);

        // Assert
        Assert.True(mapped.IsSuccess);
        Assert.Equal(10, mapped.Value);
    }

    [Fact]
    public void Map_OnFailedResult_PropagatesError()
    {
        // Arrange
        var result = Result<int, string>.Failure("Error");

        // Act
        var mapped = result.Map(x => x * 2);

        // Assert
        Assert.True(mapped.IsFailure);
        Assert.Equal("Error", mapped.Error);
    }

    [Fact]
    public void Map_ChainedTransformations_WorksCorrectly()
    {
        // Arrange
        var result = Result<int, string>.Success(5);

        // Act
        var mapped = result
            .Map(x => x * 2)      // 10
            .Map(x => x + 5)      // 15
            .Map(x => x.ToString()); // "15"

        // Assert
        Assert.True(mapped.IsSuccess);
        Assert.Equal("15", mapped.Value);
    }

    [Fact]
    public void Map_ChangesValueType_PreservesErrorType()
    {
        // Arrange
        var result = Result<int, string>.Success(42);

        // Act
        var mapped = result.Map(x => new Person($"User{x}", x));

        // Assert
        Assert.True(mapped.IsSuccess);
        Assert.Equal("User42", mapped.Value.Name);
        Assert.Equal(42, mapped.Value.Age);
    }

    [Fact]
    public void Map_WithNullTransformation_CreatesSuccessWithNull()
    {
        // Arrange
        var result = Result<int, string>.Success(42);

        // Act
        var mapped = result.Map<int, string?, string>(_ => null);

        // Assert
        Assert.True(mapped.IsSuccess);
        Assert.Null(mapped.Value);
    }

    #endregion

    #region Bind Tests

    [Fact]
    public void Bind_OnSuccessfulResult_ChainsOperation()
    {
        // Arrange
        var result = Result<int, string>.Success(5);

        // Act
        var bound = result.Bind(x => 
            x > 0 
                ? Result<int, string>.Success(x * 2) 
                : Result<int, string>.Failure("Value must be positive")
        );

        // Assert
        Assert.True(bound.IsSuccess);
        Assert.Equal(10, bound.Value);
    }

    [Fact]
    public void Bind_OnFailedResult_PropagatesError()
    {
        // Arrange
        var result = Result<int, string>.Failure("Initial error");

        // Act
        var bound = result.Bind(x => Result<int, string>.Success(x * 2));

        // Assert
        Assert.True(bound.IsFailure);
        Assert.Equal("Initial error", bound.Error);
    }

    [Fact]
    public void Bind_WhenOperationFails_ReturnsNewError()
    {
        // Arrange
        var result = Result<int, string>.Success(-5);

        // Act
        var bound = result.Bind(x => 
            x > 0 
                ? Result<int, string>.Success(x * 2) 
                : Result<int, string>.Failure("Value must be positive")
        );

        // Assert
        Assert.True(bound.IsFailure);
        Assert.Equal("Value must be positive", bound.Error);
    }

    [Fact]
    public void Bind_ChainedOperations_StopsAtFirstFailure()
    {
        // Arrange
        var result = Result<int, string>.Success(10);

        // Act
        var bound = result
            .Bind(x => x > 0 ? Result<int, string>.Success(x) : Result<int, string>.Failure("Error 1"))
            .Bind(x => x < 100 ? Result<int, string>.Success(x) : Result<int, string>.Failure("Error 2"))
            .Bind(x => x != 10 ? Result<int, string>.Success(x) : Result<int, string>.Failure("Error 3"));

        // Assert
        Assert.True(bound.IsFailure);
        Assert.Equal("Error 3", bound.Error);
    }

    [Fact]
    public void Bind_ChangesValueType_MaintainsErrorType()
    {
        // Arrange
        var result = Result<int, string>.Success(42);

        // Act
        var bound = result.Bind(age => 
            Result<Person, string>.Success(new Person($"User{age}", age))
        );

        // Assert
        Assert.True(bound.IsSuccess);
        Assert.Equal("User42", bound.Value.Name);
    }

    [Fact]
    public void Bind_ValidationChain_WorksCorrectly()
    {
        // Arrange
        var email = "user@example.com";

        // Act
        var result = ValidateEmail(email)
            .Bind(ValidateEmailDomain)
            .Bind(CreateUser);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(email, result.Value.Email);
    }

    #endregion

    #region MapError Tests

    [Fact]
    public void MapError_OnFailedResult_TransformsError()
    {
        // Arrange
        var result = Result<int, string>.Failure("404");

        // Act
        var mapped = result.MapError(int.Parse);

        // Assert
        Assert.True(mapped.IsFailure);
        Assert.Equal(404, mapped.Error);
    }

    [Fact]
    public void MapError_OnSuccessfulResult_PreservesValue()
    {
        // Arrange
        var result = Result<int, string>.Success(42);

        // Act
        var mapped = result.MapError(int.Parse);

        // Assert
        Assert.True(mapped.IsSuccess);
        Assert.Equal(42, mapped.Value);
    }

    [Fact]
    public void MapError_ChangesErrorType_PreservesSuccessType()
    {
        // Arrange
        var result = Result<Person, string>.Failure("Not found");

        // Act
        var mapped = result.MapError(msg => new ApiError("NOT_FOUND", msg));

        // Assert
        Assert.True(mapped.IsFailure);
        Assert.Equal("NOT_FOUND", mapped.Error.Code);
        Assert.Equal("Not found", mapped.Error.Message);
    }

    [Fact]
    public void MapError_ChainedTransformations_WorksCorrectly()
    {
        // Arrange
        var result = Result<int, int>.Failure(404);

        // Act
        var mapped = result
            .MapError(code => code.ToString())
            .MapError(msg => new ApiError("ERROR", msg));

        // Assert
        Assert.True(mapped.IsFailure);
        Assert.Equal("ERROR", mapped.Error.Code);
        Assert.Equal("404", mapped.Error.Message);
    }

    #endregion

    #region Match Tests

    [Fact]
    public void Match_OnSuccessfulResult_ExecutesSuccessHandler()
    {
        // Arrange
        var result = Result<int, string>.Success(42);

        // Act
        var output = result.Match(
            success => $"Value: {success}",
            error => $"Error: {error}"
        );

        // Assert
        Assert.Equal("Value: 42", output);
    }

    [Fact]
    public void Match_OnFailedResult_ExecutesErrorHandler()
    {
        // Arrange
        var result = Result<int, string>.Failure("Something went wrong");

        // Act
        var output = result.Match(
            success => $"Value: {success}",
            error => $"Error: {error}"
        );

        // Assert
        Assert.Equal("Error: Something went wrong", output);
    }

    [Fact]
    public void Match_ConvertsToCommonType_WorksCorrectly()
    {
        // Arrange
        var successResult = Result<int, string>.Success(42);
        var failureResult = Result<int, string>.Failure("Error");

        // Act
        var successOutput = successResult.Match(
            success => success * 2,
            error => -1
        );
        var failureOutput = failureResult.Match(
            success => success * 2,
            error => -1
        );

        // Assert
        Assert.Equal(84, successOutput);
        Assert.Equal(-1, failureOutput);
    }

    [Fact]
    public void Match_WithComplexTransformations_WorksCorrectly()
    {
        // Arrange
        var result = Result<Person, string>.Success(new Person("John", 30));

        // Act
        var output = result.Match(
            person => new { Status = "Success", Name = person.Name, AgeGroup = GetAgeGroup(person.Age) },
            error => new { Status = "Error", Name = "Unknown", AgeGroup = "Unknown" }
        );

        // Assert
        Assert.Equal("Success", output.Status);
        Assert.Equal("John", output.Name);
        Assert.Equal("Adult", output.AgeGroup);
    }

    #endregion

    #region Combined Operations Tests

    [Fact]
    public void MapAndBind_Combined_WorksCorrectly()
    {
        // Arrange
        var result = Result<int, string>.Success(5);

        // Act
        var output = result
            .Map(x => x * 2)  // 10
            .Bind(x => x > 5 
                ? Result<string, string>.Success($"Value: {x}") 
                : Result<string, string>.Failure("Too small"))
            .Map(s => s.ToUpper()); // "VALUE: 10"

        // Assert
        Assert.True(output.IsSuccess);
        Assert.Equal("VALUE: 10", output.Value);
    }

    [Fact]
    public void CompleteWorkflow_WithAllOperations_WorksCorrectly()
    {
        // Arrange
        var input = "user@example.com";

        // Act
        var result = ParseEmail(input)
            .Map(email => email.ToLower())
            .Bind(ValidateEmail)
            .Bind(ValidateEmailDomain)
            .Bind(CreateUser)
            .MapError(error => new ApiError("VALIDATION_FAILED", error))
            .Match(
                user => $"Created user: {user.Email}",
                error => $"Failed: {error.Message}"
            );

        // Assert
        Assert.Equal("Created user: user@example.com", result);
    }

    #endregion

    #region Test Helpers

    private record Person(string Name, int Age);
    private record User(string Email);
    private record ApiError(string Code, string Message);

    private static Result<string, string> ParseEmail(string input) =>
        string.IsNullOrWhiteSpace(input)
            ? Result<string, string>.Failure("Email is required")
            : Result<string, string>.Success(input);

    private static Result<string, string> ValidateEmail(string email) =>
        email.Contains("@")
            ? Result<string, string>.Success(email)
            : Result<string, string>.Failure("Invalid email format");

    private static Result<string, string> ValidateEmailDomain(string email) =>
        email.EndsWith("example.com")
            ? Result<string, string>.Success(email)
            : Result<string, string>.Failure("Domain not allowed");

    private static Result<User, string> CreateUser(string email) =>
        Result<User, string>.Success(new User(email));

    private static string GetAgeGroup(int age) =>
        age switch
        {
            < 18 => "Minor",
            < 65 => "Adult",
            _ => "Senior"
        };

    #endregion
}