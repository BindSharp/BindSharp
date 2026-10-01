# BindSharp 2.1.1 Release Notes

## Fixed: `Ensure` / `EnsureAsync` no longer overwrite an upstream failure

### The problem

```csharp
Result<int, string>.Failure("db down").Ensure(x => x > 0, "must be positive");
// 2.1.0 → Failure("must be positive")   ← the real cause is lost
// 2.1.1 → Failure("db down")
```

`Ensure` returned its own error whenever the result was not a passing success — including when the result had *already failed*. In a pipeline such as

```csharp
repository.UpdateAsync(...)
    .MapErrorAsync(e => new DataAccessError(e))
    .EnsureAsync(affected => affected > 0, new InvalidTransitionError())
```

a database failure came out as `InvalidTransitionError`, hiding the `DataAccessError`. `Map`, `Bind`, `Tap` and `EnsureNotNull` already passed failures through; `Ensure` was the odd one out.

### The fix

- An already-failed result is returned unchanged; its error is preserved.
- The predicate is **not evaluated** for a failed result.
- Successful results behave exactly as before (predicate true → unchanged, false → `Failure(error)`).

### Upgrade notes — behavior change

This is a bug fix, but it changes observable behavior. Code that (knowingly or not) relied on `Ensure` replacing an earlier failure will now see the original error. Review pipelines where `Ensure`/`EnsureAsync` follows a step that can fail — especially if your error-to-HTTP (or similar) mapping depends on which error type comes out. To keep the old behavior explicitly, map the error after the failure instead, e.g. `.MapError(_ => yourError)`.

No API signatures changed.
