namespace BindSharp.Test.ResultExtensionsTests.Try.ExceptionTry;

public sealed class EdgeCasesTests
{
    [Fact]
    public void Try_WithNullException_StillCapturesIt()
    {
        // Act
        var result = ResultExtensions.Try<int>(() => 
            throw null!); // This will actually throw NullReferenceException

        // Assert
        Assert.True(result.IsFailure);
        Assert.IsType<NullReferenceException>(result.Error);
    }

    [Fact]
    public void Try_WithSuccessAndTapError_DoesNotExecuteTapError()
    {
        // Arrange
        bool tapErrorExecuted = false;

        // Act
        var result = ResultExtensions.Try(() => 42)
            .TapError(ex => tapErrorExecuted = true);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(tapErrorExecuted);
    }

    [Fact]
    public async Task TryAsync_WithSuccessAndTapErrorAsync_DoesNotExecuteTapError()
    {
        // Arrange
        bool tapErrorExecuted = false;

        // Act
        var result = await ResultExtensions.TryAsync(async () =>
            {
                await Task.Delay(1);
                return "success";
            })
            .TapErrorAsync(async ex =>
            {
                await Task.Delay(1);
                tapErrorExecuted = true;
            });

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(tapErrorExecuted);
    }
}