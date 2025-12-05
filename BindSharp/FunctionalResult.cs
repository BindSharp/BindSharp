using System;

namespace BindSharp;

/// <summary>
/// Provides functional extension methods for working with <see cref="Result{T, TError}"/> types.
/// Enables railway-oriented programming patterns for composing operations.
/// </summary>
public static class FunctionalResult
{
    /// <summary>
    /// Transforms the success value of a result using the specified mapping function.
    /// If the result is a failure, the error is propagated unchanged.
    /// </summary>
    /// <typeparam name="T1">The type of the input success value.</typeparam>
    /// <typeparam name="T2">The type of the output success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="result">The result to transform.</param>
    /// <param name="map">The function to apply to the success value.</param>
    /// <returns>
    /// A new result with the transformed success value if successful,
    /// or the original error if failed.
    /// </returns>
    /// <example>
    /// <code>
    /// var result = Result&lt;int, string&gt;.Success(5);
    /// var doubled = result.Map(x => x * 2); // Success(10)
    /// </code>
    /// </example>
    public static Result<T2, TError> Map<T1, T2, TError>
        (this Result<T1, TError> result, Func<T1, T2> map) =>
        result.IsSuccess
            ? Result<T2, TError>.Success(map(result.Value))
            : Result<T2, TError>.Failure(result.Error);
    
    /// <summary>
    /// Chains a result-producing operation to the current result.
    /// Also known as FlatMap or SelectMany. This prevents nested Result types.
    /// If the result is a failure, the error is propagated without executing the bind function.
    /// </summary>
    /// <typeparam name="T1">The type of the input success value.</typeparam>
    /// <typeparam name="T2">The type of the output success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="result">The result to bind.</param>
    /// <param name="bind">The function that produces a new result from the success value.</param>
    /// <returns>
    /// The result produced by the bind function if the input was successful,
    /// or the original error if failed.
    /// </returns>
    /// <example>
    /// <code>
    /// var result = Result&lt;int, string&gt;.Success(5);
    /// var validated = result.Bind(x => 
    ///     x > 0 ? Result&lt;int, string&gt;.Success(x) 
    ///           : Result&lt;int, string&gt;.Failure("Must be positive"));
    /// </code>
    /// </example>
    public static Result<T2, TError> Bind<T1, T2, TError>
        (this Result<T1, TError> result, Func<T1, Result<T2, TError>> bind) =>
        result.IsSuccess ? bind(result.Value) : Result<T2, TError>.Failure(result.Error);
    
    /// <summary>
    /// Transforms the error value of a result using the specified mapping function.
    /// If the result is successful, the success value is propagated unchanged.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the input error value.</typeparam>
    /// <typeparam name="TNewError">The type of the output error value.</typeparam>
    /// <param name="result">The result to transform.</param>
    /// <param name="map">The function to apply to the error value.</param>
    /// <returns>
    /// The original success value if successful,
    /// or a new result with the transformed error if failed.
    /// </returns>
    /// <example>
    /// <code>
    /// var result = Result&lt;int, string&gt;.Failure("404");
    /// var mapped = result.MapError(int.Parse); // Failure(404 as int)
    /// </code>
    /// </example>
    public static Result<T, TNewError> MapError<T, TError, TNewError>
        (this Result<T, TError> result, Func<TError, TNewError> map) =>
        result.IsSuccess
            ? Result<T, TNewError>.Success(result.Value)
            : Result<T, TNewError>.Failure(map(result.Error));
    
    /// <summary>
    /// Matches the result and extracts a value by applying one of two functions
    /// depending on whether the result is successful or failed.
    /// This is the primary way to safely extract values from a result.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TResult">The type of the value to return.</typeparam>
    /// <param name="result">The result to match.</param>
    /// <param name="mapValue">The function to apply if the result is successful.</param>
    /// <param name="mapError">The function to apply if the result is failed.</param>
    /// <returns>
    /// The value produced by either <paramref name="mapValue"/> or <paramref name="mapError"/>.
    /// </returns>
    /// <example>
    /// <code>
    /// var result = Result&lt;int, string&gt;.Success(42);
    /// var message = result.Match(
    ///     value => $"Success: {value}",
    ///     error => $"Error: {error}"
    /// ); // "Success: 42"
    /// </code>
    /// </example>
    public static TResult Match<T, TError, TResult>
        (this Result<T, TError> result, Func<T, TResult> mapValue, Func<TError, TResult> mapError) =>
        result.IsSuccess ? mapValue(result.Value) : mapError(result.Error);
    
    /// <summary>
    /// Conditionally applies a continuation function based on a predicate.
    /// If the predicate returns true, the continuation function is applied.
    /// If the predicate returns false, the original result is returned unchanged (short-circuit).
    /// This enables conditional processing in functional pipelines.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="result">The result to evaluate.</param>
    /// <param name="predicate">The condition to check against the success value.</param>
    /// <param name="continuation">The function to apply if the predicate returns true.</param>
    /// <returns>
    /// The result of the continuation function if the predicate returns true,
    /// the original result unchanged if the predicate returns false,
    /// or the original error if the result was already failed.
    /// </returns>
    /// <example>
    /// <code>
    /// // Process data only if it needs processing
    /// var result = GetData()
    ///     .BindIf(
    ///         data => data.NeedsProcessing,
    ///         data => ProcessData(data)
    ///     );
    /// 
    /// // Multiple conditional steps
    /// var validated = GetUser()
    ///     .BindIf(
    ///         user => !user.IsComplete,
    ///         user => EnrichFromDatabase(user)  // Only enrich incomplete users
    ///     );
    /// </code>
    /// </example>
    public static Result<T, TError> BindIf<T, TError>(
        this Result<T, TError> result,
        Func<T, bool> predicate,
        Func<T, Result<T, TError>> continuation)
    {
        if (result.IsFailure) return result;
    
        return predicate(result.Value) 
            ? continuation(result.Value)
            : result;
    }
}