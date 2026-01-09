using BindSharp.Extensions;

namespace BindSharp.Test.ExtensionsTests.SideEffect.Do;

public sealed class RealWorldScenarioTests
{
    [Fact]
    public void Do_ForRequestHandling_WorksLikeExpected()
    {
        // Arrange
        bool requestHandled = false;
        bool errorHandled = false;
        var jsonPayload = Result<string, string>.Success("{\"id\":1}");

        // Act
        jsonPayload
            .Bind(json => Result<int, string>.Success(1)) // Simulate deserialization
            .Do(
                data => requestHandled = true,
                error => errorHandled = true
            );

        // Assert
        Assert.True(requestHandled);
        Assert.False(errorHandled);
    }

    [Fact]
    public async Task DoAsync_ForOrderProcessing_WorksLikeExpected()
    {
        // Arrange
        bool successNotificationSent = false;
        bool errorAlertSent = false;

        // Act
        await Task.FromResult(Result<int, string>.Success(100))
            .BindAsync(async amount => {
                await Task.Delay(10);
                return amount > 50 
                    ? Result<int, string>.Success(amount)
                    : Result<int, string>.Failure("Amount too small");
            })
            .DoAsync(
                async amount => {
                    await Task.Delay(10);
                    successNotificationSent = true;
                },
                async error => {
                    await Task.Delay(10);
                    errorAlertSent = true;
                }
            );

        // Assert
        Assert.True(successNotificationSent);
        Assert.False(errorAlertSent);
    }
}