using BindSharp.Extensions;

namespace BindSharp.Test;

/// <summary>
/// Tests for ResultExtensions Tap and TapAsync methods (side effects)
/// </summary>
public class ResultExtensionsSideEffectsTests
{
    #region Tap Tests - Synchronous

    [Fact]
    public void Tap_OnSuccessfulResult_ExecutesAction()
    {
        // Arrange
        var result = Result<int, string>.Success(42);
        var sideEffectExecuted = false;

        // Act
        var tapped = result.Tap(x => sideEffectExecuted = true);

        // Assert
        Assert.True(sideEffectExecuted);
        Assert.True(tapped.IsSuccess);
        Assert.Equal(42, tapped.Value);
    }

    [Fact]
    public void Tap_OnSuccessfulResult_CanAccessValue()
    {
        // Arrange
        var result = Result<int, string>.Success(42);
        var capturedValue = 0;

        // Act
        var tapped = result.Tap(x => capturedValue = x * 2);

        // Assert
        Assert.Equal(84, capturedValue);
        Assert.True(tapped.IsSuccess);
        Assert.Equal(42, tapped.Value); // Original value unchanged
    }

    [Fact]
    public void Tap_OnFailedResult_DoesNotExecuteAction()
    {
        // Arrange
        var result = Result<int, string>.Failure("Error");
        var sideEffectExecuted = false;

        // Act
        var tapped = result.Tap(x => sideEffectExecuted = true);

        // Assert
        Assert.False(sideEffectExecuted);
        Assert.True(tapped.IsFailure);
        Assert.Equal("Error", tapped.Error);
    }

    [Fact]
    public void Tap_ReturnsOriginalResult_Unchanged()
    {
        // Arrange
        var result = Result<int, string>.Success(42);

        // Act
        var tapped = result.Tap(x => { /* do nothing */ });

        // Assert
        Assert.Same(result, tapped); // Should return the same instance
    }

    [Fact]
    public void Tap_ChainedCalls_ExecuteInOrder()
    {
        // Arrange
        var result = Result<int, string>.Success(42);
        var log = new List<string>();

        // Act
        var tapped = result
            .Tap(x => log.Add($"First: {x}"))
            .Tap(x => log.Add($"Second: {x}"))
            .Tap(x => log.Add($"Third: {x}"));

        // Assert
        Assert.Equal(3, log.Count);
        Assert.Equal("First: 42", log[0]);
        Assert.Equal("Second: 42", log[1]);
        Assert.Equal("Third: 42", log[2]);
    }

    [Fact]
    public void Tap_WithComplexObject_WorksCorrectly()
    {
        // Arrange
        var result = Result<Person, string>.Success(new Person("John", 30));
        string? capturedName = null;

        // Act
        var tapped = result.Tap(p => capturedName = p.Name);

        // Assert
        Assert.Equal("John", capturedName);
        Assert.True(tapped.IsSuccess);
    }

    #endregion

    #region TapAsync Tests - Result + Async Action

    [Fact]
    public async Task TapAsync_OnSuccessfulResult_ExecutesAsyncAction()
    {
        // Arrange
        var result = Result<int, string>.Success(42);
        var sideEffectExecuted = false;

        // Act
        var tapped = await result.TapAsync(async x =>
        {
            await Task.Delay(1);
            sideEffectExecuted = true;
        });

        // Assert
        Assert.True(sideEffectExecuted);
        Assert.True(tapped.IsSuccess);
        Assert.Equal(42, tapped.Value);
    }

    [Fact]
    public async Task TapAsync_OnSuccessfulResult_CanAccessValue()
    {
        // Arrange
        var result = Result<int, string>.Success(42);
        var capturedValue = 0;

        // Act
        var tapped = await result.TapAsync(async x =>
        {
            await Task.Delay(1);
            capturedValue = x * 2;
        });

        // Assert
        Assert.Equal(84, capturedValue);
        Assert.True(tapped.IsSuccess);
        Assert.Equal(42, tapped.Value);
    }

    [Fact]
    public async Task TapAsync_OnFailedResult_DoesNotExecuteAction()
    {
        // Arrange
        var result = Result<int, string>.Failure("Error");
        var sideEffectExecuted = false;

        // Act
        var tapped = await result.TapAsync(async x =>
        {
            await Task.Delay(1);
            sideEffectExecuted = true;
        });

        // Assert
        Assert.False(sideEffectExecuted);
        Assert.True(tapped.IsFailure);
        Assert.Equal("Error", tapped.Error);
    }

    #endregion

    #region TapAsync Tests - Task{Result} + Async Action

    [Fact]
    public async Task TapAsync_TaskResult_ExecutesAsyncAction()
    {
        // Arrange
        var resultTask = GetValueAsync(42);
        var sideEffectExecuted = false;

        // Act
        var tapped = await resultTask.TapAsync(async x =>
        {
            await Task.Delay(1);
            sideEffectExecuted = true;
        });

        // Assert
        Assert.True(sideEffectExecuted);
        Assert.True(tapped.IsSuccess);
        Assert.Equal(42, tapped.Value);
    }

    [Fact]
    public async Task TapAsync_TaskResult_CanAccessValue()
    {
        // Arrange
        var resultTask = GetValueAsync(42);
        var capturedValue = 0;

        // Act
        var tapped = await resultTask.TapAsync(async x =>
        {
            await Task.Delay(1);
            capturedValue = x * 2;
        });

        // Assert
        Assert.Equal(84, capturedValue);
        Assert.True(tapped.IsSuccess);
        Assert.Equal(42, tapped.Value);
    }

    [Fact]
    public async Task TapAsync_ChainedAsyncCalls_ExecuteInOrder()
    {
        // Arrange
        var resultTask = GetValueAsync(42);
        var log = new List<string>();

        // Act
        var tapped = await resultTask
            .TapAsync(async x =>
            {
                await Task.Delay(1);
                log.Add($"First: {x}");
            })
            .TapAsync(async x =>
            {
                await Task.Delay(1);
                log.Add($"Second: {x}");
            })
            .TapAsync(async x =>
            {
                await Task.Delay(1);
                log.Add($"Third: {x}");
            });

        // Assert
        Assert.Equal(3, log.Count);
        Assert.Equal("First: 42", log[0]);
        Assert.Equal("Second: 42", log[1]);
        Assert.Equal("Third: 42", log[2]);
    }

    #endregion

    #region Integration with Other Operations

    [Fact]
    public void Tap_InPipeline_WorksCorrectly()
    {
        // Arrange
        var result = Result<int, string>.Success(5);
        var log = new List<string>();

        // Act
        var final = result
            .Tap(x => log.Add($"Initial: {x}"))
            .Map(x => x * 2)
            .Tap(x => log.Add($"After map: {x}"))
            .Bind(x => x > 0
                ? Result<int, string>.Success(x)
                : Result<int, string>.Failure("Must be positive"))
            .Tap(x => log.Add($"After bind: {x}"));

        // Assert
        Assert.True(final.IsSuccess);
        Assert.Equal(10, final.Value);
        Assert.Equal(3, log.Count);
        Assert.Equal("Initial: 5", log[0]);
        Assert.Equal("After map: 10", log[1]);
        Assert.Equal("After bind: 10", log[2]);
    }

    [Fact]
    public async Task TapAsync_InComplexAsyncPipeline_WorksCorrectly()
    {
        // Arrange
        var userId = 42;
        var log = new List<string>();

        // Act
        var result = await GetUserIdAsync(userId)
            .TapAsync(async id => 
            {
                await Task.Delay(1);
                log.Add($"Got ID: {id}");
            })
            .BindAsync(async id => await FetchUserAsync(id))
            .TapAsync(async user => 
            {
                await Task.Delay(1);
                log.Add($"Fetched user: {user.Email}");
            })
            .MapAsync(user => user.Email)
            .TapAsync(async email => 
            {
                await Task.Delay(1);
                log.Add($"Extracted email: {email}");
            });

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("user42@example.com", result.Value);
        Assert.Equal(3, log.Count);
    }

    [Fact]
    public async Task TapAsync_MixedWithMap_AndBind_WorksCorrectly()
    {
        // Arrange
        var result = Result<int, string>.Success(5);
        var operations = new List<string>();

        // Act
        var final = await result
            .Tap(x => operations.Add($"Tap1: {x}"))
            .MapAsync(async x =>
            {
                await Task.Delay(1);
                operations.Add($"Map: {x}");
                return x * 2;
            })
            .TapAsync(async x =>
            {
                await Task.Delay(1);
                operations.Add($"TapAsync: {x}");
            })
            .BindAsync(async x =>
            {
                await Task.Delay(1);
                operations.Add($"Bind: {x}");
                return Result<int, string>.Success(x + 5);
            })
            .TapAsync(async x =>
            {
                await Task.Delay(1);
                operations.Add($"Tap2: {x}");
            });

        // Assert
        Assert.True(final.IsSuccess);
        Assert.Equal(15, final.Value);
        Assert.Equal(5, operations.Count);
    }

    #endregion

    #region Real-World Scenarios - Logging

    [Fact]
    public void Tap_ForLogging_WorksCorrectly()
    {
        // Arrange
        var logger = new FakeLogger();
        var result = Result<Order, string>.Success(new Order(123, "user@example.com"));

        // Act
        var processed = result
            .Tap(order => logger.LogInfo($"Processing order {order.Id}"))
            .Map(order => new { order.Id, order.Email })
            .Tap(dto => logger.LogInfo($"Created DTO for order {dto.Id}"));

        // Assert
        Assert.True(processed.IsSuccess);
        Assert.Equal(2, logger.Logs.Count);
        Assert.Contains("Processing order 123", logger.Logs[0]);
        Assert.Contains("Created DTO for order 123", logger.Logs[1]);
    }

    [Fact]
    public async Task TapAsync_ForAsyncLogging_WorksCorrectly()
    {
        // Arrange
        var logger = new FakeAsyncLogger();
        var order = new Order(123, "user@example.com");

        // Act
        var result = await ProcessOrderAsync(order)
            .TapAsync(async o => await logger.LogInfoAsync($"Order {o.Id} processed"))
            .TapAsync(async o => await logger.LogInfoAsync($"Email sent to {o.Email}"));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, logger.Logs.Count);
    }

    #endregion

    #region Real-World Scenarios - Metrics

    [Fact]
    public void Tap_ForMetrics_WorksCorrectly()
    {
        // Arrange
        var metrics = new FakeMetrics();
        var result = Result<int, string>.Success(42);

        // Act
        var processed = result
            .Tap(_ => metrics.Increment("operations.started"))
            .Map(x => x * 2)
            .Tap(_ => metrics.Increment("operations.completed"));

        // Assert
        Assert.True(processed.IsSuccess);
        Assert.Equal(1, metrics.GetCount("operations.started"));
        Assert.Equal(1, metrics.GetCount("operations.completed"));
    }

    [Fact]
    public async Task TapAsync_ForAsyncMetrics_WorksCorrectly()
    {
        // Arrange
        var metrics = new FakeAsyncMetrics();

        // Act
        var result = await GetUserIdAsync(42)
            .TapAsync(async _ => await metrics.IncrementAsync("users.fetched"))
            .BindAsync(async id => await FetchUserAsync(id))
            .TapAsync(async _ => await metrics.IncrementAsync("users.processed"));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, await metrics.GetCountAsync("users.fetched"));
        Assert.Equal(1, await metrics.GetCountAsync("users.processed"));
    }

    #endregion

    #region Real-World Scenarios - Notifications

    [Fact]
    public async Task TapAsync_ForNotifications_WorksCorrectly()
    {
        // Arrange
        var notifications = new FakeNotificationService();
        var order = new Order(123, "user@example.com");

        // Act
        var result = await ProcessOrderAsync(order)
            .TapAsync(async o => await notifications.NotifyAsync(o.Email, "Order confirmed"))
            .TapAsync(async o => await notifications.NotifyAsync("admin@example.com", $"Order {o.Id} placed"));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, notifications.Notifications.Count);
        Assert.Contains(notifications.Notifications, n => n.To == "user@example.com");
        Assert.Contains(notifications.Notifications, n => n.To == "admin@example.com");
    }

    #endregion

    #region Real-World Scenarios - Audit Trail

    [Fact]
    public async Task TapAsync_ForAuditTrail_WorksCorrectly()
    {
        // Arrange
        var auditLog = new FakeAuditLog();
        var userId = 42;

        // Act
        var result = await GetUserIdAsync(userId)
            .TapAsync(async id => await auditLog.LogAsync("UserAccess", $"User {id} accessed"))
            .BindAsync(async id => await FetchUserAsync(id))
            .TapAsync(async user => await auditLog.LogAsync("UserFetch", $"User {user.Email} fetched"))
            .MapAsync(user => user.Email.ToUpper())
            .TapAsync(async email => await auditLog.LogAsync("DataTransform", $"Email transformed: {email}"));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, auditLog.Entries.Count);
        Assert.Equal("UserAccess", auditLog.Entries[0].Action);
        Assert.Equal("UserFetch", auditLog.Entries[1].Action);
        Assert.Equal("DataTransform", auditLog.Entries[2].Action);
    }

    #endregion

    #region Test Helpers

    private record Person(string Name, int Age);
    private record User(string Email);
    private record Order(int Id, string Email);

    private static Task<Result<int, string>> GetValueAsync(int value) =>
        Task.FromResult(Result<int, string>.Success(value));

    private static Task<Result<int, string>> GetUserIdAsync(int id) =>
        Task.FromResult(Result<int, string>.Success(id));

    private static async Task<Result<User, string>> FetchUserAsync(int id)
    {
        await Task.Delay(1);
        return Result<User, string>.Success(new User($"user{id}@example.com"));
    }

    private static async Task<Result<Order, string>> ProcessOrderAsync(Order order)
    {
        await Task.Delay(1);
        return Result<Order, string>.Success(order);
    }

    private class FakeLogger
    {
        public List<string> Logs { get; } = new();
        public void LogInfo(string message) => Logs.Add(message);
    }

    private class FakeAsyncLogger
    {
        public List<string> Logs { get; } = new();
        public async Task LogInfoAsync(string message)
        {
            await Task.Delay(1);
            Logs.Add(message);
        }
    }

    private class FakeMetrics
    {
        private readonly Dictionary<string, int> _counts = new();
        public void Increment(string metric) =>
            _counts[metric] = _counts.GetValueOrDefault(metric) + 1;
        public int GetCount(string metric) => _counts.GetValueOrDefault(metric);
    }

    private class FakeAsyncMetrics
    {
        private readonly Dictionary<string, int> _counts = new();
        public async Task IncrementAsync(string metric)
        {
            await Task.Delay(1);
            _counts[metric] = _counts.GetValueOrDefault(metric) + 1;
        }
        public Task<int> GetCountAsync(string metric) =>
            Task.FromResult(_counts.GetValueOrDefault(metric));
    }

    private class FakeNotificationService
    {
        public List<(string To, string Message)> Notifications { get; } = new();
        public async Task NotifyAsync(string to, string message)
        {
            await Task.Delay(1);
            Notifications.Add((to, message));
        }
    }

    private class FakeAuditLog
    {
        public List<(string Action, string Details)> Entries { get; } = new();
        public async Task LogAsync(string action, string details)
        {
            await Task.Delay(1);
            Entries.Add((action, details));
        }
    }

    #endregion
}