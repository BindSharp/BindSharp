using BindSharp.Extensions;

namespace BindSharp.Test.ExtensionsTests.SideEffect.Do;

public sealed class IntegrationTests
{
    [Fact]
    public void Do_InComplexPipeline_WorksCorrectly()
    {
        // Arrange
        var input = "42";
        var log = new List<string>();

        // Act
        var result = Result<string, string>.Success(input)
            .Do(
                s => log.Add($"Input: {s}"),
                e => log.Add($"Error: {e}")
            )
            .Bind(s => int.TryParse(s, out var num) 
                ? Result<int, string>.Success(num)
                : Result<int, string>.Failure("Parse failed"))
            .Do(
                n => log.Add($"Parsed: {n}"),
                e => log.Add($"Parse error: {e}")
            )
            .Map(n => n * 2)
            .Do(
                n => log.Add($"Doubled: {n}"),
                e => log.Add($"Double error: {e}")
            );

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(84, result.Value);
        Assert.Equal(3, log.Count);
        Assert.Equal("Input: 42", log[0]);
        Assert.Equal("Parsed: 42", log[1]);
        Assert.Equal("Doubled: 84", log[2]);
    }

    [Fact]
    public async Task DoAsync_InComplexAsyncPipeline_WorksCorrectly()
    {
        // Arrange
        var log = new List<string>();

        // Act
        var result = await Task.FromResult(Result<int, string>.Success(10))
            .DoAsync(
                async n => {
                    await Task.Delay(10);
                    log.Add($"Start: {n}");
                },
                async e => await Task.CompletedTask
            )
            .MapAsync(async n => {
                await Task.Delay(10);
                return n * 2;
            })
            .DoAsync(
                n => log.Add($"Doubled: {n}"),
                e => { }
            )
            .BindAsync(async n => {
                await Task.Delay(10);
                return n > 15 
                    ? Result<int, string>.Success(n)
                    : Result<int, string>.Failure("Too small");
            })
            .DoAsync(
                async n => {
                    await Task.Delay(10);
                    log.Add($"Final: {n}");
                },
                async e => {
                    await Task.Delay(10);
                    log.Add($"Error: {e}");
                }
            );

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(20, result.Value);
        Assert.Equal(3, log.Count);
    }

    [Fact]
    public void Do_WithFailurePropagation_SkipsSuccessHandlers()
    {
        // Arrange
        var log = new List<string>();

        // Act
        var result = Result<int, string>.Success(5)
            .Do(
                n => log.Add($"Start: {n}"),
                e => log.Add($"Start error: {e}")
            )
            .Bind(n => n > 10 
                ? Result<int, string>.Success(n)
                : Result<int, string>.Failure("Too small"))
            .Do(
                n => log.Add($"After bind: {n}"),
                e => log.Add($"Bind error: {e}")
            )
            .Map(n => n * 2)
            .Do(
                n => log.Add($"After map: {n}"),
                e => log.Add($"Map error: {e}")
            );

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Too small", result.Error);
        Assert.Equal(3, log.Count);
        Assert.Equal("Start: 5", log[0]);
        Assert.Equal("Bind error: Too small", log[1]);
        Assert.Equal("Map error: Too small", log[2]);
    }
}