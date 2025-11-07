namespace BindSharp.Test;

/// <summary>
/// Tests for the core Result{T, TError} type
/// </summary>
public class ResultTests
{
    #region Success Creation

    [Fact]
    public void Success_CreatesSuccessfulResult()
    {
        // Arrange & Act
        var result = Result<int, string>.Success(42);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Success_WithNullValue_CreatesSuccessfulResult()
    {
        // Arrange & Act
        var result = Result<string?, string>.Success(null);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }

    [Fact]
    public void Success_WithComplexType_CreatesSuccessfulResult()
    {
        // Arrange
        var person = new Person("John", 30);

        // Act
        var result = Result<Person, string>.Success(person);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(person, result.Value);
        Assert.Equal("John", result.Value.Name);
    }

    #endregion

    #region Failure Creation

    [Fact]
    public void Failure_CreatesFailedResult()
    {
        // Arrange & Act
        var result = Result<int, string>.Failure("Something went wrong");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("Something went wrong", result.Error);
    }

    [Fact]
    public void Failure_WithCustomErrorType_CreatesFailedResult()
    {
        // Arrange
        var error = new ValidationError("Email", "Invalid format");

        // Act
        var result = Result<User, ValidationError>.Failure(error);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
        Assert.Equal("Email", result.Error.Field);
    }

    #endregion

    #region Value Access

    [Fact]
    public void Value_OnSuccessfulResult_ReturnsValue()
    {
        // Arrange
        var result = Result<int, string>.Success(42);

        // Act
        var value = result.Value;

        // Assert
        Assert.Equal(42, value);
    }

    [Fact]
    public void Value_OnFailedResult_ThrowsInvalidOperationException()
    {
        // Arrange
        var result = Result<int, string>.Failure("Error");

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => result.Value);
        Assert.Equal("Result is not successful", exception.Message);
    }

    #endregion

    #region Error Access

    [Fact]
    public void Error_OnFailedResult_ReturnsError()
    {
        // Arrange
        var result = Result<int, string>.Failure("Something went wrong");

        // Act
        var error = result.Error;

        // Assert
        Assert.Equal("Something went wrong", error);
    }

    [Fact]
    public void Error_OnSuccessfulResult_ThrowsInvalidOperationException()
    {
        // Arrange
        var result = Result<int, string>.Success(42);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => result.Error);
        Assert.Equal("Result is successful", exception.Message);
    }

    #endregion

    #region IsSuccess and IsFailure

    [Theory]
    [InlineData(42, true)]
    [InlineData(0, true)]
    [InlineData(-1, true)]
    public void IsSuccess_OnSuccessfulResults_ReturnsTrue(int value, bool expected)
    {
        // Arrange
        var result = Result<int, string>.Success(value);

        // Act & Assert
        Assert.Equal(expected, result.IsSuccess);
    }

    [Fact]
    public void IsFailure_OnFailedResult_ReturnsTrue()
    {
        // Arrange
        var result = Result<int, string>.Failure("Error");

        // Act & Assert
        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void IsSuccess_And_IsFailure_AreOpposites()
    {
        // Arrange
        var success = Result<int, string>.Success(42);
        var failure = Result<int, string>.Failure("Error");

        // Assert
        Assert.True(success.IsSuccess);
        Assert.False(success.IsFailure);
        Assert.False(failure.IsSuccess);
        Assert.True(failure.IsFailure);
    }

    #endregion

    #region Test Helpers

    private record Person(string Name, int Age);
    private record User(string Email, int Age);
    private record ValidationError(string Field, string Message);

    #endregion
}