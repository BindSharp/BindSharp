# BindSharp 🚂

A lightweight, powerful functional programming library for .NET that makes error handling elegant and composable. Say goodbye to messy try-catch blocks and hello to **Railway-Oriented Programming**.

[![NuGet](https://img.shields.io/nuget/v/BindSharp)](https://www.nuget.org/packages/BindSharp)
[![NuGet Downloads](https://img.shields.io/nuget/dt/BindSharp)](https://www.nuget.org/packages/BindSharp)
[![License: MPL-2.0](https://img.shields.io/badge/License-MPL_2.0-brightgreen.svg)](https://opensource.org/licenses/MPL-2.0)
[![.NET Standard 2.0](https://img.shields.io/badge/.NET%20Standard-2.0-blue)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)

```bash
dotnet add package BindSharp
```

---

## Why BindSharp?

Traditional error handling turns business logic into a pyramid of doom:

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
        throw;
    }
}
catch (HttpRequestException hex)
{
    _logger.LogError(hex, "Network error");
    throw;
}
```

With BindSharp, each step is a composable operation on a railway — success continues forward, failures short-circuit to the end:

```csharp
using BindSharp;
using BindSharp.Extensions;

return await Result.TryAsync(() => FetchDataAsync())
    .TapErrorAsync(ex => _logger.LogError(ex, "Fetch failed"))
    .MapErrorAsync(ex => "Network error")
    .BindAsync(ValidateDataAsync)
    .TapErrorAsync(err => _logger.LogError("Validation failed: {Error}", err))
    .BindIfAsync(data => data.RequiresTransformation, TransformDataAsync)
    .BindAsync(SaveAsync)
    .DoAsync(
        data  => _logger.LogInfo("Saved: {Data}", data),
        error => _logger.LogError("Save failed: {Error}", error)
    )
    .MatchAsync(
        success => $"✅ Saved: {success}",
        error   => $"❌ Failed: {error}"
    );
```

---

## What's New in 2.1.1

Version 2.1.1 fixes a bug in **`Ensure` / `EnsureAsync`**: when the incoming result had already failed, `Ensure` replaced that failure with its own validation error, silently discarding the real cause. A failed result now passes through `Ensure` untouched — the original error is preserved and the predicate is not evaluated (the same way `Map`, `Bind` and `EnsureNotNull` already behaved).

**Behavior change.** If a pipeline relied on `Ensure` overwriting an earlier failure, it will now surface the original error instead. The typical case is `.MapErrorAsync(...)` followed by `.EnsureAsync(...)`: a data-access failure used to come out as the validation error and now comes out as the mapped data-access error. Successful results are unaffected. See [RELEASE_NOTES_2.1.1](ReleaseNotes/RELEASE_NOTES_2.1.1.md).

**Previous releases:**
- **2.1.0** — License update to MPL-2.0
- **2.0.0** — `Do`/`DoAsync` dual side effects, `BindSharp.Extensions` namespace
- **1.6.0** — Exception-first `Try`, mixed async/sync pipelines
- **1.5.0** — `TapError` / `TapErrorAsync`
- **1.4.1** — `BindIf` conditional processing
- **1.3.0** — Equality support, implicit conversions
- **1.2.0** — `Unit` type
- **1.1.0** — `Ensure`, `Tap`, `Using`, `ToResult`

---

## Table of Contents

- [Result\<T, TError\>](#resultt-terror) — the core type
- [Unit](#unit) — representing no value
- [Try / TryAsync](#try--tryasync) — bridging exception-based APIs
- [Map / MapAsync](#map--mapasync) — transform success values
- [Bind / BindAsync](#bind--bindasync) — chain fallible operations
- [BindIf / BindIfAsync](#bindif--bindifasync) — conditional execution
- [MapError / MapErrorAsync](#maperror--maperrorasync) — transform error values
- [Match / MatchAsync](#match--matchasync) — extract a final value
- [Ensure / EnsureAsync](#ensure--ensureasync) — inline validation
- [EnsureNotNull / EnsureNotNullAsync](#ensurenotnull--ensurenotnullasync) — null safety
- [ToResult](#toresult) — convert nullables to Results
- [AsTask](#astask) — sync-to-async bridge
- [Tap / TapAsync](#tap--tapasync) — success side effects
- [TapError / TapErrorAsync](#taperror--taperrorasync) — failure side effects
- [Do / DoAsync](#do--doasync) — dual side effects
- [Using / UsingAsync](#using--usingasync) — resource management

---

## `Result<T, TError>`

**What** — A value that is either `Success(value)` or `Failure(error)`. It cannot be both, and you cannot access `.Value` without first checking `.IsSuccess`.

**Why** — Makes every operation that can fail explicit at the type level. The compiler forces you to handle both outcomes — no silent exceptions, no forgotten null checks.

**When** — Use it as the return type of any method that can fail. Prefer custom error types over `string` to keep errors structured and type-safe.

```csharp
using BindSharp;

// Explicit construction
var ok  = Result<int, string>.Success(42);
var err = Result<int, string>.Failure("Something went wrong");

// Implicit conversions — works when T and TError are different types
Result<int, string> ok2  = 42;
Result<int, string> err2 = "Something went wrong";

// Properties
ok.IsSuccess   // true
ok.IsFailure   // false
ok.Value       // 42              (throws InvalidOperationException if IsFailure)
err.Error      // "Something..."  (throws InvalidOperationException if IsSuccess)

// Equality — two Results are equal if state and payload are equal
ok == ok2      // true
ok == err      // false

// ToString
ok.ToString()  // "Success(42)"
err.ToString() // "Failure(Something went wrong)"
```

**Example — functions that can fail**

```csharp
public Result<int, string> Divide(int a, int b)
{
    if (b == 0) return "Division by zero";  // implicit Failure
    return a / b;                           // implicit Success
}

public Result<User, string> GetUser(int id)
{
    if (id < 0) return "Invalid ID";
    if (id == 0) return "User not found";
    return new User(id);
}
```

> ⚠️ **Never use the same type for both `T` and `TError`** — implicit conversions become ambiguous. Use distinct types, preferably custom error records.

```csharp
// ❌ Ambiguous — compiler cannot resolve the implicit conversion
public Result<string, string> GetName() { ... }

// ✅ Clear — different types, no ambiguity
public record UserError(string Code, string Message);
public Result<string, UserError> GetName() { ... }
```

---

## `Unit`

**What** — A singleton type that represents "no meaningful return value", analogous to `void` but usable as a generic type parameter.

**Why** — `void` cannot be used as `T` in `Result<T, TError>`. Without `Unit`, operations like deletes, writes, and notifications would need inconsistent return types (`bool`, `int`, or bare `Task`), breaking pipeline composition.

**When** — Use `Unit` as the success type for any operation where success matters but the return value doesn't: database writes, deletes, email sends, notifications.

```csharp
using BindSharp;

// Unit.Value is a singleton — effectively zero allocation
Result<Unit, string> voidResult = Unit.Value;
```

**Example — CRUD operations that compose cleanly**

```csharp
public Task<Result<Unit, string>> DeleteUserAsync(int id) =>
    Result.TryAsync(
        async () => {
            await _repository.DeleteAsync(id);
            return Unit.Value;
        },
        ex => $"Delete failed: {ex.Message}"
    );

public Task<Result<Unit, string>> SendWelcomeEmailAsync(string email) =>
    Result.TryAsync(
        async () => {
            await _mailer.SendAsync(email, "Welcome!");
            return Unit.Value;
        },
        ex => $"Email send failed: {ex.Message}"
    );

// Both return Result<Unit, string> — they compose naturally
public async Task<Result<Unit, string>> RegisterAsync(string email)
{
    return await CreateUserAsync(email)
        .BindAsync(_ => SendWelcomeEmailAsync(email))
        .BindAsync(_ => InitializePreferencesAsync(email));
}
```

---

## `Try` / `TryAsync`

**What** — Executes code that may throw exceptions and converts the outcome into a `Result`, eliminating try-catch boilerplate at call sites.

**Why** — Exception-based APIs are incompatible with functional pipelines. `Try` acts as an adapter, converting any thrown exception into a typed `Failure` so the rest of the pipeline can continue composing.

**When** — Use at the boundary between legacy or third-party APIs and your functional pipeline. Don't scatter try-catch throughout business logic — wrap once at the edge, compose freely inside.

### Overload 1 — Custom error factory

Convert the exception immediately into your domain error type:

```csharp
// Sync
Result<T, TError> Result.Try<T, TError>(
    Func<T> operation,
    Func<Exception, TError> errorFactory)

// Async
Task<Result<T, TError>> Result.TryAsync<T, TError>(
    Func<Task<T>> operation,
    Func<Exception, TError> errorFactory)
```

```csharp
// Parse with a friendly message
var age = Result.Try(
    () => int.Parse(input),
    ex => $"Invalid age: {ex.Message}"
);

// HTTP call converted to a domain error
var response = await Result.TryAsync(
    async () => await _httpClient.GetStringAsync(url),
    ex => $"Request failed: {ex.Message}"
);
```

### Overload 2 — Exception-first

Returns `Result<T, Exception>`, preserving the raw exception so you can inspect, log, or pattern-match it before transforming:

```csharp
// Sync
Result<T, Exception> Result.Try<T>(
    Func<T> operation)

// Async
Task<Result<T, Exception>> Result.TryAsync<T>(
    Func<Task<T>> operation)
```

```csharp
// Log with full stack trace, then map to a friendly message
var result = Result.Try(() => File.ReadAllText(path))
    .TapError(ex => _logger.LogError(ex, "Read failed"))   // full Exception context
    .MapError(ex => ex switch {
        FileNotFoundException       => "File not found",
        UnauthorizedAccessException => "Permission denied",
        _                           => "Failed to read file"
    });
```

**Example — full HTTP pipeline with exception inspection**

```csharp
public async Task<Result<WeatherData, string>> GetWeatherAsync(string city)
{
    return await Result.TryAsync(async () => await _weatherApi.GetAsync(city))
        .TapErrorAsync(ex => {
            switch (ex) {
                case HttpRequestException http:
                    _logger.LogWarning(http, "HTTP {Status} for {City}", http.StatusCode, city);
                    break;
                case TaskCanceledException:
                    _logger.LogWarning("Timeout fetching weather for {City}", city);
                    break;
                default:
                    _logger.LogError(ex, "Unexpected error for {City}", city);
                    break;
            }
        })
        .MapErrorAsync(ex => ex switch {
            HttpRequestException  => "Weather service unavailable",
            TaskCanceledException => "Request timed out",
            _                     => "Failed to fetch weather"
        })
        .BindAsync(json => Result.Try(
            () => JsonSerializer.Deserialize<WeatherData>(json),
            ex  => "Invalid response format"))
        .EnsureNotNullAsync("Weather data was empty");
}
```

---

## `Map` / `MapAsync`

**What** — Transforms the success value of a `Result` using a pure function. If the result is already a failure, the function is never called and the error passes through unchanged.

**Why** — Lets you reshape data inside a pipeline without breaking the flow or introducing new failure modes. It is the standard "transform the happy path" operation.

**When** — Use `Map` when the transformation cannot itself fail (no I/O, no validation, no exceptions). If the transformation can fail, use `Bind` instead.

```csharp
// Sync
Result<T2, TError> Map<T1, T2, TError>(
    this Result<T1, TError> result,
    Func<T1, T2> map)

// Async — 3 overloads covering all combinations:
//   Result    + sync  func  →  .Map(x => ...)
//   Result    + async func  →  .MapAsync(async x => ...)
//   Task<r> + sync  func  →  .MapAsync(x => ...)
//   Task<r> + async func  →  .MapAsync(async x => ...)
```

**Example — transform a domain model into a DTO**

```csharp
public async Task<Result<UserDto, string>> GetUserDtoAsync(int id)
{
    return await FetchUserAsync(id)                              // Result<User, string>
        .Map(user => user with { Name = user.Name.Trim() })     // pure transformation
        .MapAsync(async user => new UserDto
        {
            Id        = user.Id,
            FullName  = $"{user.FirstName} {user.LastName}",
            Email     = user.Email,
            AvatarUrl = await _cdn.ResolveUrlAsync(user.AvatarId)
        });
}
```

---

## `Bind` / `BindAsync`

**What** — Chains an operation that itself returns a `Result`. If the current result is already a failure, the operation is skipped entirely. Also known as `FlatMap` or `SelectMany`.

**Why** — Without `Bind`, chaining fallible operations produces `Result<Result<T, E>, E>` — nested types that are impossible to work with. `Bind` flattens them, keeping the pipeline clean and linear.

**When** — Use `Bind` whenever the next step can fail: a database call, an HTTP request, a validation function that returns a `Result`. If the step cannot fail, use `Map`.

```csharp
// Sync
Result<T2, TError> Bind<T1, T2, TError>(
    this Result<T1, TError> result,
    Func<T1, Result<T2, TError>> bind)

// Async — 3 overloads (same pattern as MapAsync)
```

**Example — multi-step registration flow**

```csharp
public async Task<Result<User, string>> RegisterAsync(RegistrationRequest request)
{
    return await ValidateRequest(request)              // Result<RegistrationRequest, string>
        .BindAsync(r => CheckEmailAvailableAsync(r))   // can fail: email taken
        .BindAsync(r => HashPasswordAsync(r))          // can fail: hashing error
        .BindAsync(r => CreateUserAsync(r))            // can fail: DB error
        .BindAsync(u => SendVerificationEmailAsync(u));// can fail: mail error
    // First failure short-circuits — no nesting, no branching
}
```

**Example — error propagation**

```csharp
var result = ValidateEmail("not-an-email")  // Failure("Invalid email")
    .Bind(email => SendEmail(email));       // ← never runs

// result remains Failure("Invalid email")
```

---

## `BindIf` / `BindIfAsync`

**What** — Conditionally runs a `Bind`-style operation based on a predicate. If the predicate is `true`, the continuation executes. If `false`, the original result passes through unchanged.

**Why** — Some pipeline steps only apply in certain situations. Without `BindIf` you'd need to break the chain, write an `if`, and re-enter the pipeline — destroying composition.

**When** — Use when a step is optional or context-dependent: enrichment only needed for incomplete records, validation only applicable to certain order types, transformations gated on a database flag.

```csharp
// Sync
Result<T, TError> BindIf<T, TError>(
    this Result<T, TError> result,
    Func<T, bool> predicate,
    Func<T, Result<T, TError>> continuation)

// Async — 7 overloads covering sync/async predicates × sync/async continuations
//         × Result or Task<r> inputs
```

> **Rule:** predicate `true` → continuation runs. `false` → result passes through unchanged. Already failed → error propagates, predicate is never evaluated.

**Example — conditional enrichment with an async database check**

```csharp
public async Task<Result<Order, string>> ProcessOrderAsync(Order order)
{
    return await Result<Order, string>.Success(order)
        .BindIfAsync(
            async o => await _db.RequiresEnrichmentAsync(o.Id),  // async predicate
            async o => await EnrichOrderAsync(o)                  // runs only if true
        )
        .BindIfAsync(
            o => o.HasDiscount,                   // sync predicate
            o => ApplyDiscountRulesAsync(o)        // runs only if true
        )
        .BindAsync(o => SaveOrderAsync(o));
}
```

**`BindIf` vs `Ensure`**

| | Predicate is `true` | Predicate is `false` |
|---|---|---|
| `Ensure` | success passes through | **returns Failure** (an already-failed result is left as is) |
| `BindIf` | **continuation executes** | success passes through unchanged |

---

## `MapError` / `MapErrorAsync`

**What** — Transforms the error value of a `Result` using a function. If the result is a success, the function is never called and the value passes through unchanged.

**Why** — Different layers need different error representations. Your repository returns `SqlException`, your service layer wants `DatabaseError`, your API layer needs `ProblemDetails`. `MapError` translates between them without touching the happy path.

**When** — Use at layer boundaries to convert technical errors into domain errors, or domain errors into user-facing messages. Always pair with `TapError` when logging: tap first (side effect), map second (transformation).

```csharp
// Sync
Result<T, TNewError> MapError<T, TError, TNewError>(
    this Result<T, TError> result,
    Func<TError, TNewError> map)

// Async — 3 overloads
```

**Example — layered error translation**

```csharp
// Repository: Exception → domain error
public async Task<Result<User, DatabaseError>> GetUserAsync(int id)
{
    return await Result.TryAsync(() => _db.FindAsync(id))
        .MapErrorAsync(ex => ex switch {
            TimeoutException => new DatabaseError("TIMEOUT", "Database timed out"),
            SqlException sql => new DatabaseError("SQL",     sql.Message),
            _                => new DatabaseError("UNKNOWN", ex.Message)
        });
}

// Controller: domain error → HTTP response
public async Task<IActionResult> GetUser(int id)
{
    var result = await _service.GetUserAsync(id)
        .MapErrorAsync(dbErr => dbErr.Code switch {
            "TIMEOUT" => (Status: 503, Message: "Service temporarily unavailable"),
            _         => (Status: 500, Message: "An error occurred")
        });

    return result.Match(
        user => Ok(user),
        err  => StatusCode(err.Status, new { err.Message })
    );
}
```

---

## `Match` / `MatchAsync`

**What** — Extracts a final value from a `Result` by providing a handler for both outcomes. It is the only way to safely unwrap a result.

**Why** — Forces you to handle both the success and failure cases before leaving the `Result` world. This is the natural exit point of a pipeline — you've composed all your transformations and now need a concrete value to return.

**When** — Use at the outermost boundary of a pipeline: returning an `IActionResult`, building a view model, formatting a message. Do not use `Match` in the middle of a pipeline — use `Map` or `Bind` there.

```csharp
// Sync
TResult Match<T, TError, TResult>(
    this Result<T, TError> result,
    Func<T,      TResult> mapValue,
    Func<TError, TResult> mapError)

// Async — 7 overloads covering all combinations of sync/async handlers × sync/async results
```

**Example — API controller exit point**

```csharp
public async Task<IActionResult> CreateOrder(CreateOrderRequest request)
{
    var result = await _orderService.PlaceOrderAsync(request);

    return result.Match(
        order => Created($"/orders/{order.Id}", order),
        error => error.Code switch {
            "VALIDATION" => UnprocessableEntity(new { error.Message }),
            "INVENTORY"  => Conflict(new { error.Message }),
            _            => StatusCode(500, new { error.Message })
        }
    );
}
```

**Example — async handlers at both branches**

```csharp
return await result.MatchAsync(
    async order => {
        await _auditLog.LogOrderCreatedAsync(order);
        return Created($"/orders/{order.Id}", order);
    },
    async error => {
        await _auditLog.LogFailureAsync(error);
        return BadRequest(new { error });
    }
);
```

---

## `Ensure` / `EnsureAsync`

**What** — Validates a condition on a successful value. If the condition holds, the result passes through unchanged. If not, it becomes a `Failure` with the provided error. A result that is already a `Failure` passes through untouched — its original error is kept and the predicate is not evaluated.

**Why** — Validation rules belong in the pipeline, not scattered across ad hoc `if` statements. `Ensure` keeps all business rules inline, composable, and easy to read.

**When** — Use for business rule validation: range checks, format rules, cross-field constraints. Chain multiple `Ensure` calls to build a validation gate — the first failing rule short-circuits the rest.

```csharp
// Sync
Result<T, TError> Ensure<T, TError>(
    this Result<T, TError> result,
    Func<T, bool> predicate,
    TError error)

// Async — supports Task<r> inputs and async predicates
Task<Result<T, TError>> EnsureAsync<T, TError>(
    this Task<Result<T, TError>> result,
    Func<T, bool> predicate,
    TError error)
```

**Example — order validation pipeline**

```csharp
public Result<Order, string> ValidateOrder(OrderRequest request)
{
    return request.ToResult("Order request is required")
        .Ensure(r => r.Items.Any(),                       "Order must have at least one item")
        .Ensure(r => r.Items.All(i => i.Quantity > 0),   "All quantities must be positive")
        .Ensure(r => r.Total > 0,                         "Order total must be greater than zero")
        .Ensure(r => !string.IsNullOrEmpty(r.CustomerId), "Customer ID is required")
        .Map(r => new Order(r));
}
```

**Example — async predicate (database uniqueness check)**

```csharp
public async Task<Result<Account, string>> CreateAccountAsync(string email)
{
    return await ValidateEmail(email)
        .EnsureAsync(
            async e => !await _db.EmailExistsAsync(e),
            "Email address is already registered")
        .BindAsync(e => CreateAccountRecordAsync(e));
}
```

---

## `EnsureNotNull` / `EnsureNotNullAsync`

**What** — Converts a `Result<T?, TError>` (nullable success value) into a `Result<T, TError>` (non-nullable), failing with a provided error if the value is `null`.

**Why** — Many APIs return nullable values: `_cache.Get(key)` returns `T?`, `_db.Find(id)` returns `T?`. `EnsureNotNull` bridges C#'s nullable reference type system into the Result pipeline without boilerplate null checks.

**When** — Use whenever a nullable value enters the pipeline from a cache, a database query, a dictionary lookup, or any API that returns `null` to signal "not found".

```csharp
// Sync
Result<T, TError> EnsureNotNull<T, TError>(
    this Result<T?, TError> result,
    TError errorWhenNull)
    where T : class

// Async
Task<Result<T, TError>> EnsureNotNullAsync<T, TError>(
    this Task<Result<T?, TError>> result,
    TError errorWhenNull)
    where T : class
```

**Example — cache lookup with not-found handling**

```csharp
public async Task<Result<User, string>> GetUserAsync(int id)
{
    return await Result.TryAsync(() => _cache.GetAsync<User>(id))  // returns User?
        .EnsureNotNullAsync("User not found in cache")
        .Ensure(user => user.IsActive, "User account is inactive")
        .TapAsync(user => _logger.LogInfo("Cache hit for user {Id}", id));
}
```

**Example — inside a product lookup pipeline**

```csharp
public async Task<Result<Product, string>> GetProductAsync(string sku)
{
    return await Result.TryAsync(async () => await _db.Products.FindAsync(sku))
        .EnsureNotNullAsync($"Product '{sku}' not found")
        .Ensure(p => !p.IsDiscontinued, "Product has been discontinued")
        .MapAsync(async p => await EnrichWithInventoryAsync(p));
}
```

---

## `ToResult`

**What** — Converts a nullable reference type (`T?`) directly into a `Result<T, TError>`, succeeding if the value is non-null and failing with the provided error if it is null.

**Why** — The C# nullable type system produces `T?` values everywhere. `ToResult` is the idiomatic entry point into a BindSharp pipeline when your starting value may be null.

**When** — Use at the top of a pipeline when the input may be null: session values, cache reads, dictionary lookups, first-or-default queries, nullable constructor arguments.

```csharp
Result<T, TError> ToResult<T, TError>(
    this T? value,
    TError error)
    where T : class
```

**Example — session-based pipeline**

```csharp
public Result<Product, string> GetProductFromSession(HttpContext context)
{
    return context.Session.GetString("current_product")
        .ToResult("No product selected in session")
        .Bind(json => Result.Try(
            () => JsonSerializer.Deserialize<Product>(json),
            ex  => "Corrupt session data"))
        .EnsureNotNull("Product was null after deserialization")
        .Ensure(p => !p.IsDeleted, "Product is no longer available");
}
```

**Example — dictionary lookup**

```csharp
var apiKey = _settings.GetValueOrDefault("ApiKey")
    .ToResult("ApiKey is not configured")
    .Ensure(key => key.Length > 10, "ApiKey appears invalid");
```

---

## `AsTask`

**What** — Wraps a synchronous `Result<T, TError>` in a completed `Task<Result<T, TError>>` using `Task.FromResult`.

**Why** — Async pipelines and async method return types require `Task<Result<...>>`. `AsTask` lets a synchronous result fit into an async context without verbose wrapping syntax.

**When** — Use on a fast synchronous path (cache hit, guard clause, early return) inside a method whose signature is `Task<Result<...>>`, or when bridging a sync result into an async pipeline chain.

```csharp
Task<Result<T, TError>> AsTask<T, TError>(
    this Result<T, TError> result)
```

**Example — fast synchronous path in an async method**

```csharp
public Task<Result<User, string>> GetUserAsync(int id)
{
    // Fast path — sync cache hit, but return type must be Task<Result<...>>
    var cached = _cache.Get<User>($"user:{id}");
    if (cached != null)
        return Result<User, string>.Success(cached).AsTask();

    // Slow path — async database fetch
    return FetchUserFromDatabaseAsync(id);
}
```

---

## `Tap` / `TapAsync`

**What** — Executes a side effect on a **successful** result's value without modifying the result. The result flows through unchanged.

**Why** — Logging, caching, and metrics don't belong as transformation steps — they shouldn't change the value or error type. `Tap` keeps side effects visible in the pipeline without polluting the data flow.

**When** — Use for success-path side effects: writing to a log, updating a cache, emitting a metric, triggering a notification after a successful step. For failure-path side effects use `TapError`. For both in one call use `Do`.

```csharp
// Sync: Result + sync action
Result<T, TError> Tap<T, TError>(
    this Result<T, TError> result,
    Action<T> action)

// Async — 3 overloads:
//   Result    + Func<T, Task>    → TapAsync   (async action on sync result)
//   Task<r> + Func<T, Task>  → TapAsync   (async action on async result)
//   Task<r> + Action<T>      → TapAsync   (sync action in async pipeline ✅)
```

**Example — observability through a pipeline**

```csharp
public async Task<Result<Order, string>> ProcessOrderAsync(Order order)
{
    return await ValidateOrder(order)
        .TapAsync(o => _logger.LogInfo("Order {Id} validated", o.Id))    // sync action ✅
        .BindAsync(o => ReserveInventoryAsync(o))
        .TapAsync(async o => await _cache.SetAsync($"order:{o.Id}", o))  // async action ✅
        .TapAsync(o => _metrics.Increment("orders.reserved"))            // sync action ✅
        .BindAsync(o => ChargePaymentAsync(o));
}
```

---

## `TapError` / `TapErrorAsync`

**What** — Executes a side effect on a **failed** result's error without modifying the result. The result flows through unchanged.

**Why** — Errors need logging, alerting, and metrics too. `TapError` is the symmetric counterpart to `Tap` — it fires on the failure rail while leaving the success rail completely untouched.

**When** — Use for failure-path side effects: logging exceptions, incrementing error counters, sending alerts. Keep `TapError` for side effects only — use `MapError` if you need to transform the error value, not just observe it.

```csharp
// Sync: Result + sync action
Result<T, TError> TapError<T, TError>(
    this Result<T, TError> result,
    Action<TError> action)

// Async — 3 overloads (mirrors TapAsync):
//   Result    + Func<TError, Task>    → TapErrorAsync
//   Task<r> + Func<TError, Task>  → TapErrorAsync
//   Task<r> + Action<TError>      → TapErrorAsync  (sync action in async pipeline ✅)
```

**Example — exception-first pattern: log then transform**

```csharp
var result = await Result.TryAsync(async () => await _db.SaveAsync(record))
    .TapErrorAsync(ex => {
        _logger.LogError(ex, "Save failed for record {Id}", record.Id);
        _metrics.Increment("db.errors");

        if (ex is SqlException sql && sql.Number == 2627)
            _alerting.NotifyDuplicateKey(record.Id);
    })
    .MapErrorAsync(ex => ex switch {
        SqlException sql when sql.Number == 2627 => "Duplicate record",
        TimeoutException => "Database timed out",
        _ => "Failed to save record"
    });
```

**`Tap` vs `TapError` vs `MapError`**

| Method | Fires on | Modifies result? | Use for |
|---|---|---|---|
| `Tap` | Success only | No | Log success, update cache, emit metric |
| `TapError` | Failure only | No | Log error, send alert, increment counter |
| `MapError` | Failure only | **Yes** — changes error type | Transform error to a different type |

---

## `Do` / `DoAsync`

**What** — Executes exactly one of two side effects depending on whether the result is a success or failure, then returns the result unchanged.

**Why** — Many cross-cutting concerns (logging, metrics, audit trails) require different actions for both outcomes. Doing this with `Tap` + `TapError` splits related logic across two separate calls. `Do` groups them together, making the intent clear and reducing the risk of forgetting one branch.

**When** — Use when success and failure handling are related concerns that belong together: paired log messages, metric success/failure counters, audit entries with different codes. Use separate `Tap` + `TapError` when the two handlers are genuinely unrelated (e.g., cache write on success vs. retry scheduler on failure).

```csharp
// Sync: both handlers sync
Result<T, TError> Do<T, TError>(
    this Result<T, TError> result,
    Action<T>      onSuccess,
    Action<TError> onFailure)

// DoAsync — 8 overloads covering all combinations:
//   Result or Task<r> input
//   × sync or async onSuccess
//   × sync or async onFailure
```

**Example — paired logging at every pipeline stage**

```csharp
public async Task<Result<Order, string>> PlaceOrderAsync(Cart cart)
{
    return await ValidateCart(cart)
        .DoAsync(
            _        => _logger.LogInfo("Cart validated"),
            error    => _logger.LogWarning("Cart validation failed: {Error}", error)
        )
        .BindAsync(c => ReserveInventoryAsync(c))
        .DoAsync(
            async o  => await _metrics.RecordAsync("inventory.reserved"),
            async err => await _alerting.NotifyLowStockAsync(err)
        )
        .BindAsync(o => ChargePaymentAsync(o))
        .DoAsync(
            o    => { _metrics.Increment("orders.success"); _logger.LogInfo("Order placed: {Id}", o.Id); },
            err  => { _metrics.Increment("orders.failed");  _logger.LogError("Payment failed: {Error}", err); }
        );
}
```

**`Do` vs `Match`**

| | Returns | Use for |
|---|---|---|
| `Match` | A new value (`TResult`) | **Exiting** the pipeline — produce a string, IActionResult, etc. |
| `Do` | Same `Result<T, TError>` | **Staying** in the pipeline — side effects only |

---

## `Using` / `UsingAsync`

**What** — Executes an operation with an `IDisposable` resource and guarantees its disposal — whether the operation succeeds, fails, or throws. This is the functional "bracket" pattern.

**Why** — `using` statements don't compose inside functional pipelines. A database transaction opened mid-pipeline needs to be disposed even if the next five steps fail. `Using` provides that guarantee while keeping the pipeline linear and readable.

**When** — Use whenever a `Result` carries a resource that must be cleaned up: database transactions, file streams, HTTP response streams, connection objects.

```csharp
// Sync
Result<TResult, TError> Using<TResource, TResult, TError>(
    this Result<TResource, TError> resource,
    Func<TResource, Result<TResult, TError>> operation)
    where TResource : IDisposable

// Async — 2 overloads:
//   Result<TResource>          + async operation
//   Task<Result<TResource>>    + async operation
```

**Example — database transaction**

```csharp
public async Task<Result<Order, string>> CreateOrderAsync(CreateOrderRequest request)
{
    return await Result.TryAsync(
            async () => await _db.BeginTransactionAsync(),
            ex => $"Could not start transaction: {ex.Message}")
        .UsingAsync(async transaction =>
            await ValidateRequest(request)
                .BindAsync(r => InsertOrderAsync(r))
                .BindAsync(o => UpdateInventoryAsync(o))
                .TapAsync(async o => await transaction.CommitAsync())
                .MapErrorAsync(async error => {
                    await transaction.RollbackAsync();
                    return error;
                })
        );
    // transaction.Dispose() is always called ✅
}
```

**Example — file stream**

```csharp
public Result<Report, string> GenerateReport(string path)
{
    return Result.Try(
            () => File.OpenRead(path),
            ex => $"Cannot open file: {ex.Message}")
        .Using(stream =>
            Result.Try(
                () => _parser.Parse(stream),
                ex => $"Parse error: {ex.Message}")
        );
    // stream.Dispose() always called ✅
}
```

---

## Putting It All Together

A complete order processing pipeline using every feature:

```csharp
using BindSharp;
using BindSharp.Extensions;

public async Task<Result<OrderConfirmation, OrderError>> PlaceOrderAsync(Cart cart)
{
    return await cart.ToResult(OrderError.InvalidCart)             // ToResult

        // Ensure — inline business rule validation
        .Ensure(c => c.Items.Any(),        OrderError.EmptyCart)
        .Ensure(c => c.CustomerId != null, OrderError.MissingCustomer)

        // Try + exception-first — wrap external API, log before transforming
        .BindAsync(async c => await Result.TryAsync(
                async () => await _inventory.CheckStockAsync(c.Items))
            .TapErrorAsync(ex => _logger.LogError(ex, "Inventory check failed"))
            .MapErrorAsync(ex => OrderError.InventoryUnavailable))

        // Do — paired logging after validation
        .DoAsync(
            _    => _logger.LogInfo("Cart validated, proceeding to price"),
            err  => _logger.LogWarning("Validation failed: {Error}", err))

        // Bind — price calculation
        .BindAsync(CalculatePriceAsync)

        // BindIf — only apply special handling for eligible orders
        .BindIfAsync(
            async o => await RequiresSpecialHandlingAsync(o),
            ApplySpecialHandlingAsync)

        // Try + UsingAsync — transaction opened, used, and always disposed
        .BindAsync(async order => await Result.TryAsync(
                async () => await _db.BeginTransactionAsync(),
                ex => OrderError.TransactionFailed)
            .UsingAsync(async tx =>
                await ChargeCustomerAsync(order)
                    .TapAsync(async _ => await tx.CommitAsync())
                    .MapErrorAsync(async err => {
                        await tx.RollbackAsync();
                        return err;
                    })))

        // Bind + Tap — persist and cache
        .BindAsync(CreateOrderRecordAsync)
        .TapAsync(async o => await _cache.SetAsync($"order:{o.Id}", o))

        // Map — project to confirmation DTO
        .MapAsync(order => new OrderConfirmation(order))

        // Do — final metrics, audit, and alerting
        .DoAsync(
            async conf => {
                await _auditLog.LogOrderCreatedAsync(conf);
                _metrics.Increment("orders.success");
            },
            async err => {
                await _auditLog.LogFailureAsync(err);
                _metrics.Increment("orders.failed");
                await _alerting.NotifyOpsAsync($"Order failure: {err}");
            });
}
```

---

## Installation

```bash
dotnet add package BindSharp
```

Targets **netstandard2.0** — works with .NET Framework 4.6.1+, .NET Core 2.0+, and all modern .NET versions. Zero dependencies.

---

## API Quick Reference

### `BindSharp` namespace

| Type / Method | Description |
|---|---|
| `Result<T, TError>` | Core result type |
| `Unit` / `Unit.Value` | Represents no value |
| `Result.Try<T, TError>` | Sync exception handling with custom error factory |
| `Result.Try<T>` | Sync exception-first — returns `Result<T, Exception>` |
| `Result.TryAsync<T, TError>` | Async exception handling with custom error factory |
| `Result.TryAsync<T>` | Async exception-first — returns `Task<Result<T, Exception>>` |
| `.Map` / `.MapAsync` | Transform success value (cannot fail) |
| `.Bind` / `.BindAsync` | Chain a fallible operation |
| `.BindIf` / `.BindIfAsync` | Conditionally execute an operation |
| `.MapError` / `.MapErrorAsync` | Transform error value |
| `.Match` / `.MatchAsync` | Exit pipeline — handle both outcomes |

### `BindSharp.Extensions` namespace

```csharp
using BindSharp.Extensions;
```

| Method | Description |
|---|---|
| `.Ensure` / `.EnsureAsync` | Validate a condition; fail if false |
| `.EnsureNotNull` / `.EnsureNotNullAsync` | Fail if success value is null |
| `.ToResult` | Convert a nullable value to a Result |
| `.AsTask` | Wrap a sync Result in a Task |
| `.Tap` / `.TapAsync` | Side effect on success only |
| `.TapError` / `.TapErrorAsync` | Side effect on failure only |
| `.Do` / `.DoAsync` | Side effects on both outcomes |
| `.Using` / `.UsingAsync` | Safe `IDisposable` resource management |

---

## Best Practices

1. **Define custom error types.** `Result<User, UserError>` is far safer than `Result<User, string>`. Use `record` types for easy equality and deconstruction.

2. **Never use the same type for `T` and `TError`.** Implicit conversions become ambiguous and produce subtle bugs.

3. **`Bind` for fallible steps, `Map` for pure transformations.** If a step can fail, it returns a `Result` — use `Bind`. If it's a pure projection, use `Map`.

4. **`Try` at the edges, not deep in domain logic.** Wrap third-party and framework APIs once at the boundary. Keep domain code exception-free.

5. **`TapError` before `MapError`.** Log the raw exception or original error with `TapError`, then convert it with `MapError`. Never mix logging and transformation in the same function.

6. **`Match` at the outermost boundary.** Convert `Result` to `IActionResult`, view models, or formatted strings at the API or UI layer — not inside business logic.

7. **`Do` for related concerns, `Tap` + `TapError` for separate ones.** Paired log messages belong in `Do`. An unrelated cache write on success and a retry-scheduler on failure belong in separate calls.

---

## Acknowledgments

Special thanks to **[Zoran Horvat](https://www.youtube.com/@zoran-horvat)** — his tutorials on Railway-Oriented Programming in C# are the foundation this library is built on. Go give his channel the views it deserves. 🙏

## License

[MPL-2.0](https://opensource.org/licenses/MPL-2.0)

## Contributing

Issues and pull requests are welcome!

---

Built with ❤️ for the .NET community
