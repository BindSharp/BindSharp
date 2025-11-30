# BindSharp 1.4.1 Release Notes

## 🎉 What's New

Version 1.4.1 introduces **conditional processing** to functional pipelines, enabling you to create clean if-then logic without breaking your Railway-Oriented Programming chains.

### New Feature: BindIf / BindIfAsync

**The Problem:**
Previously, when you needed conditional logic in a pipeline (e.g., "if needs processing, then process"), you had to break the chain or use awkward workarounds:
```csharp
// ❌ Before: Awkward nested Bind
var result = GetPayload()
    .Bind(payload => 
        payload.NeedsProcessing
            ? ProcessPayload(payload)           // Process it
            : Result<string, string>.Success(payload)  // Return as-is
    );
```

**The Solution:**
`BindIf` provides clean conditional processing with standard if-then behavior:
```csharp
// ✅ After: Clean and intuitive
var result = GetPayload()
    .BindIf(
        payload => payload.NeedsProcessing,  // If TRUE
        payload => ProcessPayload(payload)   // Then execute
    );
```

---

## 📦 New API Methods

### FunctionalResult

#### `BindIf<T, TError>`
```csharp
public static Result<T, TError> BindIf<T, TError>(
    this Result<T, TError> result,
    Func<T, bool> predicate,
    Func<T, Result<T, TError>> continuation)
```

Conditionally applies a continuation function based on a predicate:
- If predicate returns **true** → applies continuation function
- If predicate returns **false** → returns original result unchanged (short-circuit)
- If result is already failed → propagates error without evaluating predicate

**Standard if-then behavior:** Works like a regular `if` statement in functional pipelines.

### AsyncFunctionalResult

#### `BindIfAsync<T, TError>` - 7 Overloads

Complete async support covering all scenarios:

1. **Task\<Result\> + sync predicate + sync continuation**
2. **Result + sync predicate + async continuation**
3. **Task\<Result\> + sync predicate + async continuation**
4. **Result + async predicate + sync continuation**
5. **Task\<Result\> + async predicate + sync continuation**
6. **Result + async predicate + async continuation**
7. **Task\<Result\> + async predicate + async continuation**

This matches BindSharp's comprehensive async pattern seen in `MatchAsync`.

---

## 💡 Usage Examples

### Example 1: Conditional Enrichment
```csharp
// Enrich user data only if incomplete
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

### Example 2: Conditional Validation
```csharp
// Validate order only if it requires validation
public async Task<Result<Order, string>> ProcessOrderAsync(Order order)
{
    return await Result<Order, string>.Success(order)
        .BindIfAsync(
            async o => await RequiresValidationAsync(o.Id),  // If needs validation
            async o => await ValidateOrderAsync(o)           // Then validate
        )
        .BindAsync(async o => await SaveOrderAsync(o));
}
```

### Example 3: JSON Extraction
```csharp
// Extract JSON only if payload is NOT already JSON
public Result<string, string> ExtractJson(string payload)
{
    return Result<string, string>.Success(payload)
        .Map(p => p.TrimStart())
        .BindIf(
            p => !(p.StartsWith("{") || p.StartsWith("[")),  // If NOT JSON
            p => ExtractJsonAfterPrefix(p)                    // Then extract
        )
        .Ensure(json => !string.IsNullOrEmpty(json), "JSON cannot be empty");
}
```

### Example 4: Caching Pattern
```csharp
// Fetch from source only if not cached
public async Task<Result<Data, string>> GetDataAsync(string key)
{
    return await CheckCacheAsync(key)
        .BindIfAsync(
            cached => cached == null,  // If not cached (TRUE)
            async _ => await FetchFromSourceAsync(key)  // Then fetch
        )
        .TapAsync(async data => await UpdateCacheAsync(key, data));
}
```

### Example 5: Complete Pipeline
```csharp
public async Task<Result<ProcessedData, string>> ProcessDataAsync(string input)
{
    return await ValidateInput(input)
        .MapAsync(async data => await NormalizeDataAsync(data))
        .BindIfAsync(
            async data => await RequiresExpensiveProcessingAsync(data.Id),
            async data => await ExpensiveProcessingAsync(data)
        )
        .TapAsync(async data => await LogProcessingAsync(data))
        .MapAsync(data => new ProcessedData(data));
}
```

---

## 🎯 When to Use BindIf

### ✅ Use BindIf When:
- You need conditional execution without breaking the chain
- "If condition is true, then do something"
- You need standard if-then logic in functional pipelines
- Processing is expensive and should be skipped when unnecessary
- You want to avoid nested `Bind` calls for conditions

### ✅ Perfect For:
- **Conditional processing** - "If needs processing, then process"
- **Enrichment** - "If incomplete, then enrich from database"
- **Validation** - "If invalid, then apply fixes"
- **Caching** - "If not cached, then fetch from source"
- **Optimization** - "If expensive path needed, then execute"

### ❌ Don't Use BindIf When:
- You need validation that fails (use `Ensure` instead)
- You need to change the result type (use `Bind` instead)
- You're doing pure transformation (use `Map` instead)
- You need side effects without conditions (use `Tap` instead)

---

## 🔄 Comparison with Other Methods

| Method | Use Case | Changes Result Type? | Executes Continuation? |
|--------|----------|---------------------|------------------------|
| `Map` | Transform value | Yes | Always (on success) |
| `Bind` | Chain operations | Yes | Always (on success) |
| `Ensure` | Validate condition | No | Never (validates only) |
| `BindIf` | Conditional execution | No | Only if predicate TRUE |
| `Tap` | Side effects | No | Always (on success) |

**Key Differences:**
- `Ensure` - Fails if condition is false (validation)
- `BindIf` - Executes continuation if condition is true (conditional processing)
- `Tap` - Always executes (side effects)

---

## 📊 Performance Characteristics

- **Zero allocation overhead** for sync versions
- **Async versions** follow standard Task patterns
- **Short-circuit optimization** - continuation not called if predicate is false
- **Predicate evaluated only once** per invocation

---

## 🔧 Migration from 1.3.x

**No migration needed!** All changes are backwards compatible.

### What Continues to Work:
```csharp
// All existing code unchanged
var result = GetData()
    .Map(x => x * 2)
    .Bind(Validate)
    .Ensure(x => x > 0, "Must be positive");
```

### What You Can Now Do:
```csharp
// New conditional processing
var result = GetData()
    .Map(x => x * 2)
    .BindIf(x => x > 100, x => ProcessLargeValue(x))  // If > 100, process
    .Ensure(x => x > 0, "Must be positive");
```

---

## 📖 Documentation Updates

- Added `BindIf` section to README.md
- Added 7 async overloads documentation
- Added real-world examples for conditional processing
- Updated API reference

---

## 🧪 Testing

- 48 comprehensive unit tests covering all overloads
- Tests for predicate evaluation and continuation execution
- Tests for short-circuiting when predicate is false
- Tests for error propagation
- Real-world scenario tests (JSON extraction, user enrichment, caching)

---

## 📋 Changelog

### Added
- `BindIf<T, TError>` - Conditional processing in functional pipelines
- `BindIfAsync<T, TError>` (7 overloads) - Full async support for conditional processing
  - Task\<Result\> + sync predicate + sync continuation
  - Result + sync predicate + async continuation
  - Task\<Result\> + sync predicate + async continuation
  - Result + async predicate + sync continuation
  - Task\<Result\> + async predicate + sync continuation
  - Result + async predicate + async continuation
  - Task\<Result\> + async predicate + async continuation
- Comprehensive XML documentation for all new methods
- Package tags: `conditional-branching`, `bindif`

### Changed
- None (no breaking changes)

### Fixed
- None

### Removed
- None

---

## 🎓 Best Practices

### DO:
✅ Use `BindIf` for conditional execution ("if X, then Y")  
✅ Use async predicates when condition requires I/O (database, cache)  
✅ Combine with other Result methods for complex pipelines  
✅ Keep predicates simple and side-effect free  
✅ Think of it as standard `if` statements in functional style  

### DON'T:
❌ Use `BindIf` when `Ensure` is more appropriate (validation that should fail)  
❌ Put complex logic in predicates (extract to named methods)  
❌ Modify state in predicates (keep them pure)  
❌ Use when you need to change the result type (use `Bind`)  

---

## 💡 Pattern Examples

### Pattern 1: Conditional Enrichment
```csharp
// "If incomplete, then enrich"
.BindIfAsync(
    entity => !entity.IsComplete,
    async entity => await EnrichAsync(entity)
)
```

### Pattern 2: Negative Conditions
```csharp
// "If NOT already processed, then process"
.BindIf(
    data => !data.IsProcessed,
    data => ProcessData(data)
)
```

### Pattern 3: Async Database Checks
```csharp
// "If requires validation (async check), then validate"
.BindIfAsync(
    async order => await RequiresValidationAsync(order.Id),
    async order => await ValidateAsync(order)
)
```

### Pattern 4: Caching
```csharp
// "If not in cache, then fetch"
.BindIfAsync(
    async key => !await ExistsInCacheAsync(key),
    async key => await FetchFromSourceAsync(key)
)
```

---

## 🔗 Related Methods

- **`Bind`** - Chain operations that can fail (always executes on success)
- **`Ensure`** - Validate conditions (fails if false)
- **`Map`** - Transform success values (always executes on success)
- **`Tap`** - Execute side effects (always executes on success)

---

## 🚀 Next Steps

1. **Update your package:**
```bash
   dotnet add package BindSharp --version 1.4.1
```

2. **Explore conditional processing** in your pipelines

3. **Simplify existing code** that uses nested `Bind` for conditionals

4. **Use async predicates** for database-backed conditions

---

## 🙏 Acknowledgments

Thanks to the community for feedback and suggestions that led to this feature!

Special thanks to all contributors and users who helped shape this release.

---

## 📚 Additional Resources

- **GitHub:** https://github.com/GDB-11/BindSharp/
- **Documentation:** See README.md for complete usage guide
- **Previous Releases:**
  - [1.3.0](RELEASE_NOTES_1_3_0.md) - Equality & implicit conversions
  - [1.2.0](RELEASE_NOTES_1_2_0.md) - Unit type
  - [1.1.0](RELEASE_NOTES_1_1_0.md) - ResultExtensions utilities

---

## 🎉 Conclusion

Version 1.4.1 brings intuitive conditional processing to BindSharp's functional pipelines:
- ✅ Standard if-then logic in Railway-Oriented Programming
- ✅ Full async support (7 overloads)
- ✅ Zero breaking changes
- 🎯 More expressive and readable functional code

Upgrade today and enjoy cleaner, more intuitive functional code!

Happy coding! 🚀
