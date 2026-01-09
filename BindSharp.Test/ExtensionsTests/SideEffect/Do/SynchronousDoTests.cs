using BindSharp.Extensions;

namespace BindSharp.Test.ExtensionsTests.SideEffect.Do;

public sealed class SynchronousDoTests
{
    [Fact]
    public void Do_WithSuccessfulResult_ExecutesOnSuccess()
    {
        // Arrange
        var result = Result<int, string>.Success(42);
        bool successCalled = false;
        bool failureCalled = false;

        // Act
        var returned = result.Do(
            success => successCalled = true,
            error => failureCalled = true
        );

        // Assert
        Assert.True(successCalled);
        Assert.False(failureCalled);
        Assert.Equal(result, returned); // Verify result is unchanged
    }

    [Fact]
    public void Do_WithFailedResult_ExecutesOnFailure()
    {
        // Arrange
        var result = Result<int, string>.Failure("Error occurred");
        bool successCalled = false;
        bool failureCalled = false;

        // Act
        var returned = result.Do(
            success => successCalled = true,
            error => failureCalled = true
        );

        // Assert
        Assert.False(successCalled);
        Assert.True(failureCalled);
        Assert.Equal(result, returned); // Verify result is unchanged
    }

    [Fact]
    public void Do_PreservesResultForFurtherComposition()
    {
        // Arrange
        var result = Result<int, string>.Success(10);

        // Act
        var final = result
            .Do(
                success => Console.WriteLine($"Value: {success}"),
                error => Console.WriteLine($"Error: {error}")
            )
            .Map(x => x * 2)
            .Map(x => x.ToString());

        // Assert
        Assert.True(final.IsSuccess);
        Assert.Equal("20", final.Value);
    }

    [Fact]
    public void Do_PassesCorrectValuesToHandlers()
    {
        // Arrange
        var expectedSuccess = 42;
        var result = Result<int, string>.Success(expectedSuccess);
        int? capturedSuccess = null;

        // Act
        result.Do(
            success => capturedSuccess = success,
            error => { }
        );

        // Assert
        Assert.Equal(expectedSuccess, capturedSuccess);
    }

    [Fact]
    public void Do_PassesCorrectErrorToHandlers()
    {
        // Arrange
        var expectedError = "Test error";
        var result = Result<int, string>.Failure(expectedError);
        string? capturedError = null;

        // Act
        result.Do(
            success => { },
            error => capturedError = error
        );

        // Assert
        Assert.Equal(expectedError, capturedError);
    }
}