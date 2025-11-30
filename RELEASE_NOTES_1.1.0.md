# BindSharp 1.1.0 - Integration Guide

## 🎉 What's New in Version 1.1.0

This release adds **ResultExtensions** - a powerful set of utility methods that complement the existing functional operations.

### New Features

#### 1. **Exception Handling** (`Try` / `TryAsync`)
Convert exception-based code into Results:

```csharp
// Before: Exception-based
int value = int.Parse(userInput); // Throws on invalid input

// After: Result-based
var result = ResultExtensions.Try(
    () => int.Parse(userInput),
    ex => $"Invalid number: {ex.Message}"
);
// Returns: Result<int, string>
```

```csharp
// Async version
var user = await ResultExtensions.TryAsync(
    async () => await httpClient.GetStringAsync(url),
    ex => $"HTTP request failed: {ex.Message}"
);
```

#### 2. **Validation** (`Ensure` / `EnsureNotNull`)
Validate results in your pipeline:

```csharp
var result = GetAge()
    .Ensure(age => age >= 18, "Must be 18 or older")
    .Ensure(age => age <= 120, "Invalid age");

// Null checking
Result<User?, string> maybeUser = FindUser(id);
Result<User, string> user = maybeUser.EnsureNotNull("User not found");
```

#### 3. **Side Effects** (`Tap` / `TapAsync`)
Execute side effects without breaking your pipeline:

```csharp
var result = await GetUserAsync()
    .TapAsync(async u => await LogActivityAsync(u))
    .TapAsync(async u => await SendNotificationAsync(u))
    .MapAsync(u => u.ToDto());
// Logs and sends notification, then maps to DTO
```

#### 4. **Nullable Conversion** (`ToResult`)
Convert nullable values to Results:

```csharp
string? cacheValue = cache.Get("key");
var result = cacheValue.ToResult("Not found in cache");
// Result<string, string>
```

#### 5. **Resource Management** (`Using` / `UsingAsync`)
Functional resource management with guaranteed disposal:

```csharp
var data = OpenFile("data.txt")
    .Using(stream => ReadData(stream))
    .Map(data => ProcessData(data));
// stream is automatically disposed

// Async version
var data = await OpenFileAsync("data.txt")
    .UsingAsync(async stream => await ReadDataAsync(stream));
```

#### 6. **Task Conversion** (`AsTask`)
Convert sync Results to async when needed:

```csharp
Result<int, string> syncResult = Validate(value);
Task<Result<int, string>> asyncResult = syncResult.AsTask();
```

---

## 📦 Installation

```bash
dotnet add package BindSharp --version 1.1.0
```

Or update your `.csproj`:

```xml
<PackageReference Include="BindSharp" Version="1.1.0" />
```

---

## 🔄 Migration from 1.0.0

**No breaking changes!** All existing code will continue to work. The new methods are purely additive.

If you were using a custom `ResultExtensions` class (like in your test project), you may want to:

1. Remove your custom implementation
2. Update the namespace from `Global.Helpers.Functional` to `BindSharp`
3. Update method signatures to use generic error types:

### Before (your custom code):
```csharp
public static Result<T, string> Try<T>(Func<T> operation, string errorMessage)
{
    try
    {
        return Result<T, string>.Success(operation());
    }
    catch (Exception ex)
    {
        return Result<T, string>.Failure($"{errorMessage} Details: {ex.Message}");
    }
}
```

### After (BindSharp 1.1.0):
```csharp
var result = ResultExtensions.Try(
    () => operation(),
    ex => $"{errorMessage} Details: {ex.Message}"
);
```

The new version is more flexible - you can use any error type, not just `string`.

---

## 💡 Complete Usage Examples

### Example 1: API Call with Error Handling

```csharp
using BindSharp;

public async Task<Result<UserDto, string>> GetUserAsync(int userId)
{
    return await ResultExtensions.TryAsync(
            async () => await httpClient.GetStringAsync($"/api/users/{userId}"),
            ex => $"Failed to fetch user: {ex.Message}"
        )
        .BindAsync(json => ResultExtensions.Try(
            () => JsonSerializer.Deserialize<User>(json),
            ex => $"Failed to parse user: {ex.Message}"
        ))
        .EnsureNotNullAsync("User data was null")
        .TapAsync(async user => await LogUserAccessAsync(user))
        .MapAsync(user => user.ToDto());
}
```

### Example 2: File Processing Pipeline

```csharp
public async Task<Result<ProcessedData, string>> ProcessFileAsync(string path)
{
    return ResultExtensions.Try(
            () => File.OpenRead(path),
            ex => $"Cannot open file: {ex.Message}"
        )
        .UsingAsync(async stream => 
            await ResultExtensions.TryAsync(
                async () => await ReadDataAsync(stream),
                ex => $"Cannot read data: {ex.Message}"
            )
        )
        .Ensure(data => data.Length > 0, "File is empty")
        .TapAsync(async data => await LogProcessingAsync(data))
        .MapAsync(data => Process(data));
}
```

### Example 3: Form Validation

```csharp
public Result<CreateUserCommand, ValidationError> ValidateUserForm(UserForm form)
{
    return form.Email.ToResult(ValidationError.Required("Email"))
        .Ensure(
            email => email.Contains("@"),
            ValidationError.Invalid("Email must contain @")
        )
        .Bind(email => form.Age.ToResult(ValidationError.Required("Age"))
            .Ensure(
                age => age >= 18,
                ValidationError.Invalid("Must be 18 or older")
            )
            .Map(age => new CreateUserCommand(email, age))
        );
}
```

### Example 4: Database Transaction

```csharp
public async Task<Result<Order, string>> CreateOrderAsync(CreateOrderDto dto)
{
    return await ResultExtensions.TryAsync(
            async () => await dbContext.Database.BeginTransactionAsync(),
            ex => $"Failed to begin transaction: {ex.Message}"
        )
        .UsingAsync(async transaction =>
            await CreateOrderEntityAsync(dto)
                .TapAsync(async order => await SaveInventoryAsync(order))
                .TapAsync(async order => await SendEmailAsync(order))
                .TapAsync(async _ => await transaction.CommitAsync())
                .MapErrorAsync(async error =>
                {
                    await transaction.RollbackAsync();
                    return error;
                })
        );
}
```

---

## 📖 Method Reference

### Exception Handling
| Method | Signature | Purpose |
|--------|-----------|---------|
| `Try` | `Func<T> → Result<T, TError>` | Wrap exception-prone sync code |
| `TryAsync` | `Func<Task<T>> → Task<Result<T, TError>>` | Wrap exception-prone async code |

### Validation
| Method | Signature | Purpose |
|--------|-----------|---------|
| `Ensure` | `(T → bool) → Result<T, TError>` | Validate a condition |
| `EnsureAsync` | `Task<Result> → (T → bool) → Task<Result>` | Async validation |
| `EnsureNotNull` | `Result<T?, TError> → Result<T, TError>` | Ensure non-null |
| `EnsureNotNullAsync` | `Task<Result<T?, TError>> → Task<Result<T, TError>>` | Async non-null check |

### Side Effects
| Method | Signature | Purpose |
|--------|-----------|---------|
| `Tap` | `(T → void) → Result<T, TError>` | Execute side effect |
| `TapAsync` | `(T → Task) → Task<Result<T, TError>>` | Execute async side effect |

### Conversion
| Method | Signature | Purpose |
|--------|-----------|---------|
| `ToResult` | `T? → Result<T, TError>` | Convert nullable to Result |
| `AsTask` | `Result<T, TError> → Task<Result<T, TError>>` | Convert to Task |

### Resource Management
| Method | Signature | Purpose |
|--------|-----------|---------|
| `Using` | `Result<IDisposable> → (T → Result) → Result` | Sync resource management |
| `UsingAsync` | `Result<IDisposable> → (T → Task<Result>) → Task<Result>` | Async resource management |

---

## 🎯 Best Practices

### 1. Use Generic Error Types
```csharp
// ❌ Don't lock yourself into string errors
public static Result<T, string> Try<T>(Func<T> op, string msg) { ... }

// ✅ Allow any error type
public static Result<T, TError> Try<T, TError>(
    Func<T> op, 
    Func<Exception, TError> errorFactory
) { ... }
```

### 2. Combine with Existing Methods
```csharp
var result = await GetIdAsync()
    .BindAsync(id => FetchUserAsync(id))     // Existing method
    .EnsureNotNullAsync("User not found")    // New method
    .TapAsync(u => LogUserAsync(u))          // New method
    .MapAsync(u => u.ToDto());               // Existing method
```

### 3. Use Resource Management for Safety
```csharp
// ❌ Manual disposal (error-prone)
var streamResult = OpenFile(path);
if (streamResult.IsSuccess)
{
    try
    {
        var data = ReadData(streamResult.Value);
        streamResult.Value.Dispose();
        return data;
    }
    catch
    {
        streamResult.Value.Dispose();
        throw;
    }
}

// ✅ Automatic disposal with Using
return OpenFile(path)
    .Using(stream => ReadData(stream));
```

---

## 📝 Changelog

### Version 1.1.0 (2024)
**Added:**
- `Try<T, TError>` - Exception handling for synchronous operations
- `TryAsync<T, TError>` - Exception handling for asynchronous operations
- `Ensure<T, TError>` - Result validation
- `EnsureAsync<T, TError>` - Async result validation
- `EnsureNotNull<T, TError>` - Null checking for reference types
- `EnsureNotNullAsync<T, TError>` - Async null checking
- `ToResult<T, TError>` - Convert nullable to Result
- `Tap<T, TError>` - Synchronous side effects
- `TapAsync<T, TError>` - Asynchronous side effects (2 overloads)
- `Using<TResource, TResult, TError>` - Resource management
- `UsingAsync<TResource, TResult, TError>` - Async resource management
- `AsTask<T, TError>` - Convert Result to Task<Result>

**Changed:**
- None (no breaking changes)

**Fixed:**
- None

### Version 1.0.0 (Initial Release)
- Core `Result<T, TError>` type
- `FunctionalResult` extensions (Map, Bind, MapError, Match)
- `AsyncFunctionalResult` extensions for async operations
- Railway-oriented programming support

---

## 🚀 Next Steps

1. **Update your package:**
   ```bash
   dotnet add package BindSharp --version 1.1.0
   ```

2. **Remove custom implementations** if you have any similar methods

3. **Update namespaces** from custom to `BindSharp`

4. **Refactor to use generic error types** instead of string-only

5. **Explore the examples** to see patterns you can apply

---

## 🤝 Contributing

Found a bug or have a suggestion? Please open an issue on GitHub!

## 📄 License

[Your License Here]