using System;
using System.Threading.Tasks;

namespace BindSharp;

/// <summary>
/// Provides additional utility extension methods for working with <see cref="Result{T, TError}"/> types.
/// Includes exception handling, validation, side effects, and resource management patterns.
/// </summary>
public static class ResultExtensions
{
    #region Exception Handling

    /// <summary>
    /// Executes code that may throw exceptions and converts it to a Result.
    /// This provides a functional way to handle exception-based APIs.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="operation">The operation that may throw an exception.</param>
    /// <param name="errorFactory">A function that converts an exception to an error value.</param>
    /// <returns>
    /// A successful result containing the operation's return value, 
    /// or a failure result containing the converted exception.
    /// </returns>
    /// <example>
    /// <code>
    /// var result = ResultExtensions.Try(
    ///     () => int.Parse("42"),
    ///     ex => $"Parse failed: {ex.Message}"
    /// ); // Success(42)
    /// 
    /// var failed = ResultExtensions.Try(
    ///     () => int.Parse("invalid"),
    ///     ex => $"Parse failed: {ex.Message}"
    /// ); // Failure("Parse failed: ...")
    /// </code>
    /// </example>
    public static Result<T, TError> Try<T, TError>(
        Func<T> operation,
        Func<Exception, TError> errorFactory)
    {
        try
        {
            return Result<T, TError>.Success(operation());
        }
        catch (Exception ex)
        {
            return Result<T, TError>.Failure(errorFactory(ex));
        }
    }

    /// <summary>
    /// Asynchronously executes code that may throw exceptions and converts it to a Result.
    /// This provides a functional way to handle exception-based async APIs.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="operation">The async operation that may throw an exception.</param>
    /// <param name="errorFactory">A function that converts an exception to an error value.</param>
    /// <returns>
    /// A task containing a successful result with the operation's return value,
    /// or a failure result containing the converted exception.
    /// </returns>
    /// <example>
    /// <code>
    /// var result = await ResultExtensions.TryAsync(
    ///     async () => await httpClient.GetStringAsync("https://api.example.com"),
    ///     ex => $"HTTP request failed: {ex.Message}"
    /// );
    /// </code>
    /// </example>
    public static async Task<Result<T, TError>> TryAsync<T, TError>(
        Func<Task<T>> operation,
        Func<Exception, TError> errorFactory)
    {
        try
        {
            return Result<T, TError>.Success(await operation());
        }
        catch (Exception ex)
        {
            return Result<T, TError>.Failure(errorFactory(ex));
        }
    }

    #endregion

    #region Validation

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

    #endregion

    #region Nullable Conversion

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

    #endregion

    #region Side Effects (Tap)

    /// <summary>
    /// Executes a synchronous side effect on a successful result's value without modifying the result.
    /// Useful for logging, debugging, or triggering side effects in a functional pipeline.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="result">The result to tap into.</param>
    /// <param name="action">The side effect to execute on the success value.</param>
    /// <returns>The original result unchanged.</returns>
    /// <example>
    /// <code>
    /// var result = Result&lt;int, string&gt;.Success(42)
    ///     .Tap(x => Console.WriteLine($"Value: {x}"))
    ///     .Map(x => x * 2);
    /// // Prints "Value: 42" and returns Success(84)
    /// </code>
    /// </example>
    public static Result<T, TError> Tap<T, TError>(
        this Result<T, TError> result,
        Action<T> action)
    {
        if (result.IsSuccess)
            action(result.Value);

        return result;
    }

    /// <summary>
    /// Executes an asynchronous side effect on a successful result's value without modifying the result.
    /// Useful for async logging, database operations, or triggering async side effects in a functional pipeline.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="result">The result to tap into.</param>
    /// <param name="action">The async side effect to execute on the success value.</param>
    /// <returns>A task containing the original result unchanged.</returns>
    /// <example>
    /// <code>
    /// var result = await Result&lt;User, string&gt;.Success(user)
    ///     .TapAsync(async u => await LogUserActivityAsync(u))
    ///     .MapAsync(u => u.Id);
    /// </code>
    /// </example>
    public static async Task<Result<T, TError>> TapAsync<T, TError>(
        this Result<T, TError> result,
        Func<T, Task> action)
    {
        if (result.IsSuccess)
            await action(result.Value);

        return result;
    }

    /// <summary>
    /// Executes an asynchronous side effect on a successful async result's value without modifying the result.
    /// Useful for async logging, database operations, or triggering async side effects in a functional pipeline.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resultTask">The async result to tap into.</param>
    /// <param name="action">The async side effect to execute on the success value.</param>
    /// <returns>A task containing the original result unchanged.</returns>
    /// <example>
    /// <code>
    /// var result = await GetUserAsync()
    ///     .TapAsync(async u => await LogUserActivityAsync(u))
    ///     .MapAsync(u => u.Id);
    /// </code>
    /// </example>
    public static async Task<Result<T, TError>> TapAsync<T, TError>(
        this Task<Result<T, TError>> resultTask,
        Func<T, Task> action)
    {
        Result<T, TError> result = await resultTask;

        if (result.IsSuccess)
            await action(result.Value);

        return result;
    }
    
    /// <summary>
    /// Executes a synchronous side effect on a failed result's error without modifying the result.
    /// Useful for logging errors, recording metrics, or triggering error-handling side effects in a functional pipeline.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="result">The result to tap into.</param>
    /// <param name="action">The side effect to execute on the error value.</param>
    /// <returns>The original result unchanged.</returns>
    /// <example>
    /// <code>
    /// var result = Result&lt;int, string&gt;.Failure("Invalid input")
    ///     .TapError(err => Console.WriteLine($"Error: {err}"))
    ///     .MapError(err => $"Processing failed: {err}");
    /// // Prints "Error: Invalid input" and returns Failure("Processing failed: Invalid input")
    /// </code>
    /// </example>
    public static Result<T, TError> TapError<T, TError>(
        this Result<T, TError> result,
        Action<TError> action)
    {
        if (result.IsFailure)
            action(result.Error);

        return result;
    }

    /// <summary>
    /// Executes an asynchronous side effect on a failed result's error without modifying the result.
    /// Useful for async error logging, sending error notifications, or recording async error metrics in a functional pipeline.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="result">The result to tap into.</param>
    /// <param name="action">The async side effect to execute on the error value.</param>
    /// <returns>A task containing the original result unchanged.</returns>
    /// <example>
    /// <code>
    /// var result = await Result&lt;User, string&gt;.Failure("Database connection failed")
    ///     .TapErrorAsync(async err => await LogErrorToExternalServiceAsync(err))
    ///     .MapErrorAsync(async err => await EnrichErrorWithContextAsync(err));
    /// </code>
    /// </example>
    public static async Task<Result<T, TError>> TapErrorAsync<T, TError>(
        this Result<T, TError> result,
        Func<TError, Task> action)
    {
        if (result.IsFailure)
            await action(result.Error);

        return result;
    }

    /// <summary>
    /// Executes an asynchronous side effect on a failed async result's error without modifying the result.
    /// Useful for async error logging, sending error notifications, or recording async error metrics in a functional pipeline.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resultTask">The async result to tap into.</param>
    /// <param name="action">The async side effect to execute on the error value.</param>
    /// <returns>A task containing the original result unchanged.</returns>
    /// <example>
    /// <code>
    /// var result = await GetUserAsync()
    ///     .TapErrorAsync(async err => await _logger.LogErrorAsync(err))
    ///     .TapErrorAsync(async err => await _metrics.RecordErrorAsync(err));
    /// </code>
    /// </example>
    public static async Task<Result<T, TError>> TapErrorAsync<T, TError>(
        this Task<Result<T, TError>> resultTask,
        Func<TError, Task> action)
    {
        Result<T, TError> result = await resultTask;

        if (result.IsFailure)
            await action(result.Error);

        return result;
    }

    #endregion

    #region Resource Management

    /// <summary>
    /// Executes an operation on an IDisposable resource and guarantees its disposal,
    /// even if the operation fails. This implements the "bracket" pattern from functional programming.
    /// </summary>
    /// <typeparam name="TResource">The type of the disposable resource.</typeparam>
    /// <typeparam name="TResult">The type of the operation result value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resource">The result containing the resource to use.</param>
    /// <param name="operation">The operation to perform with the resource.</param>
    /// <returns>
    /// The result of the operation if the resource was successfully acquired,
    /// otherwise the original resource acquisition error.
    /// The resource is guaranteed to be disposed.
    /// </returns>
    /// <example>
    /// <code>
    /// var result = OpenFile("data.txt")
    ///     .Using(stream => ReadData(stream));
    /// // stream is automatically disposed
    /// </code>
    /// </example>
    public static Result<TResult, TError> Using<TResource, TResult, TError>(
        this Result<TResource, TError> resource,
        Func<TResource, Result<TResult, TError>> operation)
        where TResource : IDisposable
    {
        if (resource.IsFailure)
            return Result<TResult, TError>.Failure(resource.Error);

        try
        {
            return operation(resource.Value);
        }
        finally
        {
            resource.Value.Dispose();
        }
    }

    /// <summary>
    /// Asynchronously executes an operation on an IDisposable resource and guarantees its disposal,
    /// even if the operation fails. This implements the "bracket" pattern from functional programming.
    /// </summary>
    /// <typeparam name="TResource">The type of the disposable resource.</typeparam>
    /// <typeparam name="TResult">The type of the operation result value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resource">The result containing the resource to use.</param>
    /// <param name="operation">The async operation to perform with the resource.</param>
    /// <returns>
    /// A task containing the result of the operation if the resource was successfully acquired,
    /// otherwise the original resource acquisition error.
    /// The resource is guaranteed to be disposed.
    /// </returns>
    /// <example>
    /// <code>
    /// var result = await OpenFileAsync("data.txt")
    ///     .UsingAsync(async stream => await ReadDataAsync(stream));
    /// // stream is automatically disposed
    /// </code>
    /// </example>
    public static async Task<Result<TResult, TError>> UsingAsync<TResource, TResult, TError>(
        this Result<TResource, TError> resource,
        Func<TResource, Task<Result<TResult, TError>>> operation)
        where TResource : IDisposable
    {
        if (resource.IsFailure)
            return Result<TResult, TError>.Failure(resource.Error);

        try
        {
            return await operation(resource.Value);
        }
        finally
        {
            resource.Value.Dispose();
        }
    }

    #endregion

    #region Conversion

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

    #endregion
    
    #region Resource Management (Task-based)
    
    /// <summary>
    /// Asynchronously executes an operation on an IDisposable resource from an async result
    /// and guarantees its disposal, even if the operation fails.
    /// </summary>
    /// <typeparam name="TResource">The type of the disposable resource.</typeparam>
    /// <typeparam name="TResult">The type of the operation result value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resourceTask">The async result containing the resource to use.</param>
    /// <param name="operation">The async operation to perform with the resource.</param>
    /// <returns>
    /// A task containing the result of the operation if the resource was successfully acquired,
    /// otherwise the original resource acquisition error.
    /// The resource is guaranteed to be disposed.
    /// </returns>
    /// <example>
    /// <code>
    /// var result = await OpenFileAsync("data.txt")
    ///     .UsingAsync(async stream => await ProcessDataAsync(stream));
    /// // stream is automatically disposed
    /// </code>
    /// </example>
    public static async Task<Result<TResult, TError>> UsingAsync<TResource, TResult, TError>(
        this Task<Result<TResource, TError>> resourceTask,
        Func<TResource, Task<Result<TResult, TError>>> operation)
        where TResource : IDisposable
    {
        var resource = await resourceTask;
        return await resource.UsingAsync(operation);
    }

    #endregion
}