# BindSharp 1.2.0 Release Notes

## New Features

### Unit Struct
Added `Unit` type for representing "no value" in functional programming patterns.

**Usage:**
```csharp
public Task<Result<Unit, Error>> SaveAsync(Data data) =>
    TryAsync(() => repository.SaveAsync(data))
        .MapAsync(_ => Unit.Value);
```

**Benefits:**
- Zero memory overhead (empty struct)
- Enables consistent Result<T, E> signatures for operations without meaningful return values
- Replaces `void` in monadic chains for better composability

**API:**
```csharp
public readonly struct Unit
{
    public static readonly Unit Value = new();
}
```

## What's Changed
- Added `Unit` struct to support functional result types
- NO BREAKING CHANGE INTRODUCED. The library is fully backwards compatible.

Happy coding!