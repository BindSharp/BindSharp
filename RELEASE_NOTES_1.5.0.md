# BindSharp 1.5.0 Release Notes

## 🎉 What's New

Version 1.5.0 introduces **error-specific side effects** to functional pipelines, enabling you to handle logging, metrics, and error notifications without transforming the error value.

### New Feature: TapError / TapErrorAsync

**The Problem:**

Previously, when you needed to log errors or trigger error-specific side effects (metrics, notifications), you had to use MapErrorAsync even though you weren't transforming the error:

```csharp
// ❌ Before: Awkward - using MapError just for logging
var result = await GetUserAsync(id)
    .BindAsync(user => ProcessUserAsync(user))
    .MapErrorAsync(async error => {
        await _logger.LogErrorAsync(error);  // Side effect
        return error;  // Have to return the same error - ugly!
    });
```

**The Solution:**

TapError provides clean error-specific side effects without transformation:

```csharp
// ✅ After: Clean and intuitive
var result = await GetUserAsync(id)
    .BindAsync(user => ProcessUserAsync(user))
    .TapErrorAsync(error => _logger.LogErrorAsync(error));  // Clean!
```

## 📦 New API Methods

### ResultExtensions

#### TapError

```csharp
public static Result<T, TError> TapError<T, TError>(
    this Result<T, TError> result,
    Action<TError> action)
```

Executes a synchronous side effect on a failed result's error without modifying the result:

- If result is **failure** → executes action with error value
- If result is **success** → skips action, returns result unchanged
- Always returns the original result unchanged

**Symmetric with Tap:** Works exactly like Tap but for error cases instead of success cases.

### Async Overloads

#### TapErrorAsync (Result)

```csharp
public static Task<Result<T, TError>> TapErrorAsync<T, TError>(
    this Result<T, TError> result,
    Func<TError, Task> action)
```

#### TapErrorAsync (Task<Result>)

```csharp
public static Task<Result<T, TError>> TapErrorAsync<T, TError>(
    this Task<Result<T, TError>> resultTask,
    Func<TError, Task> action)
```

Complete async support covering all scenarios (3 overloads total, matching Tap pattern).

## 💡 Usage Examples

### Example 1: Error Logging

```csharp
// Log errors without transforming them
public async Task<Result<User, string>> GetUserWithPostsAsync(int userId)
{
    return await _userRepository.GetByIdAsync(userId)
        .EnsureNotNullAsync("User not found")
        .BindAsync(user => _postRepository
            .GetByUserIdAsync(userId)
            .MapAsync(posts => { user.Posts = posts; return user; })
        )
        .TapAsync(user => _cache.SetAsync($"user:{userId}", user))
        .TapErrorAsync(error => _logger.LogErrorAsync(error));  // ✨ Clean!
}
```

### Example 2: Error Metrics

```csharp
// Track error rates without transforming errors
public async Task<Result<Order, string>> ProcessOrderAsync(Order order)
{
    return await ValidateOrder(order)
        .BindAsync(async o => await SaveOrderAsync(o))
        .BindAsync(async o => await ChargePaymentAsync(o))
        .TapErrorAsync(async error => {
            await _metrics.IncrementAsync("orders.failed");
            await _metrics.RecordErrorAsync("order_processing", error);
        });
}
```

### Example 3: Error Notifications

```csharp
// Notify on errors without changing the error
public async Task<Result<Data, string>> FetchCriticalDataAsync(string id)
{
    return await _apiClient.GetDataAsync(id)
        .TapErrorAsync(async error => 
            await _alerting.SendCriticalAlertAsync($"Data fetch failed: {error}")
        )
        .TapErrorAsync(async error => 
            await _audit.LogFailureAsync(id, error)
        );
}
```

### Example 4: Symmetric Success/Error Handling

```csharp
// Handle both success and failure side effects symmetrically
public async Task<Result<Report, string>> GenerateReportAsync(int id)
{
    return await FetchReportDataAsync(id)
        .BindAsync(data => ProcessReportAsync(data))
        .TapAsync(async report => {
            await _logger.LogInfoAsync($"Report {id} generated successfully");
            await _metrics.IncrementAsync("reports.success");
        })
        .TapErrorAsync(async error => {
            await _logger.LogErrorAsync($"Report {id} failed: {error}");
            await _metrics.IncrementAsync("reports.failed");
        });
}
```

### Example 5: Complete Pipeline with Error Handling

```csharp
public async Task<Result<Invoice, string>> CreateInvoiceAsync(CreateInvoiceRequest request)
{
    return await ValidateInvoiceRequest(request)
        .TapAsync(_ => _logger.LogInfoAsync("Invoice validation passed"))
        .BindAsync(async req => await CreateInvoiceEntityAsync(req))
        .TapAsync(async invoice => await _cache.SetAsync($"invoice:{invoice.Id}", invoice))
        .BindAsync(async invoice => await SendInvoiceEmailAsync(invoice))
        .TapAsync(async invoice => {
            await _metrics.IncrementAsync("invoices.created");
            await _logger.LogInfoAsync($"Invoice {invoice.Id} created");
        })
        .TapErrorAsync(async error => {
            await _logger.LogErrorAsync($"Invoice creation failed: {error}");
            await _metrics.IncrementAsync("invoices.failed");
            await _alerting.NotifyAdminAsync($"Invoice failure: {error}");
        });
}
```

## 🎯 When to Use TapError

### ✅ Use TapError When:

- You need to log errors without transforming them
- Recording error metrics or analytics
- Sending error notifications/alerts
- Audit logging for failures
- Triggering error-specific workflows
- Any side effect that should only happen on failure

### ✅ Perfect For:

- **Error logging** - "Log this error but keep it unchanged"
- **Metrics** - "Increment error counter for this operation"
- **Alerting** - "Notify admins when this fails"
- **Audit trails** - "Record failure in audit log"
- **Monitoring** - "Track error patterns over time"

### ❌ Don't Use TapError When:

- You need to transform the error (use MapError instead)
- You need to recover from errors (use Bind with recovery logic)
- You need to add context to errors (use MapError to enrich)
- The side effect should run on success (use Tap instead)

## 🔄 Comparison with Other Methods

| Method | Executes On | Changes Result? | Use Case |
|--------|-------------|-----------------|----------|
| Tap | Success | No | Success-only side effects |
| TapError | Failure | No | Error-only side effects |
| MapError | Failure | Yes (transforms error) | Error transformation |
| MatchAsync | Both | Yes (returns new value) | Handle both cases |

**Key Differences:**

- Tap - Success-only side effects (logging successes, caching)
- TapError - Error-only side effects (logging errors, alerting)
- MapError - Transforms errors (enriching, translating error types)
- Match - Extracts values from both success and failure

**Symmetric Design:**

```csharp
// Tap and TapError are symmetric - one for success, one for failure
result
    .Tap(value => Console.WriteLine($"Success: {value}"))      // Only on success
    .TapError(error => Console.WriteLine($"Error: {error}"));  // Only on failure
```

## 🔧 Migration from 1.4.x

**No migration needed!** All changes are backwards compatible.

### What Continues to Work:

```csharp
// All existing code unchanged
var result = await GetData()
    .MapAsync(x => x * 2)
    .BindAsync(Validate)
    .TapAsync(async x => await LogSuccessAsync(x));
```

### What You Can Now Do:

```csharp
// New error-specific side effects
var result = await GetData()
    .MapAsync(x => x * 2)
    .BindAsync(Validate)
    .TapAsync(async x => await LogSuccessAsync(x))
    .TapErrorAsync(async err => await LogErrorAsync(err));  // ✨ New!
```

### Before vs After:

**Before (1.4.x):**

```csharp
var result = await ProcessDataAsync()
    .MapErrorAsync(async error => {
        await _logger.LogErrorAsync(error);  // Side effect
        return error;  // Awkward - have to return unchanged
    });
```

**After (1.5.0):**

```csharp
var result = await ProcessDataAsync()
    .TapErrorAsync(error => _logger.LogErrorAsync(error));  // ✨ Clean!
```

## 📖 Documentation Updates

- Added TapError / TapErrorAsync section to README.md
- Added error-specific side effects examples
- Updated API reference with 3 new method signatures
- Added comparison table showing Tap vs TapError
- Updated best practices section

## 🧪 Testing

- 15 comprehensive unit tests covering all overloads
- Tests for error execution and success skipping
- Tests for result preservation (unchanged returns)
- Tests for chaining multiple TapError calls
- Tests for Tap/TapError interaction
- Real-world scenario tests (logging, metrics)

## 📋 Changelog

### Added

- TapError - Execute side effects on failed results (3 overloads)
    - Synchronous version for Result
    - Async version for Result
    - Async version for Task<Result>
- Comprehensive XML documentation for all new methods
- Package tags: error-handling, side-effects

### Changed

- None (no breaking changes)

### Fixed

- None

### Removed

- None

## 🎓 Best Practices

### DO:

✅ Use TapError for error-specific side effects (logging, metrics, alerts)  
✅ Use Tap for success-specific side effects (logging, caching, notifications)  
✅ Chain multiple TapError calls for different error concerns  
✅ Keep error actions side-effect only (don't modify state)  
✅ Combine Tap and TapError for complete observability

### DON'T:

❌ Use TapError when you need to transform the error (use MapError)  
❌ Put complex logic in error actions (keep them simple)  
❌ Use TapError for error recovery (use Bind with recovery logic)  
❌ Modify the error value inside TapError (it won't persist)

## 💡 Pattern Examples

### Pattern 1: Complete Observability

```csharp
// Log both successes and failures
.TapAsync(data => _logger.LogInfoAsync($"Success: {data}"))
.TapErrorAsync(error => _logger.LogErrorAsync($"Failure: {error}"))
```

### Pattern 2: Error Metrics

```csharp
// Track error rates without changing errors
.TapErrorAsync(async error => {
    await _metrics.IncrementAsync("operation.failed");
    await _metrics.RecordErrorTypeAsync(error.GetType().Name);
})
```

### Pattern 3: Multi-Concern Error Handling

```csharp
// Multiple error concerns in sequence
.TapErrorAsync(error => _logger.LogErrorAsync(error))       // Logging
.TapErrorAsync(error => _metrics.RecordErrorAsync(error))   // Metrics
.TapErrorAsync(error => _alerting.NotifyAsync(error))       // Alerting
```

### Pattern 4: Conditional Error Actions

```csharp
// Different actions for different error types
.TapErrorAsync(async error => {
    if (error.Contains("timeout"))
        await _alerting.SendCriticalAlertAsync(error);
    else
        await _logger.LogWarningAsync(error);
})
```

## 🔗 Related Methods

- **Tap / TapAsync** - Execute side effects on success (symmetric counterpart)
- **MapError / MapErrorAsync** - Transform error values
- **Match / MatchAsync** - Handle both success and failure with return values

## 🚀 Next Steps

1. **Update your package:**

```bash
dotnet add package BindSharp --version 1.5.0
```

2. **Replace MapErrorAsync logging** with TapErrorAsync
3. **Add error observability** to your pipelines
4. **Combine with Tap** for complete success/failure tracking

## 🙏 Acknowledgments

Thanks to the community for requesting better error-specific side effect handling!

This feature completes the symmetric design of Tap operations:

- Tap → Success side effects
- TapError → Error side effects

## 📚 Additional Resources

- **GitHub:** [https://github.com/GDB-11/BindSharp/](https://github.com/GDB-11/BindSharp/)
- **Documentation:** See README.md for complete usage guide
- **Previous Releases:**
    - [1.4.1](RELEASE_NOTES_1_4_1.md) - Conditional processing (BindIf)
    - [1.3.0](RELEASE_NOTES_1_3_0.md) - Equality & implicit conversions
    - [1.2.0](RELEASE_NOTES_1_2_0.md) - Unit type
    - [1.1.0](RELEASE_NOTES_1_1_0.md) - ResultExtensions utilities

## 🎉 Conclusion

Version 1.5.0 brings clean error-specific side effects to BindSharp:

- ✅ Symmetric with Tap (success) vs TapError (failure)
- ✅ Full async support (3 overloads)
- ✅ Zero breaking changes
- 🎯 Better error observability in functional pipelines

Upgrade today and enjoy cleaner error handling! 🚀
