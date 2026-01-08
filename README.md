# BindSharp

A lightweight, powerful functional programming library for .NET that makes error handling elegant and composable. Say goodbye to messy try-catch blocks and hello to Railway-Oriented Programming! 🚂

## Why BindSharp?

Traditional error handling is messy:
```csharp
try
{
    var data = await FetchDataAsync();
    
    try {
        var validated = ValidateData(data);
        var transformed = TransformData(validated);
        return await SaveAsync(transformed);
    }
    catch (ValidationException vex) {
        _logger.LogError(vex, "Validation failed");
        throw; // Loses context, can't compose further
    }
}
catch (HttpRequestException hex)
{
    _logger.LogError(hex, "Network error");
    throw; // Caller has to deal with it
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unknown error");
    throw;
}
```

With BindSharp, it's clean and composable:
```csharp
using BindSharp;
using BindSharp.Extensions;

return await Result.TryAsync(() => FetchDataAsync())
    .TapErrorAsync(ex => _logger.LogError(ex, "Fetch failed"))  // Sync logging in async pipeline
    .MapErrorAsync(ex => "Network error")    
    .BindAsync(ValidateDataAsync)
    .TapErrorAsync(err => _logger.LogError("Validation failed: {Error}", err))  // Sync logging    
    .BindIfAsync(  // Conditional processing
        data => data.RequiresTransformation,
        async data => await TransformDataAsync(data)
    )    
    .BindAsync(SaveAsync)
    .DoAsync(  // Combined side effects
        data => _logger.LogInfo("Saved: {Data}", data),
        error => _logger.LogError("Save failed: {Error}", error)
    )    
    .MatchAsync(
        success => $"✅ Saved: {success}",
        error => $"❌ Failed: {error}"
    );
```

## ✨ What's New in 2.0.0

**Major Release - Breaking Changes** 🚀

- 🔥 **Do/DoAsync** - Execute different side effects for success/failure in one method (killer feature!)
- ✅ **Cleaner API** - `Result.Try()` instead of `ResultExtensions.Try()`
- ✅ **Better Organization** - Extension methods in `BindSharp.Extensions` namespace
- ⚠️ **Breaking Changes** - See [MIGRATION_V2.md](MIGRATION_V2.md) for upgrade guide

**Migration is simple:**
1. Add `using BindSharp.Extensions;` to files using extension methods
2. Replace `ResultExtensions.Try` → `Result.Try`
3. (Optional) Refactor `Tap + TapError` pairs to `Do` for cleaner code

**Previous Releases:**
- **Version 1.6.0** added [Exception-First Try](#exception-first-try---clean-exception-handling) & Mixed Async/Sync Pipelines
- **Version 1.5.0** added [TapError - Error-Specific Side Effects](#taperror---error-specific-side-effects)
- **Version 1.4.1** added [BindIf - Conditional Processing](#bindif---conditional-processing)
- **Version 1.3.0** added [Equality Support & Implicit Conversions](#equality-support)
- **Version 1.2.0** added the [Unit Type](#unit-type---representing-no-value)
- **Version 1.1.0** added [Result Utilities](#result-utilities---utilities-for-the-real-world)

## Features

✅ **Result<T, TError>** - Explicit success/failure handling  
🔥 **Do/DoAsync** - Dual side effects in one method (new in 2.0!)  
✅ **Exception-First Try** - Clean exception inspection and logging  
✅ **Mixed Async/Sync Pipelines** - Natural composition  
✅ **BindIf** - Conditional processing in pipelines  
✅ **Equality Support** - Compare Results, use in collections  
✨ **Implicit Conversions** - Clean, concise syntax  
✅ **Unit Type** - Represent "no value" in functional pipelines  
✅ **Railway-Oriented Programming** - Chain operations that can fail  
✅ **Full Async/Await Support** - Game-changing async composition  
✅ **Exception Handling** - Convert try/catch into functional Results  
✅ **Validation Pipelines** - Business rule checking without breaking flow  
✅ **Side Effects** - Tap into pipelines for logging and metrics  
✅ **Resource Management** - Guaranteed disposal with functional style  
✅ **Type-Safe** - Compiler catches your mistakes  
✅ **Lightweight** - Zero dependencies  
✅ **Compatible** - Works with .NET Framework 4.6.1+ and all modern .NET

## Installation
```bash
dotnet add package BindSharp
```

## Quick Start

### Basic Success and Failure
```csharp
using BindSharp;

// Create results - explicit style
var success = Result<int, string>.Success(42);
var failure = Result<int, string>.Failure("Something went wrong");

// Or use implicit conversions (new in 1.3.0!)
Result<int, string> success2 = 42;
Result<int, string> failure2 = "Error occurred";

// Check the result
if (success.IsSuccess)
    Console.WriteLine(success.Value); // 42

if (failure.IsFailure)
    Console.WriteLine(failure.Error); // "Something went wrong"

// Compare results (new in 1.3.0!)
if (success == success2)  // ✅ TRUE!
    Console.WriteLine("Results are equal!");
```

## Equality Support

**New in 1.3.0!** Results now implement `IEquatable<Result<T, TError>>` for proper value equality:
```csharp
using BindSharp;

var r1 = Result<int, string>.Success(42);
var r2 = Result<int, string>.Success(42);

// Equality comparison works!
Console.WriteLine(r1 == r2);  // TRUE ✅
Console.WriteLine(r1.Equals(r2));  // TRUE ✅

// Works in collections
var set = new HashSet<Result<int, string>>();
set.Add(Result<int, string>.Success(1));
set.Add(Result<int, string>.Success(1));  // Not added (duplicate)
Console.WriteLine(set.Count);  // 1 ✅

// Use as dictionary keys
var cache = new Dictionary<Result<int, string>, string>();
cache[Result<int, string>.Success(1)] = "one";

// Better debugging
Console.WriteLine(r1);  // "Success(42)"
Console.WriteLine(failure);  // "Failure(Something went wrong)"
```

### In Tests
```csharp
[Fact]
public void Divide_ReturnsCorrectResult()
{
    var result = Calculator.Divide(10, 2);
    var expected = Result<int, string>.Success(5);
    
    Assert.Equal(expected, result);  // ✅ Now works!
}
```

## Implicit Conversions - Cleaner Syntax

**New in 1.3.0!** Return values and errors directly without wrapping them:

### Simple Example
```csharp
using BindSharp;

// Before: Verbose
public Result<int, string> ParseAge(string input)
{
    if (string.IsNullOrWhiteSpace(input))
        return Result<int, string>.Failure("Age is required");
    
    if (!int.TryParse(input, out int age))
        return Result<int, string>.Failure("Must be a number");
    
    if (age < 0 || age > 150)
        return Result<int, string>.Failure("Invalid age");
    
    return Result<int, string>.Success(age);
}

// After: Clean! (50% less code)
public Result<int, string> ParseAge(string input)
{
    if (string.IsNullOrWhiteSpace(input)) return "Age is required";
    if (!int.TryParse(input, out int age)) return "Must be a number";
    if (age < 0 || age > 150) return "Invalid age";
    
    return age;
}
```

### Switch Expressions
```csharp
public Result<decimal, string> GetDiscount(string code)
{
    return code.ToUpper() switch
    {
        "SAVE10" => 0.10m,  // ✨ Implicit Success
        "SAVE20" => 0.20m,  // ✨ Implicit Success
        "SAVE50" => 0.50m,  // ✨ Implicit Success
        _ => "Invalid coupon code"  // ✨ Implicit Failure
    };
}
```

### Async Operations
```csharp
using BindSharp;

public async Task<Result<User, string>> GetUserAsync(int id)
{
    if (id < 0) return "Invalid ID";  // ✨ Clean!
    
    var user = await _db.FindUserAsync(id);
    if (user == null) return "User not found";  // ✨ Clean!
    
    return user;  // ✨ Clean!
}
```

### Real-World Example
```csharp
public Result<User, string> CreateUser(CreateUserRequest request)
{
    if (request == null) return "Request is null";
    if (string.IsNullOrEmpty(request.Email)) return "Email is required";
    if (string.IsNullOrEmpty(request.Password)) return "Password is required";
    if (request.Password.Length < 8) return "Password too short";
    
    return new User(request);  // ✨ 53% less code than before!
}
```

## ⚠️ CRITICAL: Implicit Conversions Warning

**NEVER use the same type for both `T` and `TError`** - this creates ambiguity:
```csharp
// ❌ NEVER DO THIS - Ambiguous!
public Result<string, string> GetValue()
{
    return "value";  // Is this Success or Failure? Compiler can't tell!
}

// ✅ ALWAYS DO THIS - Clear!
public Result<int, string> GetValue()
{
    if (error) return "Error message";  // Clear: string = Failure
    return 42;  // Clear: int = Success
}

// ✅ OR USE CUSTOM ERROR TYPE - Even Better!
public record ErrorInfo(string Message);

public Result<string, ErrorInfo> GetValue()
{
    if (error) return new ErrorInfo("Error");  // Clear: ErrorInfo = Failure
    return "Success value";  // Clear: string = Success
}
```

**Best Practice:** Define custom error types for your domain:
```csharp
public record ValidationError(string Field, string Message);
public record NotFoundError(string EntityType, string Id);
public record UnauthorizedError(string Reason);

public Result<User, ValidationError> ValidateUser(UserInput input);
public Result<Order, NotFoundError> GetOrder(string orderId);
public Result<Resource, UnauthorizedError> AccessResource(string userId);
```

This approach:
- ✅ Eliminates ambiguity completely
- ✅ Makes errors type-safe
- ✅ Enables better error handling
- ✅ Improves code documentation

## Unit Type - Representing "No Value"

Many operations succeed but don't produce a meaningful value. The `Unit` type lets you maintain consistent `Result<T, TError>` signatures in these cases:

### The Problem
```csharp
// ❌ Without Unit: inconsistent return types
public async Task DeleteUserAsync(int id);      // void? Task? bool?
public bool UpdateSettings(Settings s);         // What does 'true' mean?
public int InsertRecord(Record r);              // Returning affected rows... but do we care?

// These can't be composed in Result chains
```

### The Solution
```csharp
using BindSharp;

// ✅ With Unit: consistent Result<Unit, TError> signatures everywhere
public Task<Result<Unit, string>> DeleteUserAsync(int id) =>
    Result.TryAsync(
        operation: async () => {
            await _repository.DeleteAsync(id);
            return Unit.Value;  // T = Unit (success, no value)
        },
        errorFactory: ex => $"Delete failed: {ex.Message}"
    );
```

### Real-World Example: CRUD Operations Chain
```csharp
using BindSharp;
using BindSharp.Extensions;

// Every operation returns Result<Unit, string> for perfect composition
public async Task<Result<Unit, string>> CreateUserWorkflowAsync(CreateUserRequest request)
{
    return await ValidateRequest(request)                    // Result<CreateUserRequest, string>
        .BindAsync(r => InsertUserAsync(r))                  // Result<Unit, string>
        .TapAsync(_ => SendWelcomeEmailAsync(r.Email))       // Result<Unit, string> (unchanged)
        .BindAsync(_ => InitializePreferencesAsync(r.UserId)) // Result<Unit, string>
        .TapAsync(_ => _logger.LogInfo("User workflow completed"));
    
    // Clean chain - consistent Result<Unit, string> throughout
}

private async Task<Result<Unit, string>> InsertUserAsync(CreateUserRequest request) =>
    await Result.TryAsync(
        operation: async () => {
            await _database.ExecuteAsync("INSERT INTO Users ...", request);
            return Unit.Value;  // Implicit conversion works here too!
        },
        errorFactory: ex => $"Database error: {ex.Message}"
    );
```

### When to Use Unit

✅ **Database operations** - Inserts, updates, deletes that return void or row counts you don't need  
✅ **Notifications** - Sending emails, SMS, push notifications  
✅ **Validation** - Checks that produce no output, only pass/fail  
✅ **Side effects** - Logging, caching, metrics wrapped in Results  
✅ **Void replacements** - Any operation where success matters, not the return value

### Performance

`Unit.Value` is a singleton with almost zero memory footprint. There's no performance cost to using it - perfect for high-throughput functional pipelines!

## Core Operations

### Map - Transform Success Values

Transform a value when the result is successful:
```csharp
using BindSharp;

Result<int, string> GetAge() => 25;

var result = GetAge()
    .Map(age => age * 2)  // 50
    .Map(age => $"Age in months: {age * 12}");  // "Age in months: 600"

// If GetAge() returned a failure, Map would skip and propagate the error
```

**Real-world example - API response transformation:**
```csharp
public Result<UserDto, string> GetUser(int id)
{
    var userResult = _database.FindUser(id); // Returns Result<User, string>
    
    return userResult.Map(user => new UserDto
    {
        Id = user.Id,
        FullName = $"{user.FirstName} {user.LastName}",
        Email = user.Email
    });
}
```

### Bind - Chain Operations That Can Fail

Chain multiple operations where each can fail:
```csharp
using BindSharp;
using BindSharp.Extensions;

public abstract record EmailError(string Message, string? Details = null, Exception? Exception = null);

public sealed record EmailValidationError(string? Details = null)
    : EmailError("Invalid email", Details);
    
public sealed record SendEmailError(string? Details = null, Exception? Exception = null)
    : EmailError("Failed to send email", Details, Exception);

Result<string, EmailError> ValidateEmail(string email) =>
    email.Contains("@")
        ? email
        : new EmailValidationError("Email must contain '@' character");

Result<string, EmailError> SendEmail(string email) =>
    Result.Try(() =>
    {
        if (email.EndsWith("@blocked.com"))
            throw new InvalidOperationException("Domain is blocked");
        
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));
        
        // Simulate actual sending...
        Console.WriteLine($"📧 Sending email to {email}...");
        return $"Sent to {email}";
    })
    .TapError(ex => 
    {
        Console.WriteLine($"🔴 Email send error: {ex.GetType().Name} - {ex.Message}");
        if (ex.StackTrace != null)
            Console.WriteLine($"   Stack: {ex.StackTrace.Split('\n')[0]}");
    })
    .MapError(ex => ex switch
    {
        InvalidOperationException => new SendEmailError("Domain is blocked", ex),
        ArgumentException => new SendEmailError("Invalid email format", ex),
        _ => new SendEmailError("Unexpected error occurred", ex)
    });

// ✅ Success case
var successResult = ValidateEmail("user@example.com")
    .Bind(SendEmail);

Console.WriteLine(successResult.Match(
    success => $"✅ {success}",
    error => $"❌ {error.Message}" + (!string.IsNullOrEmpty(error.Details) ? $": {error.Details}" : string.Empty)
));
// Output: 
// 📧 Sending email to user@example.com...
// ✅ Sent to user@example.com

// ❌ Validation failure case
var validationFailure = ValidateEmail("invalid-email")
    .Bind(SendEmail);  // Never executes - validation failed

Console.WriteLine(validationFailure.Match(
    success => $"✅ {success}",
    error => $"❌ {error.Message}" + (!string.IsNullOrEmpty(error.Details) ? $": {error.Details}" : string.Empty)
));
// Output: ❌ Invalid email: Email must contain '@' character

// ❌ Send failure case - shows exception logging!
var sendFailure = ValidateEmail("user@blocked.com")
    .Bind(SendEmail);

Console.WriteLine(sendFailure.Match(
    success => $"✅ {success}",
    error => $"❌ {error.Message}" + (error.Details != null ? $": {error.Details}" : "")
));
// Output:
// 🔴 Email send error: InvalidOperationException - Domain is blocked
//    Stack: at SendEmail(String email) in ...
// ❌ Failed to send email: Domain is blocked
```

**Real-world example - User registration flow:**
```csharp
public Result<User, string> RegisterUser(string email, string password)
{
    return ValidateEmail(email)
        .Bind(validEmail => ValidatePassword(password)
            .Map(_ => validEmail))  // Keep the email, discard password validation result
        .Bind(validEmail => CreateUser(validEmail, password))
        .Bind(user => SendWelcomeEmail(user));
}

// Each step only runs if the previous succeeded!
// First failure stops the chain and returns the error
```

### Match - Handle Both Cases

Extract a value by handling both success and failure:
```csharp
var result = DivideNumbers(10, 2);

var message = result.Match(
    success => $"Result: {success}",
    error => $"Error: {error}"
);

Console.WriteLine(message); // "Result: 5" or "Error: Division by zero"
```

**Real-world example - API response:**
```csharp
public IActionResult GetProduct(int id)
{
    var result = _productService.FindProduct(id);
    
    return result.Match(
        product => Ok(product),           // 200 OK with product
        error => NotFound(new { error })  // 404 Not Found with error message
    );
}
```

### MapError - Transform Error Values

Change the error type while preserving success:
```csharp
using BindSharp;

Result<int, string> result = "404";

var transformed = result.MapError(errorCode => new 
{
    Code = int.Parse(errorCode),
    Message = "Resource not found"
});
// Result<int, { Code, Message }>
```

**Real-world example - Error localization:**
```csharp
public Result<Order, LocalizedError> GetOrder(int id)
{
    return _orderRepository.FindOrder(id)  // Returns Result<Order, string>
        .MapError(errorCode => new LocalizedError
        {
            Code = errorCode,
            Message = _localizer.GetString(errorCode),
            Timestamp = DateTime.UtcNow
        });
}
```

### BindIf - Conditional Processing

**New in 1.4.1!** Execute operations conditionally based on a predicate:
```csharp
using BindSharp;

// Process data only if it needs processing
var result = GetData()
    .BindIf(
        data => data.RequiresProcessing,  // If TRUE
        data => ProcessData(data)         // Then execute
    );
// If predicate is FALSE, returns data unchanged
```

**How it works:**
- Predicate returns **TRUE** → Continuation executes
- Predicate returns **FALSE** → Original result returned unchanged (short-circuit)
- Result is already failed → Error propagates without evaluating predicate

**Real-world example - Conditional enrichment:**
```csharp
using BindSharp;
using BindSharp.Extensions;

public async Task<Result<User, string>> GetUserAsync(int id)
{
    return await FetchUserAsync(id)
        .BindIfAsync(
            user => !user.IsComplete,  // If incomplete (TRUE)
            async user => await EnrichFromDatabaseAsync(user)  // Then enrich
        )
        .TapAsync(async user => await CacheUserAsync(user));
}
```

**Example - JSON extraction:**
```csharp
// Extract JSON only if it's NOT already in JSON format
var result = GetPayload()
    .Map(p => p.TrimStart())
    .BindIf(
        p => !(p.StartsWith("{") || p.StartsWith("[")),  // If NOT JSON (TRUE)
        p => ExtractJsonAfterPrefix(p)                    // Then extract
    );
```

**With async predicates (database checks):**
```csharp
using BindSharp;
using BindSharp.Extensions;

public async Task<Result<Order, string>> ProcessOrderAsync(Order order)
{
    return await Result<Order, string>.Success(order)
        .BindIfAsync(
            async o => await RequiresValidationAsync(o.Id),  // Async check
            async o => await ValidateOrderAsync(o)           // Then validate
        );
}
```

**Key Difference from Ensure:**
- `Ensure` - Validates and **fails** if condition is false
- `BindIf` - **Executes continuation** if condition is true, skips if false

## 🚀 Async Support - The Game Changer!

This is where BindSharp really shines! Handle async operations with the same elegant composition.

### MapAsync - Async Transformations

Three overloads for every scenario:
```csharp
using BindSharp;
using BindSharp.Extensions;

// 1. Task<Result> + sync function
Task<Result<int, string>> asyncResult = GetUserIdAsync();
var user = await asyncResult.MapAsync(id => GetUserFromCache(id));

// 2. Result + async function
Result<int, string> userId = 42;
var user = await userId.MapAsync(async id => await FetchUserAsync(id));

// 3. Task<Result> + async function (most common!)
Task<Result<int, string>> asyncResult = GetUserIdAsync();
var user = await asyncResult.MapAsync(async id => await FetchUserAsync(id));
```

**Real-world example - API call chain:**
```csharp
using BindSharp;
using BindSharp.Extensions;

public async Task<Result<OrderSummary, string>> GetOrderSummaryAsync(int orderId)
{
    return await FetchOrderAsync(orderId)
        .MapAsync(async order => await EnrichWithCustomerDataAsync(order))
        .MapAsync(async order => await CalculateTotalsAsync(order))
        .MapAsync(order => new OrderSummary(order));
    
    // Clean, readable, and each step only runs if previous succeeded!
}
```

### BindAsync - Chain Async Operations
```csharp
using BindSharp;
using BindSharp.Extensions;

public async Task<Result<Receipt, string>> ProcessPaymentAsync(PaymentRequest request)
{
    return await ValidatePaymentRequest(request)  // Result<PaymentRequest, string>
        .BindAsync(async req => await ChargeCardAsync(req))  // Task<Result<Transaction, string>>
        .BindAsync(async tx => await SaveTransactionAsync(tx))  // Task<Result<Transaction, string>>
        .MapAsync(async tx => await GenerateReceiptAsync(tx));  // Task<Result<Receipt, string>>
}

// Beautiful async composition! No nested try-catch, no ugly error handling.
```

**Real-world example - Multi-step async workflow:**
```csharp
public async Task<Result<ShipmentConfirmation, string>> FulfillOrderAsync(int orderId)
{
    return await GetOrderAsync(orderId)
        .BindAsync(async order => await ValidateInventoryAsync(order))
        .BindAsync(async order => await ReserveItemsAsync(order))
        .BindAsync(async order => await CreateShipmentAsync(order))
        .BindAsync(async shipment => await NotifyCustomerAsync(shipment))
        .MapAsync(shipment => new ShipmentConfirmation(shipment));
    
    // Each async operation runs in sequence
    // First failure stops the chain immediately
    // Error is automatically propagated
}
```

### MatchAsync - Async Result Handling

Handle async results with async handlers:
```csharp
var result = await FetchDataAsync();

var output = await result.MatchAsync(
    async data => await ProcessSuccessAsync(data),
    async error => await LogErrorAsync(error)
);
```

**Real-world example - Complete async flow:**
```csharp
using BindSharp;
using BindSharp.Extensions;

public async Task<IActionResult> CreateUserAsync(CreateUserRequest request)
{
    var result = await ValidateUserRequest(request)
        .BindAsync(async req => await CheckEmailAvailabilityAsync(req))
        .BindAsync(async req => await CreateUserAccountAsync(req))
        .BindAsync(async user => await SendVerificationEmailAsync(user));
    
    return await result.MatchAsync(
        async user => {
            await _auditLog.LogUserCreatedAsync(user);
            return Created($"/users/{user.Id}", user);
        },
        async error => {
            await _auditLog.LogErrorAsync(error);
            return BadRequest(new { error });
        }
    );
}
```

## Result Utilities - Utilities for the Real World

BindSharp provides **Result** static class with practical utilities that handle common real-world scenarios beyond pure functional operations.

### Try / TryAsync - Exception Handling

Convert exception-based code into Results:
```csharp
using BindSharp;

// Synchronous - with custom error
var result = Result.Try(
    () => int.Parse(userInput),
    ex => $"Invalid number: {ex.Message}"
);

// Asynchronous - with custom error
var data = await Result.TryAsync(
    async () => await httpClient.GetStringAsync(url),
    ex => $"HTTP request failed: {ex.Message}"
);
```

**Real-world example - API integration:**
```csharp
using BindSharp;
using BindSharp.Extensions;

public async Task<Result<WeatherData, string>> GetWeatherAsync(string city)
{
    return await Result.TryAsync(
            async () => await _weatherApi.GetWeatherAsync(city),
            ex => $"Failed to fetch weather for {city}: {ex.Message}"
        )
        .BindAsync(json => Result.Try(
            () => JsonSerializer.Deserialize<WeatherData>(json),
            ex => $"Invalid weather data format: {ex.Message}"
        ))
        .EnsureNotNullAsync("Weather data was null")
        .TapAsync(async weather => await _cache.SetAsync(city, weather));
}
```

**Use custom error types:**
```csharp
public record ApiError(string Code, string Message, Exception? InnerException);

var result = Result.Try(
    () => ProcessData(input),
    ex => new ApiError("PROCESS_FAILED", "Data processing failed", ex)
);
// Result<Data, ApiError>
```

### Exception-First Try - Clean Exception Handling

**New in 1.6.0!** Returns `Result<T, Exception>` for clean exception inspection before transformation:

```csharp
using BindSharp;
using BindSharp.Extensions;

// Exception-first - inspect then transform
var result = Result.Try(() => int.Parse("invalid"))
    .TapError(ex => _logger.LogError(ex, "Parse failed"))  // Log with full context
    .MapError(ex => "Invalid number");  // Then transform to custom error
```

**The Pattern: TapError → MapError**
```csharp
using BindSharp;
using BindSharp.Extensions;

// Clean separation: logging vs transformation
var result = Result.Try(() => File.ReadAllText("file.txt"))
    .TapError(ex => _logger.LogError(ex, "Read failed"))  // ✅ Logging (side effect)
    .MapError(ex => "Failed to read file");  // ✅ Transformation (error conversion)

// Compare to mixing concerns (avoid this):
var result = Result.Try(
    () => File.ReadAllText("file.txt"),
    ex => {
        _logger.LogError(ex, "Read failed");  // ❌ Mixed with transformation
        return "Failed to read file";
    }
);
```

**Real-world example - Pattern matching on exception types:**
```csharp
using BindSharp;
using BindSharp.Extensions;

public async Task<Result<Data, string>> FetchDataAsync(string url)
{
    return await Result.TryAsync(async () => 
            await _httpClient.GetStringAsync(url))
        .TapErrorAsync(ex => {
            // Pattern match and log with full exception context
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

**Example - File operations with specific exception handling:**
```csharp
public async Task<Result<string, string>> ReadConfigFileAsync(string path)
{
    return await Result.TryAsync(async () => 
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

**When to use Exception-First Try:**
- ✅ You need to log exceptions with full context (stack traces, types)
- ✅ Different exception types require different handling
- ✅ You want to separate logging from error transformation
- ✅ Metrics or alerting need to inspect the raw exception

**When to use Original Try:**
- ✅ You don't need exception details
- ✅ Simple transformation to custom error is sufficient

### Ensure / EnsureAsync - Validation

Add validation checks without breaking your pipeline:
```csharp
using BindSharp;
using BindSharp.Extensions;

var result = GetUserAge()
    .Ensure(age => age >= 18, "Must be 18 or older")
    .Ensure(age => age <= 120, "Invalid age")
    .Map(age => new User(age));
```

**Real-world example - Business rule validation:**
```csharp
using BindSharp;
using BindSharp.Extensions;

public Result<Order, string> ValidateOrder(OrderRequest request)
{
    return request.ToResult("Order request is required")
        .Ensure(r => r.Items.Any(), "Order must contain at least one item")
        .Ensure(r => r.Items.All(i => i.Quantity > 0), "All quantities must be positive")
        .Ensure(r => r.Total > 0, "Order total must be greater than zero")
        .Ensure(r => !string.IsNullOrEmpty(r.CustomerId), "Customer ID is required")
        .Map(r => new Order(r));
}
```

**Async validation:**
```csharp
public async Task<Result<Account, string>> CreateAccountAsync(string email)
{
    return await ValidateEmail(email)
        .EnsureAsync(e => !await _db.EmailExistsAsync(e), "Email already registered")
        .BindAsync(async e => await CreateAccountRecordAsync(e));
}
```

### EnsureNotNull - Null Safety

Convert nullable checks into Results:
```csharp
using BindSharp;
using BindSharp.Extensions;

Result<User?, string> maybeUser = FindUser(id);
Result<User, string> user = maybeUser.EnsureNotNull("User not found");

// Or in a pipeline
var result = await GetUserFromCacheAsync(id)
    .EnsureNotNullAsync("User not in cache")
    .TapAsync(async u => await LogCacheHitAsync(u))
    .MapAsync(u => u.ToDto());
```

### ToResult - Nullable Conversion

Convert nullable values to Results:
```csharp
using BindSharp;
using BindSharp.Extensions;

string? cached = _cache.Get("key");
var result = cached.ToResult("Value not found in cache");

// In a pipeline
var processed = _cache.Get("user:42")
    .ToResult("User not cached")
    .Bind(json => DeserializeUser(json))
    .Map(user => ProcessUser(user));
```

**Real-world example:**
```csharp
using BindSharp;
using BindSharp.Extensions;

public Result<Product, string> GetProductFromSession(HttpContext context)
{
    return context.Session.GetString("current_product")
        .ToResult("No product in session")
        .Bind(json => Result.Try(
            () => JsonSerializer.Deserialize<Product>(json),
            ex => "Invalid product data in session"
        ))
        .EnsureNotNull("Product was null")
        .Ensure(p => !p.IsDeleted, "Product has been deleted");
}
```

### Tap / TapAsync - Side Effects

Execute side effects (logging, metrics, notifications) without modifying the Result:
```csharp
using BindSharp;
using BindSharp.Extensions;

var result = await ProcessOrderAsync(order)
    .TapAsync(async o => await _logger.LogInfoAsync($"Order {o.Id} processed"))
    .TapAsync(async o => await _metrics.IncrementAsync("orders.processed"))
    .TapAsync(async o => await _notifications.NotifyAsync(o.CustomerId))
    .MapAsync(o => o.ToDto());

// The Result flows through unchanged, but side effects are executed on success
```

**New in 1.6.0 - Sync actions in async pipelines:**
```csharp
// Before: Awkward Task.FromResult wrapping
await GetDataAsync()
    .TapAsync(x => Task.FromResult(Console.WriteLine(x)));  // ❌ Ugly!

// After: Natural sync actions
await GetDataAsync()
    .TapAsync(x => Console.WriteLine(x));  // ✨ Clean!
```

**Real-world example - Audit logging:**
```csharp
using BindSharp;
using BindSharp.Extensions;

public async Task<Result<User, string>> UpdateUserAsync(int id, UpdateUserRequest request)
{
    return await GetUserAsync(id)
        .Tap(user => _auditLog.LogAccess(user.Id, "Update attempted"))  // ✨ Sync!
        .BindAsync(user => ValidateUpdateAsync(user, request))
        .BindAsync(async user => await ApplyChangesAsync(user, request))
        .TapAsync(user => _auditLog.LogSuccess(user.Id, "Updated"))  // ✨ Sync!
        .TapAsync(async user => await _cache.InvalidateAsync($"user:{user.Id}"))
        .BindAsync(async user => await SaveChangesAsync(user));
}
```

### TapError / TapErrorAsync - Error-Specific Side Effects

**New in 1.5.0!** Execute side effects specifically on errors without modifying the result:
```csharp
using BindSharp;
using BindSharp.Extensions;

var result = await ProcessDataAsync()
    .TapAsync(data => _logger.LogInfoAsync($"Processing {data.Id}"))
    .TapErrorAsync(error => _logger.LogErrorAsync(error));  // Only on failure!
```

**New in 1.6.0 - Sync actions in async pipelines:**
```csharp
// Before: Awkward Task.FromResult wrapping
await GetDataAsync()
    .TapErrorAsync(ex => Task.FromResult(_logger.LogError(ex, "Failed")));  // ❌ Ugly!

// After: Natural sync actions
await GetDataAsync()
    .TapErrorAsync(ex => _logger.LogError(ex, "Failed"));  // ✨ Clean!
```

**How it works:**
- Result is **failure** → Action executes with error value
- Result is **success** → Action is skipped
- Always returns the original result unchanged

**Real-world example - Complete observability:**
```csharp
using BindSharp;
using BindSharp.Extensions;

public async Task<Result<Order, string>> ProcessOrderAsync(Order order)
{
    return await ValidateOrder(order)
        .TapAsync(o => _logger.LogInfo("Validation passed"))  // ✨ Sync!
        .BindAsync(async o => await SaveOrderAsync(o))
        .TapAsync(o => {  // ✨ Sync!
            _logger.LogInfo($"Order {o.Id} saved");
            _metrics.Increment("orders.success");
        })
        .TapErrorAsync(error => {  // ✨ Sync!
            _logger.LogError($"Order failed: {error}");
            _metrics.Increment("orders.failed");
            _alerting.NotifyAdmin(error);
        });
}
```

**Key Difference from MapError:**
- `MapError` - Transforms the error value (returns different error)
- `TapError` - Executes side effects only (returns same error)

**Symmetric Design:**
```csharp
// Tap and TapError are symmetric - one for success, one for failure
result
    .Tap(value => Console.WriteLine($"Success: {value}"))      // Only on success
    .TapError(error => Console.WriteLine($"Error: {error}"));  // Only on failure
```

### Do / DoAsync - Dual Side Effects

**New in 2.0!** 🔥 Execute different side effects for success and failure in a single method call.

Often you need to perform different actions based on whether an operation succeeds or fails - logging, metrics, notifications, etc. Previously, this required two separate method calls:

```csharp
// Old way (v1.x) - Still works but verbose
var result = await ProcessDataAsync()
    .TapAsync(data => _logger.LogInfo($"Processing succeeded: {data}"))
    .TapErrorAsync(error => _logger.LogError($"Processing failed: {error}"));
```

**New way (v2.0) - Cleaner and more intentional:**
```csharp
using BindSharp;
using BindSharp.Extensions;

var result = await ProcessDataAsync()
    .DoAsync(
        data => _logger.LogInfo($"Processing succeeded: {data}"),
        error => _logger.LogError($"Processing failed: {error}")
    );
```

**Benefits:**
- ✅ Single method call
- ✅ Success and failure handling grouped together
- ✅ Clear intent: "Do this on success, do that on failure"
- ✅ Harder to forget one path
- ✅ Cleaner, more maintainable code

**How it works:**

`Do/DoAsync` executes one of two actions based on the Result's state, then **returns the original Result unchanged** (just like `Tap`).

```csharp
var result = Result<int, string>.Success(42);

// Both actions provided, only success executes
var sameResult = result.Do(
    onSuccess: value => Console.WriteLine($"Success: {value}"),  // ✅ Executes
    onFailure: error => Console.WriteLine($"Error: {error}")     // ❌ Skipped
);

// sameResult == result (unchanged Success(42))
```

**Key Difference from Match:**

| Method | Purpose | Returns |
|--------|---------|---------|
| **Match** | **Transform** the result into a new value | Different type (e.g., `string`, `IActionResult`) |
| **Do** | **Execute side effects** only | Same `Result<T, TError>` |

```csharp
// Match - Transform result into a message
string message = result.Match(
    value => $"Got value: {value}",   // Returns string
    error => $"Got error: {error}"    // Returns string
);
// Type: string ✅

// Do - Execute side effects, keep result
Result<int, string> sameResult = result.Do(
    value => Console.WriteLine(value),  // Side effect only
    error => Console.WriteLine(error)   // Side effect only
);
// Type: Result<int, string> ✅
```

**Async Combinations:**

`DoAsync` supports **all combinations** of sync/async handlers:

```csharp
using BindSharp;
using BindSharp.Extensions;

// 1. Both sync
result.Do(
    value => Log(value),
    error => Log(error)
);

// 2. Async success, sync failure
await result.DoAsync(
    async value => await LogToDbAsync(value),
    error => Console.WriteLine(error)
);

// 3. Sync success, async failure
await result.DoAsync(
    value => Console.WriteLine(value),
    async error => await AlertAdminAsync(error)
);

// 4. Both async
await result.DoAsync(
    async value => await LogSuccessAsync(value),
    async error => await LogErrorAsync(error)
);

// Works with Task<Result> too!
await GetDataAsync()
    .DoAsync(
        data => Console.WriteLine(data),
        error => Console.WriteLine(error)
    );
```

**Real-World Examples:**

```csharp
using BindSharp;
using BindSharp.Extensions;

// Example 1: Complete Observability
public async Task<Result<Order, string>> ProcessOrderAsync(Order order)
{
    return await ValidateOrder(order)
        .DoAsync(
            o => _logger.LogInfo($"Validation passed for order {o.Id}"),
            error => _logger.LogWarning($"Validation failed: {error}")
        )
        .BindAsync(async o => await SaveOrderAsync(o))
        .DoAsync(
            o => {
                _logger.LogInfo($"Order {o.Id} saved");
                _metrics.Increment("orders.success");
            },
            error => {
                _logger.LogError($"Save failed: {error}");
                _metrics.Increment("orders.failed");
                _alerting.NotifyAdmin(error);
            }
        );
}

// Example 2: API Response with Logging
public async Task<IActionResult> GetUserAsync(int id)
{
    var result = await _userService.FetchUserAsync(id)
        .DoAsync(
            async user => await _auditLog.LogAccessAsync(user.Id, "Viewed"),
            async error => await _auditLog.LogAccessDeniedAsync(id, error)
        );

    return result.Match(
        user => Ok(user),
        error => NotFound(new { error })
    );
}

// Example 3: Metrics and Alerting
public async Task<Result<Data, string>> ImportDataAsync(string source)
{
    var stopwatch = Stopwatch.StartNew();

    return await FetchDataAsync(source)
        .DoAsync(
            async data => {
                stopwatch.Stop();
                await _metrics.RecordLatencyAsync("import.success", stopwatch.Elapsed);
                await _metrics.IncrementAsync("import.count");
            },
            async error => {
                stopwatch.Stop();
                await _metrics.RecordLatencyAsync("import.failure", stopwatch.Elapsed);
                await _alerting.NotifyAsync($"Import failed from {source}: {error}");
            }
        )
        .BindAsync(async data => await TransformDataAsync(data));
}
```

**When to Use Do vs. Tap + TapError:**

Both patterns are valid! Choose based on your preference:

**Use `Do` when:**
- ✅ Success and failure are related (same logging concern)
- ✅ You want handlers visually grouped
- ✅ Cleaner, more concise code matters

**Use `Tap + TapError` when:**
- ✅ Success and failure are unrelated concerns
- ✅ Handlers are complex and better separated
- ✅ You might add one handler later

```csharp
// Related logging - Do is clearer
.DoAsync(
    data => _logger.LogInfo($"Success: {data}"),
    error => _logger.LogError($"Failed: {error}")
)

// Unrelated concerns - Tap + TapError is fine
.TapAsync(data => _cache.SetAsync("key", data))  // Caching (success only)
.TapErrorAsync(error => _retry.ScheduleAsync())  // Retry (failure only)
```

### Using / UsingAsync - Resource Management

Safe resource management with guaranteed disposal (the "bracket" pattern):
```csharp
using BindSharp;
using BindSharp.Extensions;

var data = OpenFile("data.txt")
    .Using(stream => ReadAllData(stream));
// stream is automatically disposed, even if ReadAllData fails

// Async version
var data = await OpenDatabaseConnectionAsync()
    .UsingAsync(async connection =>
        await QueryDataAsync(connection)
            .BindAsync(async data => await ValidateDataAsync(data))
            .MapAsync(data => TransformData(data))
    );
// connection is automatically disposed
```

**Real-world example - Database transaction:**
```csharp
using BindSharp;
using BindSharp.Extensions;

public async Task<Result<Order, string>> CreateOrderWithTransactionAsync(CreateOrderRequest request)
{
    return await Result.TryAsync(
            async () => await _dbContext.Database.BeginTransactionAsync(),
            ex => $"Failed to begin transaction: {ex.Message}"
        )
        .UsingAsync(async transaction =>
            await ValidateOrderRequest(request)
                .BindAsync(async req => await CreateOrderEntityAsync(req))
                .TapAsync(async order => await UpdateInventoryAsync(order))
                .TapAsync(async order => await CreateOrderHistoryAsync(order))
                .BindAsync(async order => {
                    await transaction.CommitAsync();
                    return order;
                })
                .MapErrorAsync(async error => {
                    await transaction.RollbackAsync();
                    return error;
                })
        );
    // transaction is automatically disposed
}
```

### AsTask - Sync to Async Conversion

Convert synchronous Results to Task-wrapped Results:
```csharp
using BindSharp;
using BindSharp.Extensions;

Result<int, string> syncResult = Validate(value);
Task<Result<int, string>> asyncResult = syncResult.AsTask();

// Useful for matching method signatures
public Task<Result<User, string>> GetUserAsync(int id)
{
    // Fast path: check cache
    var cached = _cache.Get<User>(id);
    if (cached != null)
        return cached.AsTask();  // ✨ Implicit conversion + AsTask!
    
    // Slow path: fetch from database
    return FetchUserFromDatabaseAsync(id);
}
```

## Complete Real-World Example

Here's how everything comes together with all the features:
```csharp
using BindSharp;
using BindSharp.Extensions;

public class OrderService
{
    public async Task<Result<OrderConfirmation, OrderError>> PlaceOrderAsync(Cart cart)
    {
        return await cart.ToResult(OrderError.InvalidCart("Cart is null"))
            // Validation
            .Ensure(c => c.Items.Any(), OrderError.EmptyCart())
            .Ensure(c => c.CustomerId != null, OrderError.MissingCustomer())
            
            // Exception handling with logging (NEW in 1.6.0!)
            .BindAsync(async c => await Result.TryAsync(
                async () => await _inventory.CheckStockAsync(c.Items))
                .TapErrorAsync(ex => {  // ✨ Log exception with full context
                    _logger.LogError(ex, "Inventory check failed");
                    _metrics.RecordException(ex);
                })
                .MapErrorAsync(ex => OrderError.InventoryError(ex.Message))
            )
            
            // Business logic with dual side effects (NEW in 2.0!)
            .DoAsync(
                c => _logger.LogInfo($"Processing cart {c.Id}"),
                error => _logger.LogError($"Validation failed: {error}")
            )
            .BindAsync(async c => await CalculatePriceAsync(c))
            .TapAsync(c => _metrics.Increment("orders.pricing.completed"))
            
            // Conditional processing
            .BindIfAsync(
                async order => await RequiresSpecialHandlingAsync(order),
                async order => await ApplySpecialHandlingAsync(order)
            )
            
            // Payment with resource management
            .BindAsync(async order => await ProcessPaymentWithTransactionAsync(order))
            .TapAsync(order => _logger.LogInfo($"Payment processed for order {order.Id}"))
            
            // Finalization
            .BindAsync(async order => await CreateOrderRecordAsync(order))
            .BindAsync(async order => await ReserveInventoryAsync(order))
            
            // Notifications
            .TapAsync(async order => await _emailService.SendConfirmationAsync(order))
            .TapAsync(async order => await _sms.SendNotificationAsync(order.CustomerId))
            
            // Transform and dual side effects (NEW in 2.0!)
            .MapAsync(order => new OrderConfirmation(order))
            .DoAsync(
                conf => _auditLog.LogOrderCreated(conf),
                error => {
                    _logger.LogError($"Order failed: {error}");
                    _metrics.Increment("orders.failed");
                    _alerting.NotifyAdmin($"Order processing failure: {error}");
                }
            );
    }
    
    private async Task<Result<Order, OrderError>> ProcessPaymentWithTransactionAsync(Order order)
    {
        return await Result.TryAsync(
                async () => await _paymentGateway.BeginTransactionAsync())
            .TapErrorAsync(ex => _logger.LogError(ex, "Payment transaction failed"))
            .MapErrorAsync(ex => OrderError.PaymentError($"Transaction failed: {ex.Message}"))
            .UsingAsync(async transaction =>
                await ChargeCustomerAsync(order)
                    .TapAsync(async charge => await transaction.CommitAsync())
                    .MapErrorAsync(async error => {
                        await transaction.RollbackAsync();
                        return error;
                    })
            );
    }
}
```

## Tips & Best Practices

### General
1. **Use descriptive error types** - `Result<User, UserError>` is better than `Result<User, string>`
2. **Keep operations small** - Each function should do one thing
3. **Use Bind for chaining** - When next operation depends on previous result
4. **Use Map for transforming** - When just changing the value
5. **Match at boundaries** - Convert Result to concrete types at API/UI boundaries
6. **Async all the way** - If any operation is async, make the whole chain async

### Implicit Conversions (NEW in 1.3.0)
7. **Use different types for T and TError** - NEVER use `Result<string, string>`
8. **Define custom error types** - Makes code clearer and type-safer
9. **Mix styles freely** - Implicit and explicit can coexist
10. **Use for guard clauses** - Perfect for early returns

### Result Utilities (v2.0)
11. **Use Try for exception-based APIs** - Convert legacy code to Results
12. **Use Ensure for business rules** - Keep validation in the pipeline
13. **Use Tap for side effects** - Logging, metrics, notifications
14. **Use Using for resources** - Database connections, file streams, transactions
15. **Use ToResult for nullables** - Convert optional values to Results

### BindIf (NEW in 1.4.1)
16. **Use BindIf for conditional execution** - "If condition, then execute"
17. **Keep predicates side-effect free** - Predicates should be pure functions
18. **Use async predicates for I/O** - Database checks, cache lookups, etc.
19. **Remember: TRUE executes, FALSE skips** - Standard if-then behavior

### TapError (NEW in 1.5.0)
20. **Use TapError for error logging** - Log errors without transforming them
21. **Use Tap + TapError for observability** - Complete success/failure tracking
22. **Keep error actions simple** - Just logging, metrics, alerts
23. **Don't transform in TapError** - Use MapError if you need to change the error

### Do/DoAsync (NEW in 2.0)
24. **Use Do for related concerns** - When success and failure handling are coupled
25. **Keep handlers simple** - Do is for side effects, not complex logic
26. **Use for cross-cutting concerns** - Logging, metrics, caching, notifications
27. **Combine with other methods naturally** - Do fits seamlessly in pipelines

### Exception-First Try (NEW in 1.6.0)
28. **Use exception-first Try for logging** - Log exceptions before transforming
29. **Pattern match on exception types** - Handle different exceptions differently
30. **Separate logging from transformation** - TapError then MapError
31. **Use for metrics and alerting** - Inspect raw exceptions for monitoring

### Mixed Async/Sync Pipelines (NEW in 1.6.0)
32. **Use sync actions when possible** - Simpler than wrapping in Task.FromResult
33. **Keep side effects lightweight** - Heavy operations should be async
34. **Natural composition** - Let the compiler choose the right overload

### Error Handling Strategy
```csharp
using BindSharp;

// ✅ Good: Specific error types
public record OrderError(string Code, string Message);

// ✅ Good: Enables implicit conversions without ambiguity
public Result<Order, OrderError> CreateOrder(OrderRequest request)
{
    if (request.Items.Count == 0)
        return new OrderError("EMPTY_CART", "Cart is empty");
    
    return new Order(request);
}

// ❌ Avoid: Same type for T and TError (ambiguous with implicit conversions!)
public Result<string, string> GetValue() { ... }  // DON'T DO THIS!
```

### Exception Handling Patterns (NEW in 1.6.0)
```csharp
using BindSharp;
using BindSharp.Extensions;

// ✅ Good: Separate logging from transformation
Result.Try(() => operation())
    .TapError(ex => _logger.LogError(ex, "Failed"))  // ✅ Logging
    .MapError(ex => "User-friendly message");  // ✅ Transformation

// ❌ Avoid: Mixing concerns
Result.Try(
    () => operation(),
    ex => {
        _logger.LogError(ex, "Failed");  // ❌ Side effect mixed with transformation
        return "User-friendly message";
    }
);
```

## Why Async Support is a Game-Changer

Traditional async error handling gets messy fast:
```csharp
// 😢 The old way
try {
    var user = await GetUserAsync(id);
    try {
        var orders = await GetOrdersAsync(user.Id);
        try {
            var enriched = await EnrichOrdersAsync(orders);
            return await FormatResponseAsync(enriched);
        } catch (Exception ex3) { /* handle */ }
    } catch (Exception ex2) { /* handle */ }
} catch (Exception ex1) { /* handle */ }
```

With BindSharp:
```csharp
using BindSharp;
using BindSharp.Extensions;

// 😊 The new way
return await GetUserAsync(id)
    .BindAsync(user => GetOrdersAsync(user.Id))
    .BindAsync(orders => EnrichOrdersAsync(orders))
    .MapAsync(enriched => FormatResponseAsync(enriched))
    .MatchAsync(
        success => Ok(success),
        error => BadRequest(error)
    );
```

**Clean. Composable. Elegant. Powerful.** 🚀

## API Reference

### Core Types
- `Result<T, TError>` - Result type with Success/Failure states
- `Unit` - Represents "no value" (use `Unit.Value`)

### FunctionalResult (Core Operations)
- `Map<T1, T2, TError>` - Transform success value
- `Bind<T1, T2, TError>` - Chain operations that can fail
- `BindIf<T, TError>` - Conditional processing (new in 1.4.1!)
- `MapError<T, TError, TNewError>` - Transform error value
- `Match<T, TError, TResult>` - Handle both success and failure

### AsyncFunctionalResult (Async Core)
- `MapAsync<T1, T2, TError>` - Async transformations (3 overloads)
- `BindAsync<T1, T2, TError>` - Async chaining (3 overloads)
- `BindIfAsync<T, TError>` - Async conditional processing (7 overloads - new in 1.4.1!)
- `MapErrorAsync<T, TError, TNewError>` - Async error transformation (3 overloads)
- `MatchAsync<T, TError, TResult>` - Async result handling (7 overloads)

### Result (Static Utilities)
- `Try<T, TError>` - Exception handling with custom error factory
- `Try<T>` - Exception-first (returns Result<T, Exception>) (new in 1.6.0!)
- `TryAsync<T, TError>` - Async exception handling with custom error factory
- `TryAsync<T>` - Async exception-first (new in 1.6.0!)

### BindSharp.Extensions
Require `using BindSharp.Extensions;`

**Validation:**
- `Ensure<T, TError>` / `EnsureAsync<T, TError>` - Validation
- `EnsureNotNull<T, TError>` / `EnsureNotNullAsync<T, TError>` - Null safety

**Conversion:**
- `ToResult<T, TError>` - Convert nullable to Result
- `AsTask<T, TError>` - Convert Result to Task

**Side Effects:**
- `Tap<T, TError>` / `TapAsync<T, TError>` - Success side effects (4 overloads, +1 in 1.6.0)
- `TapError<T, TError>` / `TapErrorAsync<T, TError>` - Error side effects (4 overloads, +1 in 1.6.0)
- `Do<T, TError>` / `DoAsync<T, TError>` - Dual side effects (8 overloads - new in 2.0!)

**Resource Management:**
- `Using<TResource, TResult, TError>` / `UsingAsync` - Resource management

## Acknowledgments

Special thanks to **[Zoran Horvat](https://www.youtube.com/@zoran-horvat)** from the YouTube channel **"Zoran on C#"** for his excellent tutorials on functional programming in C#. His teaching on Railway-Oriented Programming and Result patterns made this library possible.

If you want to learn advanced C# techniques and functional programming concepts, check out his channel - he deserves way more views! We can all learn a thing or two from him. 🙏

## License

LGPL-3.0-or-later

## Contributing

Contributions are welcome! Feel free to open issues or submit pull requests.

---

Built with ❤️ for the .NET community