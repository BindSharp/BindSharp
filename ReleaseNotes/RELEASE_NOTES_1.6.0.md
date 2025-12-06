# BindSharp 1.6.0 Release Notes

## 🎉 What's New

Version 1.6.0 introduces **exception-first Try overloads** and **mixed async/sync pipeline support**, enabling cleaner exception logging and more flexible pipeline composition.

### New Feature 1: Exception-First Try Overloads

**The Problem:**

Previously, when you needed to log exceptions with full context (stack traces, exception types) before transforming them to custom errors, you had to capture the exception in the error factory:

```csharp
// ❌ Before: Exception logging mixed with error transformation
var result = ResultExtensions.Try(
    () => riskyOperation(),
    ex => {
        _logger.LogError(ex, "Operation failed");  // Side effect
        return $"Error: {ex.Message}";  // Transformation
    }
);
```

**The Solution:**

Exception-first Try overloads return `Result<T, Exception>`, allowing you to use TapError for logging before MapError for transformation:

```csharp
// ✅ After: Clean separation of concerns
var result = ResultExtensions.Try(() => riskyOperation())
    .TapError(ex => _logger.LogError(ex, "Operation failed"))  // Logging
    .MapError(ex => $"Error: {ex.Message}");  // Transformation
```

### New Feature 2: Mixed Async/Sync Pipeline Support

**The Problem:**

Previously, when you had async results but wanted to perform simple sync side effects (like basic logging), you had to wrap them in `Task.FromResult`:

```csharp
// ❌ Before: Awkward wrapping for sync actions
var result = await GetDataAsync()
    .TapErrorAsync(ex => Task.FromResult(capturedEx = ex));  // Ugly!
```

**The Solution:**

New TapAsync/TapErrorAsync overloads accept sync actions with async results:

```csharp
// ✅ After: Natural and clean
var result = await GetDataAsync()
    .TapErrorAsync(ex => _logger.LogError(ex, "Failed"));  // Clean!
```

## 📦 New API Methods

### ResultExtensions - Exception-First Try

#### Try (Exception-First)

```csharp
public static Result<T, Exception> Try<T>(Func<T> operation)
```

Executes code that may throw exceptions and converts it to a Result with the exception as the error. Use this when you want to inspect or log the exception before transforming it to a custom error type.

**Benefits:**
- Preserves full exception details (type, stack trace, inner exceptions)
- Enables pattern matching on specific exception types
- Allows clean separation of logging (TapError) and transformation (MapError)
- Composes naturally with existing Result methods

#### TryAsync (Exception-First)

```csharp
public static Task<Result<T, Exception>> TryAsync<T>(Func<Task<T>> operation)
```

Async version supporting Task-based operations.

### ResultExtensions - Mixed Pipeline Support

#### TapAsync (Async Result + Sync Action)

```csharp
public static Task<Result<T, TError>> TapAsync<T, TError>(
    this Task<Result<T, TError>> resultTask,
    Action<T> action)
```

Executes a synchronous side effect on a successful async result. Use this when you have a `Task<Result>` and want to perform sync side effects like simple logging.

#### TapErrorAsync (Async Result + Sync Action)

```csharp
public static Task<Result<T, TError>> TapErrorAsync<T, TError>(
    this Task<Result<T, TError>> resultTask,
    Action<TError> action)
```

Executes a synchronous side effect on a failed async result. Use this when you have a `Task<Result>` and want to perform sync error handling like simple logging.

## 💡 Usage Examples

### Example 1: Exception Logging with Full Context

```csharp
// Log exception with full stack trace before transforming
public Result<User, string> GetUserById(int id)
{
    return ResultExtensions.Try(() => {
            var user = _repository.GetById(id);
            if (user == null) throw new KeyNotFoundException($"User {id} not found");
            return user;
        })
        .TapError(ex => _logger.LogError(ex, "Failed to get user {UserId}", id))
        .MapError(ex => ex switch {
            KeyNotFoundException => "User not found",
            SqlException => "Database error",
            _ => "Failed to retrieve user"
        });
}
```

### Example 2: Pattern Matching on Exception Types

```csharp
// Handle different exception types with full exception details
public async Task<Result<Data, string>> FetchDataAsync(string url)
{
    return await ResultExtensions.TryAsync(async () => 
            await _httpClient.GetStringAsync(url))
        .TapErrorAsync(ex => {
            switch (ex)
            {
                case HttpRequestException http:
                    _logger.LogWarning(http, "HTTP error for {Url}: {Status}", 
                        url, http.StatusCode);
                    break;
                case TaskCanceledException timeout:
                    _logger.LogWarning("Request timeout for {Url}", url);
                    break;
                default:
                    _logger.LogError(ex, "Unexpected error for {Url}", url);
                    break;
            }
        })
        .MapErrorAsync(ex => ex switch {
            HttpRequestException => "Network error",
            TaskCanceledException => "Request timeout",
            _ => "Failed to fetch data"
        });
}
```

### Example 3: Mixed Async/Sync Pipelines

```csharp
// Sync logging in async pipeline - now clean and natural
public async Task<Result<Order, string>> ProcessOrderAsync(Order order)
{
    return await ValidateOrderAsync(order)
        .TapAsync(o => _logger.LogInformation("Order validated: {OrderId}", o.Id))
        .BindAsync(o => SaveOrderAsync(o))
        .TapAsync(o => _logger.LogInformation("Order saved: {OrderId}", o.Id))
        .TapErrorAsync(err => _logger.LogError("Order processing failed: {Error}", err))
        .BindAsync(o => ChargePaymentAsync(o));
}
```

### Example 4: Complete Exception Handling Pipeline

```csharp
// Full exception lifecycle: Try → Log → Transform → Handle
public async Task<Result<Invoice, string>> CreateInvoiceAsync(CreateInvoiceRequest request)
{
    return await ResultExtensions.TryAsync(async () => {
            var invoice = await _invoiceService.CreateAsync(request);
            await _database.SaveAsync(invoice);
            return invoice;
        })
        .TapErrorAsync(ex => {
            _logger.LogError(ex, "Invoice creation failed for customer {CustomerId}", 
                request.CustomerId);
            _metrics.RecordException(ex);
        })
        .TapErrorAsync(ex => {
            if (ex is SqlException sqlEx && sqlEx.Number == 2627)
                _alerting.NotifyDuplicateInvoiceAttempt(request);
        })
        .MapErrorAsync(ex => ex switch {
            SqlException sqlEx when sqlEx.Number == 2627 => "Duplicate invoice",
            TimeoutException => "Database timeout",
            _ => $"Failed to create invoice: {ex.Message}"
        });
}
```

### Example 5: File Operations with Specific Exception Handling

```csharp
// Pattern match on file-specific exceptions
public async Task<Result<string, string>> ReadConfigFileAsync(string path)
{
    return await ResultExtensions.TryAsync(async () => 
            await File.ReadAllTextAsync(path))
        .TapErrorAsync(ex => {
            if (ex is FileNotFoundException fnf)
                _logger.LogWarning("Config file missing: {FileName}", fnf.FileName);
            else if (ex is UnauthorizedAccessException)
                _logger.LogError(ex, "Permission denied reading config");
            else
                _logger.LogError(ex, "Failed to read config file");
        })
        .MapErrorAsync(ex => ex switch {
            FileNotFoundException => "Configuration file not found",
            UnauthorizedAccessException => "Permission denied",
            IOException => "Failed to read configuration",
            _ => "Configuration error"
        });
}
```

### Example 6: Database Operations with Retry Logic

```csharp
// Log exceptions while preserving retry capability
public async Task<Result<User, string>> SaveUserWithRetryAsync(User user)
{
    return await ResultExtensions.TryAsync(async () => 
            await _database.SaveAsync(user))
        .TapErrorAsync(ex => {
            _logger.LogWarning(ex, "Database save failed for user {UserId}", user.Id);
            
            if (ex is SqlException sqlEx)
                _metrics.RecordDatabaseError(sqlEx.Number);
        })
        .MapErrorAsync(ex => ex switch {
            SqlException sqlEx when sqlEx.Number == -2 => "Database timeout - retrying...",
            SqlException sqlEx when sqlEx.Number == 2627 => "User already exists",
            _ => "Failed to save user"
        });
}
```

## 🎯 When to Use

### ✅ Use Exception-First Try When:

- You need to log exceptions with full context (stack traces, types)
- Different exception types require different handling
- You want to separate logging from error transformation
- Metrics or alerting need to inspect the raw exception
- You need to pattern match on specific exception subtypes

### ✅ Use Mixed Async/Sync Tap When:

- You have async results but sync side effects (simple logging)
- Logging APIs are synchronous (most are)
- Side effects don't require async operations
- You want cleaner pipeline composition

### ✅ Perfect For:

- **Exception logging** - "Log this exception with full details before transforming"
- **Pattern matching** - "Handle FileNotFoundException differently than SqlException"
- **Metrics** - "Record exception types for monitoring"
- **Simple logging** - "Just write to console/logger in async pipeline"
- **Debugging** - "Inspect values in async pipelines without ceremony"

### ❌ Don't Use Exception-First Try When:

- You don't need exception details (use existing `Try<T, TError>` instead)
- You're just transforming to a custom error (existing overload is cleaner)
- No logging or pattern matching needed

## 🔄 Comparison with Other Methods

### Exception-First Try vs Original Try

| Feature | Try<T, TError> (Original) | Try<T> (Exception-First) |
|---------|---------------------------|--------------------------|
| Error Type | Custom (TError) | Exception |
| Use Case | Direct transformation | Log then transform |
| Composability | MapError | TapError → MapError |
| Exception Access | No (only via factory) | Yes (in TapError) |

**When to use which:**

```csharp
// Simple transformation - use original
Try(() => operation, ex => ex.Message)

// Need to log exception - use exception-first
Try(() => operation)
    .TapError(ex => _logger.LogError(ex, "Failed"))
    .MapError(ex => ex.Message)
```

### Mixed Pipeline Overloads

| Method | Signature | Use Case |
|--------|-----------|----------|
| TapAsync (new) | Task<Result> + Action | Sync logging in async pipeline |
| TapAsync (existing) | Task<Result> + Func<Task> | Async operations in async pipeline |
| TapErrorAsync (new) | Task<Result> + Action | Sync error logging in async pipeline |
| TapErrorAsync (existing) | Task<Result> + Func<Task> | Async error handling in async pipeline |

## 🔧 Migration from 1.5.x

**No migration needed!** All changes are backwards compatible.

### What Continues to Work:

```csharp
// All existing Try usage unchanged
var result = ResultExtensions.Try(
    () => int.Parse("42"),
    ex => $"Parse failed: {ex.Message}"
);

// All existing async taps unchanged
await GetDataAsync()
    .TapAsync(async x => await ProcessAsync(x));
```

### What You Can Now Do:

```csharp
// Exception-first Try with clean logging
var result = ResultExtensions.Try(() => int.Parse("invalid"))
    .TapError(ex => _logger.LogError(ex, "Parse failed"))
    .MapError(ex => "Invalid number");

// Sync actions in async pipelines
await GetDataAsync()
    .TapAsync(x => Console.WriteLine(x))  // Sync action - clean!
    .TapErrorAsync(err => _logger.LogError(err));  // Sync error logging!
```

### Before vs After:

**Before (1.5.x):**

```csharp
// Logging and transformation mixed
var result = ResultExtensions.Try(
    () => File.ReadAllText("file.txt"),
    ex => {
        _logger.LogError(ex, "Read failed");  // Logging
        return "Failed to read file";  // Transformation
    }
);

// Awkward sync actions in async pipelines
await GetDataAsync()
    .TapErrorAsync(ex => Task.FromResult(_logger.LogError(ex, "Failed")));
```

**After (1.6.0):**

```csharp
// Logging and transformation separated
var result = ResultExtensions.Try(() => File.ReadAllText("file.txt"))
    .TapError(ex => _logger.LogError(ex, "Read failed"))  // Logging
    .MapError(ex => "Failed to read file");  // Transformation

// Natural sync actions in async pipelines
await GetDataAsync()
    .TapErrorAsync(ex => _logger.LogError(ex, "Failed"));  // Clean!
```

## 📖 Documentation Updates

- Added Try<T> / TryAsync<T> section to README.md
- Added exception-first pattern examples
- Added mixed async/sync pipeline examples
- Updated API reference with 4 new method signatures
- Added comparison table for Try overloads
- Updated best practices for exception handling

## 📋 Changelog

### Added

- **Try<T>** - Exception-first overload returning `Result<T, Exception>`
- **TryAsync<T>** - Async exception-first overload
- **TapAsync** overload - Task<Result> + sync Action
- **TapErrorAsync** overload - Task<Result> + sync Action
- Comprehensive XML documentation for all new methods
- 24 unit tests covering all scenarios
- Package tags: exception-handling, async-patterns

### Changed

- None (no breaking changes)

### Fixed

- None

### Removed

- None

## 🎓 Best Practices

### DO:

✅ Use exception-first Try when you need to log or pattern match exceptions  
✅ Use TapError to log exceptions before MapError to transform them  
✅ Use sync actions in async pipelines when the action is truly synchronous  
✅ Pattern match on specific exception types for targeted handling  
✅ Separate logging (TapError) from transformation (MapError)  
✅ Use existing Try<T, TError> when you don't need exception details

### DON'T:

❌ Mix logging and transformation in the error factory  
❌ Use exception-first Try if you don't need exception inspection  
❌ Wrap sync actions in Task.FromResult (use new overloads)  
❌ Put async operations in sync action overloads  
❌ Lose exception details by transforming too early

## 💡 Pattern Examples

### Pattern 1: Exception Logging Pipeline

```csharp
// Log → Transform → Handle
ResultExtensions.Try(() => riskyOperation())
    .TapError(ex => _logger.LogError(ex, "Operation failed"))
    .MapError(ex => "User-friendly message")
    .Match(
        success => ProcessSuccess(success),
        error => HandleError(error)
    );
```

### Pattern 2: Exception Type Handling

```csharp
// Different handling for different exception types
ResultExtensions.Try(() => databaseOperation())
    .TapError(ex => {
        switch (ex)
        {
            case SqlException sql:
                _metrics.RecordDatabaseError(sql.Number);
                break;
            case TimeoutException:
                _metrics.IncrementTimeout();
                break;
            default:
                _metrics.RecordUnexpectedError();
                break;
        }
    })
    .MapError(ex => ex switch {
        SqlException => "Database error",
        TimeoutException => "Operation timed out",
        _ => "Unexpected error"
    });
```

### Pattern 3: Clean Async Logging

```csharp
// Sync logging in async pipeline
await FetchDataAsync()
    .TapAsync(data => _logger.LogInformation("Fetched: {Data}", data))
    .BindAsync(data => ProcessDataAsync(data))
    .TapAsync(result => _logger.LogInformation("Processed: {Result}", result))
    .TapErrorAsync(err => _logger.LogError("Failed: {Error}", err));
```

### Pattern 4: Multi-Concern Exception Handling

```csharp
// Log, metric, alert - all separated
await TryAsync(() => criticalOperation())
    .TapErrorAsync(ex => _logger.LogError(ex, "Critical failure"))
    .TapErrorAsync(ex => _metrics.RecordException(ex))
    .TapErrorAsync(ex => _alerting.NotifyOpsTeam(ex))
    .MapErrorAsync(ex => "Operation failed");
```

## 🔗 Related Methods

### Exception Handling
- **Try<T, TError>** - Direct exception to custom error transformation
- **TryAsync<T, TError>** - Async version with custom error
- **TapError** - Log errors without transformation

### Pipeline Composition
- **Map / MapAsync** - Transform success values
- **Bind / BindAsync** - Chain operations
- **Tap / TapAsync** - Success side effects

## 🚀 Next Steps

1. **Update your package:**

```bash
dotnet add package BindSharp --version 1.6.0
```

2. **Refactor exception logging** to use exception-first Try
3. **Simplify async pipelines** with sync action overloads
4. **Add pattern matching** for specific exception types
5. **Separate logging from transformation** for cleaner code

## 🙏 Acknowledgments

Thanks to the community for:
- Requesting better exception inspection in Try operations
- Identifying the async/sync pipeline friction
- Providing real-world scenarios that guided this design

## 📚 Additional Resources

- **GitHub:** [https://github.com/GDB-11/BindSharp/](https://github.com/GDB-11/BindSharp/)
- **Documentation:** See README.md for complete usage guide
- **Previous Releases:**
    - [1.5.0](ReleaseNotes/RELEASE_NOTES_1.5.0.md) - Error-specific side effects (TapError)
    - [1.4.1](ReleaseNotes/RELEASE_NOTES_1.4.1.md) - Conditional processing (BindIf)
    - [1.3.0](ReleaseNotes/RELEASE_NOTES_1.3.0.md) - Equality & implicit conversions
    - [1.2.0](ReleaseNotes/RELEASE_NOTES_1.2.0.md) - Unit type
    - [1.1.0](ReleaseNotes/RELEASE_NOTES_1.1.0.md) - ResultExtensions utilities

## 🎉 Conclusion

Version 1.6.0 brings powerful exception handling and pipeline flexibility to BindSharp:

- ✅ Exception-first Try for clean exception logging and pattern matching
- ✅ Mixed async/sync pipelines for natural composition
- ✅ Full async support (4 new overloads)
- ✅ Zero breaking changes
- 🎯 Cleaner separation of concerns in error handling
- 🎯 More natural async pipeline composition

Upgrade today and enjoy cleaner exception handling! 🚀
