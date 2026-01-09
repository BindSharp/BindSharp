# Migration Guide: BindSharp v1.x → v2.0

## Overview

BindSharp v2.0 brings improved organization and maintainability through namespace restructuring and API refinements. While there are breaking changes, migration is straightforward and can be done incrementally.

## Breaking Changes

### 1. Extension Methods Namespace

**Change:** Extension methods moved to `BindSharp.Extensions` namespace

**Before (v1.x):**
```csharp
using BindSharp;

var result = GetData()
    .Tap(x => Console.WriteLine(x))
    .Ensure(x => x > 0, "Must be positive");
```

**After (v2.0):**
```csharp
using BindSharp;
using BindSharp.Extensions;  // ✨ Add this

var result = GetData()
    .Tap(x => Console.WriteLine(x))
    .Ensure(x => x > 0, "Must be positive");
```

**Migration:**
- Add `using BindSharp.Extensions;` to files that use extension methods
- Or use fully qualified names: `result.BindSharp.Extensions.Tap(...)`

### 2. Static Utilities Rename

**Change:** `ResultExtensions` → `Result`

**Before (v1.x):**
```csharp
var result = ResultExtensions.Try(
    () => int.Parse(input),
    ex => "Invalid number"
);

var data = await ResultExtensions.TryAsync(
    async () => await FetchAsync(),
    ex => "Fetch failed"
);
```

**After (v2.0):**
```csharp
var result = Result.Try(
    () => int.Parse(input),
    ex => "Invalid number"
);

var data = await Result.TryAsync(
    async () => await FetchAsync(),
    ex => "Fetch failed"
);
```

**Migration:**
- Global find/replace: `ResultExtensions.Try` → `Result.Try`
- Global find/replace: `ResultExtensions.TryAsync` → `Result.TryAsync`

## New Features in v2.0

### Do/DoAsync - Combined Side Effects

**New in v2.0!** Execute different side effects for success and failure in a single method.

**Before (v1.x) - Separate Tap + TapError:**
```csharp
var result = await ProcessDataAsync()
    .TapAsync(data => _logger.LogInfo($"Success: {data}"))
    .TapErrorAsync(error => _logger.LogError($"Failed: {error}"));
```

**After (v2.0) - Single Do:**
```csharp
var result = await ProcessDataAsync()
    .DoAsync(
        data => _logger.LogInfo($"Success: {data}"),
        error => _logger.LogError($"Failed: {error}")
    );
```

**Benefits:**
- ✅ Less verbose - one method instead of two
- ✅ Clear intent - handles both paths together
- ✅ All async combinations supported (sync/sync, sync/async, async/sync, async/async)

## Complete Migration Checklist

### Step 1: Update Package Reference
```xml
<!-- Before -->
<PackageReference Include="BindSharp" Version="1.6.0" />

<!-- After -->
<PackageReference Include="BindSharp" Version="2.0.0" />
```

### Step 2: Add Extension Namespace
Add to files using extension methods:
```csharp
using BindSharp.Extensions;
```

### Step 3: Rename Static Class
Find and replace across your solution:
- `ResultExtensions.Try` → `Result.Try`
- `ResultExtensions.TryAsync` → `Result.TryAsync`

### Step 4: Consider Using Do (Optional)
Replace paired Tap/TapError with Do for cleaner code:
```csharp
// Old style still works
.TapAsync(x => LogSuccess(x))
.TapErrorAsync(e => LogError(e))

// New style (recommended)
.DoAsync(
    x => LogSuccess(x),
    e => LogError(e)
)
```

## File Organization Changes

**For library maintainers only** - these changes don't affect consumers:

### v1.x Structure
```
BindSharp/
├── Result.cs
├── Unit.cs
├── FunctionalResult.cs
├── AsyncFunctionalResult.cs
└── ResultExtensions.cs  (monolithic - 500+ lines)
```

### v2.0 Structure
```
BindSharp/
├── Result.cs
├── Unit.cs
├── FunctionalResult.cs
├── AsyncFunctionalResult.cs
├── ResultUtilities.cs  (formerly ResultExtensions)
└── Extensions/
    ├── SideEffectExtensions.cs       (Tap, TapError, Do)
    ├── ValidationExtensions.cs       (Ensure, EnsureNotNull)
    ├── ConversionExtensions.cs       (ToResult, AsTask)
    └── ResourceManagementExtensions.cs (Using)
```

## Example: Complete Migration

### Before (v1.x)
```csharp
using BindSharp;

public class OrderService
{
    public async Task<Result<Order, string>> ProcessOrderAsync(CreateOrderRequest request)
    {
        return await ResultExtensions.Try(
                () => ValidateRequest(request),
                ex => $"Validation error: {ex.Message}"
            )
            .BindAsync(async req => await CreateOrderAsync(req))
            .TapAsync(async order => await _logger.LogInfoAsync($"Order created: {order.Id}"))
            .TapErrorAsync(async error => await _logger.LogErrorAsync($"Failed: {error}"))
            .BindAsync(async order => await NotifyCustomerAsync(order));
    }
}
```

### After (v2.0)
```csharp
using BindSharp;
using BindSharp.Extensions;  // ✨ Added

public class OrderService
{
    public async Task<Result<Order, string>> ProcessOrderAsync(CreateOrderRequest request)
    {
        return await Result.Try(  // ✨ Renamed from ResultExtensions
                () => ValidateRequest(request),
                ex => $"Validation error: {ex.Message}"
            )
            .BindAsync(async req => await CreateOrderAsync(req))
            .DoAsync(  // ✨ Combined Tap + TapError
                async order => await _logger.LogInfoAsync($"Order created: {order.Id}"),
                async error => await _logger.LogErrorAsync($"Failed: {error}")
            )
            .BindAsync(async order => await NotifyCustomerAsync(order));
    }
}
```

## FAQ

### Q: Do I need to migrate immediately?
**A:** No. v1.x will continue to work. Migrate when convenient for your project.

### Q: Can I use v1.x and v2.0 side by side?
**A:** No. Choose one version for your project. The migration is straightforward enough to do in one go.

### Q: Will there be more breaking changes?
**A:** v2.0 establishes a stable, maintainable structure. Future versions will focus on additions rather than breaking changes.

### Q: What about performance?
**A:** Zero impact. The refactoring is purely organizational - the compiled IL is identical.

### Q: Do I have to use Do instead of Tap + TapError?
**A:** No! Both patterns work. `Do` is just a convenient shorthand. Use whichever is clearer for your use case.

## Support

- **Issues:** [GitHub Issues](https://github.com/YourRepo/BindSharp/issues)
- **Discussions:** [GitHub Discussions](https://github.com/YourRepo/BindSharp/discussions)
- **Breaking changes:** This is the complete list for v2.0

---

**Summary:** Add `using BindSharp.Extensions;` and rename `ResultExtensions` → `Result`. That's it! 🚀
