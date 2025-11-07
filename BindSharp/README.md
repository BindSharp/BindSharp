# BindSharp

A lightweight, powerful functional programming library for .NET that makes error handling elegant and composable. Say goodbye to messy try-catch blocks and hello to Railway-Oriented Programming! 🚂

## Why BindSharp?

Traditional error handling is messy:
```csharp
try {
    var data = await FetchDataAsync();
    var validated = ValidateData(data);
    var transformed = TransformData(validated);
    return await SaveAsync(transformed);
}
catch (Exception ex) {
    // What failed? Where? How do we recover?
    return null; // 😢
}
```

With BindSharp, it's clean and composable:
```csharp
return await FetchDataAsync()
    .BindAsync(ValidateDataAsync)
    .MapAsync(TransformData)
    .BindAsync(SaveAsync)
    .MatchAsync(
        success => $"Saved: {success}",
        error => $"Failed: {error}"
    );
```

## ✨ What's New in 1.1.0

**ResultExtensions** adds powerful utilities for real-world scenarios:

- 🛡️ **Exception Handling** - Convert try/catch into Results with `Try` and `TryAsync`
- ✅ **Validation** - Add business rule checks with `Ensure` and `EnsureNotNull`
- 📝 **Side Effects** - Execute logging/metrics with `Tap` without breaking pipelines
- 🔒 **Resource Management** - Safe disposal with `Using` (the bracket pattern)
- 🔄 **Nullable Conversion** - Convert `null` checks into Results with `ToResult`

See the [ResultExtensions](#resultextensions---utilities-for-the-real-world) section below for examples!

## Features

✅ **Result<T, TError>** - Explicit success/failure handling  
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

// Create a successful result
var success = Result<int, string>.Success(42);

// Create a failed result
var failure = Result<int, string>.Failure("Something went wrong");

// Check the result
if (success.IsSuccess)
    Console.WriteLine(success.Value); // 42

if (failure.IsFailure)
    Console.WriteLine(failure.Error); // "Something went wrong"
```

## Core Operations

### Map - Transform Success Values

Transform a value when the result is successful:
```csharp
Result<int, string> GetAge() => Result<int, string>.Success(25);

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
Result<string, string> ValidateEmail(string email) =>
    email.Contains("@")
        ? Result<string, string>.Success(email)
        : Result<string, string>.Failure("Invalid email");

Result<string, string> SendEmail(string email) =>
    /* send email logic */
    Result<string, string>.Success($"Sent to {email}");

var result = ValidateEmail("user@example.com")
    .Bind(SendEmail);  // Only runs if validation succeeds
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
Result<int, string> result = Result<int, string>.Failure("404");

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

## 🚀 Async Support - The Game Changer!

This is where BindSharp really shines! Handle async operations with the same elegant composition.

### MapAsync - Async Transformations

Three overloads for every scenario:
```csharp
// 1. Task<Result> + sync function
Task<Result<int, string>> asyncResult = GetUserIdAsync();
var user = await asyncResult.MapAsync(id => GetUserFromCache(id));

// 2. Result + async function
Result<int, string> userId = Result<int, string>.Success(42);
var user = await userId.MapAsync(async id => await FetchUserAsync(id));

// 3. Task<Result> + async function (most common!)
Task<Result<int, string>> asyncResult = GetUserIdAsync();
var user = await asyncResult.MapAsync(async id => await FetchUserAsync(id));
```

**Real-world example - API call chain:**
```csharp
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

## ResultExtensions - Utilities for the Real World

BindSharp 1.1.0 adds **ResultExtensions** - practical utilities that handle common real-world scenarios beyond pure functional operations.

### Try / TryAsync - Exception Handling

Convert exception-based code into Results:

```csharp
// Synchronous
var result = ResultExtensions.Try(
    () => int.Parse(userInput),
    ex => $"Invalid number: {ex.Message}"
);

// Asynchronous
var data = await ResultExtensions.TryAsync(
    async () => await httpClient.GetStringAsync(url),
    ex => $"HTTP request failed: {ex.Message}"
);
```

**Real-world example - API integration:**
```csharp
public async Task<Result<WeatherData, string>> GetWeatherAsync(string city)
{
    return await ResultExtensions.TryAsync(
            async () => await _weatherApi.GetWeatherAsync(city),
            ex => $"Failed to fetch weather for {city}: {ex.Message}"
        )
        .BindAsync(json => ResultExtensions.Try(
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

var result = ResultExtensions.Try(
    () => ProcessData(input),
    ex => new ApiError("PROCESS_FAILED", "Data processing failed", ex)
);
// Result<Data, ApiError>
```

### Ensure / EnsureAsync - Validation

Add validation checks without breaking your pipeline:

```csharp
var result = GetUserAge()
    .Ensure(age => age >= 18, "Must be 18 or older")
    .Ensure(age => age <= 120, "Invalid age")
    .Map(age => new User(age));
```

**Real-world example - Business rule validation:**
```csharp
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
public Result<Product, string> GetProductFromSession(HttpContext context)
{
    return context.Session.GetString("current_product")
        .ToResult("No product in session")
        .Bind(json => ResultExtensions.Try(
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
var result = await ProcessOrderAsync(order)
    .TapAsync(async o => await _logger.LogInfoAsync($"Order {o.Id} processed"))
    .TapAsync(async o => await _metrics.IncrementAsync("orders.processed"))
    .TapAsync(async o => await _notifications.NotifyAsync(o.CustomerId))
    .MapAsync(o => o.ToDto());

// The Result flows through unchanged, but side effects are executed on success
```

**Real-world example - Audit logging:**
```csharp
public async Task<Result<User, string>> UpdateUserAsync(int id, UpdateUserRequest request)
{
    return await GetUserAsync(id)
        .Tap(user => _auditLog.LogAccess(user.Id, "Update attempted"))
        .BindAsync(user => ValidateUpdateAsync(user, request))
        .BindAsync(async user => await ApplyChangesAsync(user, request))
        .TapAsync(async user => await _auditLog.LogSuccessAsync(user.Id, "Updated"))
        .TapAsync(async user => await _cache.InvalidateAsync($"user:{user.Id}"))
        .BindAsync(async user => await SaveChangesAsync(user));
}
```

### Using / UsingAsync - Resource Management

Safe resource management with guaranteed disposal (the "bracket" pattern):

```csharp
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
public async Task<Result<Order, string>> CreateOrderWithTransactionAsync(CreateOrderRequest request)
{
    return await ResultExtensions.TryAsync(
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
                    return Result<Order, string>.Success(order);
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
Result<int, string> syncResult = Validate(value);
Task<Result<int, string>> asyncResult = syncResult.AsTask();

// Useful for matching method signatures
public Task<Result<User, string>> GetUserAsync(int id)
{
    // Fast path: check cache
    var cached = _cache.Get<User>(id);
    if (cached != null)
        return Result<User, string>.Success(cached).AsTask();
    
    // Slow path: fetch from database
    return FetchUserFromDatabaseAsync(id);
}
```

## Complete Real-World Example with ResultExtensions

Here's how everything comes together:

```csharp
public class OrderService
{
    public async Task<Result<OrderConfirmation, OrderError>> PlaceOrderAsync(Cart cart)
    {
        return await cart.ToResult(OrderError.InvalidCart("Cart is null"))
            // Validation
            .Ensure(c => c.Items.Any(), OrderError.EmptyCart())
            .Ensure(c => c.CustomerId != null, OrderError.MissingCustomer())
            
            // Exception handling
            .BindAsync(async c => await ResultExtensions.TryAsync(
                async () => await _inventory.CheckStockAsync(c.Items),
                ex => OrderError.InventoryError(ex.Message)
            ))
            
            // Business logic with logging
            .TapAsync(async c => await _logger.LogInfoAsync($"Processing cart {c.Id}"))
            .BindAsync(async c => await CalculatePriceAsync(c))
            .TapAsync(async c => await _metrics.IncrementAsync("orders.pricing.completed"))
            
            // Payment with resource management
            .BindAsync(async order => await ProcessPaymentWithTransactionAsync(order))
            .TapAsync(async order => await _logger.LogInfoAsync($"Payment processed for order {order.Id}"))
            
            // Finalization
            .BindAsync(async order => await CreateOrderRecordAsync(order))
            .BindAsync(async order => await ReserveInventoryAsync(order))
            
            // Notifications
            .TapAsync(async order => await _emailService.SendConfirmationAsync(order))
            .TapAsync(async order => await _sms.SendNotificationAsync(order.CustomerId))
            
            // Transform and audit
            .MapAsync(order => new OrderConfirmation(order))
            .TapAsync(async conf => await _auditLog.LogOrderCreatedAsync(conf));
    }
    
    private async Task<Result<Order, OrderError>> ProcessPaymentWithTransactionAsync(Order order)
    {
        return await ResultExtensions.TryAsync(
                async () => await _paymentGateway.BeginTransactionAsync(),
                ex => OrderError.PaymentError($"Transaction failed: {ex.Message}")
            )
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

## Advanced Patterns

### Error Recovery
```csharp
public async Task<Result<Data, string>> GetDataWithFallbackAsync(int id)
{
    var primaryResult = await FetchFromPrimarySourceAsync(id);
    
    if (primaryResult.IsSuccess)
        return primaryResult;
    
    // Try fallback
    return await FetchFromCacheAsync(id)
        .BindAsync(async cached => {
            if (cached.IsSuccess)
                return cached;
            return await FetchFromBackupSourceAsync(id);
        })
        .MapError(backupError => 
            $"All sources failed: Primary={primaryResult.Error}, Backup={backupError}"
        );
}
```

### Parallel Async Operations
```csharp
public async Task<Result<CombinedData, string>> FetchAllDataAsync(int userId)
{
    // Start all operations in parallel
    var userTask = GetUserAsync(userId);
    var ordersTask = GetOrdersAsync(userId);
    var preferencesTask = GetPreferencesAsync(userId);
    
    await Task.WhenAll(userTask, ordersTask, preferencesTask);
    
    // Combine results
    var user = await userTask;
    var orders = await ordersTask;
    var preferences = await preferencesTask;
    
    // If any failed, return first failure
    if (user.IsFailure) return Result<CombinedData, string>.Failure(user.Error);
    if (orders.IsFailure) return Result<CombinedData, string>.Failure(orders.Error);
    if (preferences.IsFailure) return Result<CombinedData, string>.Failure(preferences.Error);
    
    return Result<CombinedData, string>.Success(new CombinedData
    {
        User = user.Value,
        Orders = orders.Value,
        Preferences = preferences.Value
    });
}
```

### Validation Chains with Ensure
```csharp
public Result<User, ValidationError> ValidateAndCreateUser(UserRegistration input)
{
    return input.ToResult(ValidationError.Required("User registration data"))
        .Ensure(
            i => !string.IsNullOrWhiteSpace(i.Email),
            ValidationError.Required("Email")
        )
        .Ensure(
            i => i.Email.Contains("@"),
            ValidationError.Invalid("Email format")
        )
        .Ensure(
            i => !string.IsNullOrWhiteSpace(i.Password),
            ValidationError.Required("Password")
        )
        .Ensure(
            i => i.Password.Length >= 8,
            ValidationError.Invalid("Password must be at least 8 characters")
        )
        .Ensure(
            i => i.Age >= 18,
            ValidationError.Invalid("Must be 18 or older")
        )
        .Map(i => new User(i.Email, i.Password, i.Age));
}

// Each validation only runs if previous succeeded
// First failure stops and returns the error
```

### Complete Pipeline with All Features
```csharp
public async Task<Result<ProcessedOrder, OrderError>> ProcessOrderPipelineAsync(int orderId)
{
    return await ResultExtensions.TryAsync(
            async () => await _repository.GetOrderAsync(orderId),
            ex => OrderError.DatabaseError(ex.Message)
        )
        .EnsureNotNullAsync(OrderError.NotFound(orderId))
        .Ensure(o => !o.IsProcessed, OrderError.AlreadyProcessed(orderId))
        .Tap(o => _logger.LogInfo($"Starting processing for order {o.Id}"))
        .BindAsync(async o => await ValidateOrderAsync(o))
        .TapAsync(async o => await _metrics.IncrementAsync("orders.validated"))
        .BindAsync(async o => await EnrichOrderDataAsync(o))
        .TapAsync(async o => await _cache.SetAsync($"order:{o.Id}", o))
        .BindAsync(async o => await ApplyBusinessRulesAsync(o))
        .TapAsync(async o => await _auditLog.LogAsync("Order processed", o.Id))
        .MapAsync(o => new ProcessedOrder(o));
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

### ResultExtensions Specific
7. **Use Try for exception-based APIs** - Convert legacy code to Results
8. **Use Ensure for business rules** - Keep validation in the pipeline
9. **Use Tap for side effects** - Logging, metrics, notifications
10. **Use Using for resources** - Database connections, file streams, transactions
11. **Use ToResult for nullables** - Convert optional values to Results
12. **Chain Tap calls sparingly** - Too many can impact readability

### Error Handling Strategy
```csharp
// ✅ Good: Specific error types
public record OrderError(string Code, string Message);

// ✅ Good: Error factory for flexibility
ResultExtensions.Try(
    () => Parse(input),
    ex => new ParseError(ex.Message, ex.GetType().Name)
);

// ⚠️ Okay: String errors for simple cases
ResultExtensions.Try(
    () => Parse(input),
    ex => $"Parse failed: {ex.Message}"
);

// ❌ Avoid: Losing exception information
ResultExtensions.Try(
    () => Parse(input),
    ex => "Parse failed"  // Lost all exception details!
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

### FunctionalResult (Core Operations)
- `Map<T1, T2, TError>` - Transform success value
- `Bind<T1, T2, TError>` - Chain operations that can fail
- `MapError<T, TError, TNewError>` - Transform error value
- `Match<T, TError, TResult>` - Handle both success and failure

### AsyncFunctionalResult (Async Core)
- `MapAsync<T1, T2, TError>` - Async transformations (3 overloads)
- `BindAsync<T1, T2, TError>` - Async chaining (3 overloads)
- `MapErrorAsync<T, TError, TNewError>` - Async error transformation (3 overloads)
- `MatchAsync<T, TError, TResult>` - Async result handling (7 overloads)

### ResultExtensions (Utilities)
- `Try<T, TError>` / `TryAsync<T, TError>` - Exception handling
- `Ensure<T, TError>` / `EnsureAsync<T, TError>` - Validation
- `EnsureNotNull<T, TError>` / `EnsureNotNullAsync<T, TError>` - Null safety
- `ToResult<T, TError>` - Convert nullable to Result
- `Tap<T, TError>` / `TapAsync<T, TError>` - Side effects (3 overloads)
- `Using<TResource, TResult, TError>` / `UsingAsync` - Resource management
- `AsTask<T, TError>` - Convert Result to Task

## Acknowledgments

Special thanks to **[Zoran Horvat](https://www.youtube.com/@zoran-horvat)** from the YouTube channel **"Zoran on C#"** for his excellent tutorials on functional programming in C#. His teaching on Railway-Oriented Programming and Result patterns made this library possible.

If you want to learn advanced C# techniques and functional programming concepts, check out his channel - he deserves way more views! We can all learn a thing or two from him. 🙏

## License

LGPL-3.0-or-later

## Contributing

Contributions are welcome! Feel free to open issues or submit pull requests.

---

Built with ❤️ for the .NET community