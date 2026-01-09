using System;
using System.Threading.Tasks;

namespace BindSharp.Extensions;

/// <summary>
/// Provides validation extension methods for <see cref="BindSharp.Result{T, TError}"/> types.
/// Enables adding validation rules and null checks to functional pipelines.
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// Validates a condition on a successful result's value.
    /// If the result is already failed or the condition is not met, returns a failure.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="result">The result to validate.</param>
    /// <param name="predicate">The validation condition to check.</param>
    /// <param name="error">The error to return if validation fails.</param>
    /// <returns>
    /// The original result if successful and the predicate returns true,
    /// otherwise a failure result with the specified error.
    /// </returns>
    /// <example>
    /// <code>
    /// var result = Result&lt;int, string&gt;.Success(5);
    /// var validated = result.Ensure(
    ///     x => x > 0,
    ///     "Value must be positive"
    /// ); // Success(5)
    /// 
    /// var invalid = result.Ensure(
    ///     x => x > 10,
    ///     "Value must be greater than 10"
    /// ); // Failure("Value must be greater than 10")
    /// </code>
    /// </example>
    public static Result<T, TError> Ensure<T, TError>(
        this Result<T, TError> result,
        Func<T, bool> predicate,
        TError error)
    {
        return result.IsSuccess && predicate(result.Value)
            ? result
            : Result<T, TError>.Failure(error);
    }

    /// <summary>
    /// Asynchronously validates a condition on a successful result's value.
    /// If the result is already failed or the condition is not met, returns a failure.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="result">The async result to validate.</param>
    /// <param name="predicate">The validation condition to check.</param>
    /// <param name="error">The error to return if validation fails.</param>
    /// <returns>
    /// A task containing the original result if successful and the predicate returns true,
    /// otherwise a failure result with the specified error.
    /// </returns>
    public static async Task<Result<T, TError>> EnsureAsync<T, TError>(
        this Task<Result<T, TError>> result,
        Func<T, bool> predicate,
        TError error)
    {
        return (await result).Ensure(predicate, error);
    }

    /// <summary>
    /// Ensures that a nullable reference type result contains a non-null value.
    /// Converts a Result with nullable value to a Result with non-nullable value.
    /// </summary>
    /// <typeparam name="T">The reference type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="result">The result with a potentially null value.</param>
    /// <param name="errorWhenNull">The error to return if the value is null.</param>
    /// <returns>
    /// A result with a non-nullable value if successful and non-null,
    /// otherwise a failure result.
    /// </returns>
    /// <example>
    /// <code>
    /// Result&lt;string?, string&gt; maybeNull = GetUser();
    /// Result&lt;string, string&gt; ensured = maybeNull.EnsureNotNull("User not found");
    /// </code>
    /// </example>
    public static Result<T, TError> EnsureNotNull<T, TError>(
        this Result<T?, TError> result,
        TError errorWhenNull) where T : class
    {
        return result.Bind(value =>
            value is not null
                ? Result<T, TError>.Success(value)
                : Result<T, TError>.Failure(errorWhenNull)
        );
    }

    /// <summary>
    /// Asynchronously ensures that a nullable reference type result contains a non-null value.
    /// Converts a Task of Result with nullable value to a Result with non-nullable value.
    /// </summary>
    /// <typeparam name="T">The reference type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="result">The async result with a potentially null value.</param>
    /// <param name="errorWhenNull">The error to return if the value is null.</param>
    /// <returns>
    /// A task containing a result with a non-nullable value if successful and non-null,
    /// otherwise a failure result.
    /// </returns>
    public static Task<Result<T, TError>> EnsureNotNullAsync<T, TError>(
        this Task<Result<T?, TError>> result,
        TError errorWhenNull) where T : class
    {
        return result.BindAsync(value =>
            value is not null
                ? Task.FromResult(Result<T, TError>.Success(value))
                : Task.FromResult(Result<T, TError>.Failure(errorWhenNull))
        );
    }
}