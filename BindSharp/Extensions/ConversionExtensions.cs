using System.Threading.Tasks;

namespace BindSharp.Extensions;

/// <summary>
/// Provides conversion extension methods for <see cref="BindSharp.Result{T, TError}"/> types.
/// Enables converting between sync/async Results and handling nullable values.
/// </summary>
public static class ConversionExtensions
{
    /// <summary>
    /// Converts a nullable reference type to a Result.
    /// Useful for converting nullable return values from external APIs into the Result type.
    /// </summary>
    /// <typeparam name="T">The reference type.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="value">The nullable value to convert.</param>
    /// <param name="error">The error to use if the value is null.</param>
    /// <returns>
    /// A successful result if the value is not null,
    /// otherwise a failure result with the specified error.
    /// </returns>
    /// <example>
    /// <code>
    /// string? maybeUser = GetUserFromCache();
    /// var result = maybeUser.ToResult("User not found in cache");
    /// </code>
    /// </example>
    public static Result<T, TError> ToResult<T, TError>(
        this T? value,
        TError error) where T : class
    {
        return value is not null
            ? Result<T, TError>.Success(value)
            : Result<T, TError>.Failure(error);
    }

    /// <summary>
    /// Converts a synchronous Result to a Task-wrapped Result.
    /// Useful when you need to match signatures in async pipelines.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <returns>A completed task containing the result.</returns>
    /// <example>
    /// <code>
    /// Result&lt;int, string&gt; syncResult = Result&lt;int, string&gt;.Success(42);
    /// Task&lt;Result&lt;int, string&gt;&gt; asyncResult = syncResult.AsTask();
    /// </code>
    /// </example>
    public static Task<Result<T, TError>> AsTask<T, TError>(this Result<T, TError> result) =>
        Task.FromResult(result);
}