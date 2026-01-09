using System;
using System.Threading.Tasks;

namespace BindSharp.Extensions;

/// <summary>
/// Provides side effect extension methods for <see cref="BindSharp.Result{T, TError}"/> types.
/// Enables executing logging, metrics, and other side effects without modifying the Result.
/// </summary>
public static class SideEffectExtensions
{
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
    /// Executes a synchronous side effect on a successful async result's value without modifying the result.
    /// Use this when you have a Task&lt;Result&gt; and want to perform sync side effects (like simple logging).
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resultTask">The async result to tap into.</param>
    /// <param name="action">The synchronous side effect to execute on the success value.</param>
    /// <returns>A task containing the original result unchanged.</returns>
    /// <example>
    /// <code>
    /// var result = await GetUserAsync()
    ///     .TapAsync(u => Console.WriteLine($"User: {u.Name}"))  // Sync action
    ///     .MapAsync(u => u.Id);
    /// </code>
    /// </example>
    public static async Task<Result<T, TError>> TapAsync<T, TError>(
        this Task<Result<T, TError>> resultTask,
        Action<T> action)
    {
        Result<T, TError> result = await resultTask;

        if (result.IsSuccess)
            action(result.Value);

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
    
    /// <summary>
    /// Executes a synchronous side effect on a failed async result's error without modifying the result.
    /// Use this when you have a Task&lt;Result&gt; and want to perform sync error handling (like simple logging).
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resultTask">The async result to tap into.</param>
    /// <param name="action">The synchronous side effect to execute on the error value.</param>
    /// <returns>A task containing the original result unchanged.</returns>
    /// <example>
    /// <code>
    /// var result = await GetUserAsync()
    ///     .TapErrorAsync(err => Console.WriteLine($"Error: {err}"))  // Sync action
    ///     .MapErrorAsync(err => $"Failed: {err}");
    /// </code>
    /// </example>
    public static async Task<Result<T, TError>> TapErrorAsync<T, TError>(
        this Task<Result<T, TError>> resultTask,
        Action<TError> action)
    {
        Result<T, TError> result = await resultTask;

        if (result.IsFailure)
            action(result.Error);

        return result;
    }

    /// <summary>
    /// Executes one of two side effects based on the result state, returning the result unchanged.
    /// Use this when you need to handle both success and failure cases with side effects
    /// while keeping the result composable.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="onSuccess">The side effect to execute if the result is successful.</param>
    /// <param name="onFailure">The side effect to execute if the result is failed.</param>
    /// <returns>The original result unchanged.</returns>
    /// <example>
    /// <code>
    /// // At the end of a pipeline (consuming)
    /// ValidateData(input)
    ///     .Bind(data => ProcessData(data))
    ///     .Do(
    ///         success => Console.WriteLine($"Processed: {success}"),
    ///         error => Console.WriteLine($"Error: {error}")
    ///     );
    /// 
    /// // In the middle of a pipeline (observing)
    /// var result = ValidateData(input)
    ///     .Do(
    ///         data => _logger.LogInfo("Validation passed"),
    ///         error => _logger.LogError(error)
    ///     )
    ///     .Bind(data => ProcessData(data))
    ///     .Map(data => FormatOutput(data));
    /// 
    /// // Compare to Match (which extracts a value and breaks the chain)
    /// var message = result.Match(
    ///     success => $"Success: {success}",
    ///     error => $"Error: {error}"
    /// ); // Can't continue the chain after Match
    /// </code>
    /// </example>
    public static Result<T, TError> Do<T, TError>(
        this Result<T, TError> result,
        Action<T> onSuccess,
        Action<TError> onFailure)
    {
        if (result.IsSuccess)
            onSuccess(result.Value);
        else
            onFailure(result.Error);

        return result;
    }

    /// <summary>
    /// Executes one of two synchronous side effects on an async result based on its state,
    /// returning the result unchanged.
    /// Use this when you have a Task&lt;Result&gt; and want to perform simple side effects
    /// like logging without async operations.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resultTask">The async result to inspect.</param>
    /// <param name="onSuccess">The synchronous side effect to execute if successful.</param>
    /// <param name="onFailure">The synchronous side effect to execute if failed.</param>
    /// <returns>A task containing the original result unchanged.</returns>
    /// <example>
    /// <code>
    /// var result = await FetchDataAsync()
    ///     .Do(
    ///         data => Console.WriteLine($"Fetched: {data}"),
    ///         error => Console.WriteLine($"Error: {error}")
    ///     )
    ///     .MapAsync(data => TransformData(data));
    /// </code>
    /// </example>
    public static async Task<Result<T, TError>> DoAsync<T, TError>(
        this Task<Result<T, TError>> resultTask,
        Action<T> onSuccess,
        Action<TError> onFailure)
    {
        var result = await resultTask;
        return result.Do(onSuccess, onFailure);
    }

    /// <summary>
    /// Executes one of two asynchronous side effects on a result based on its state,
    /// returning the result unchanged.
    /// Use this when you have a Result and need to perform async side effects
    /// like database logging or API calls.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="onSuccessAsync">The async side effect to execute if successful.</param>
    /// <param name="onFailureAsync">The async side effect to execute if failed.</param>
    /// <returns>A task containing the original result unchanged.</returns>
    /// <example>
    /// <code>
    /// var result = await ValidateData(input)
    ///     .DoAsync(
    ///         async data => await _logger.LogInfoAsync("Validation passed"),
    ///         async error => await _logger.LogErrorAsync(error)
    ///     )
    ///     .BindAsync(async data => await ProcessDataAsync(data));
    /// </code>
    /// </example>
    public static async Task<Result<T, TError>> DoAsync<T, TError>(
        this Result<T, TError> result,
        Func<T, Task> onSuccessAsync,
        Func<TError, Task> onFailureAsync)
    {
        if (result.IsSuccess)
            await onSuccessAsync(result.Value);
        else
            await onFailureAsync(result.Error);

        return result;
    }

    /// <summary>
    /// Executes one of two asynchronous side effects on an async result based on its state,
    /// returning the result unchanged.
    /// Use this when you have a Task&lt;Result&gt; and need to perform async side effects.
    /// This is the most flexible async overload.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resultTask">The async result to inspect.</param>
    /// <param name="onSuccessAsync">The async side effect to execute if successful.</param>
    /// <param name="onFailureAsync">The async side effect to execute if failed.</param>
    /// <returns>A task containing the original result unchanged.</returns>
    /// <example>
    /// <code>
    /// var result = await FetchDataAsync()
    ///     .DoAsync(
    ///         async data => await _logger.LogInfoAsync($"Fetched: {data}"),
    ///         async error => await _alerting.NotifyAsync(error)
    ///     )
    ///     .MapAsync(async data => await TransformDataAsync(data));
    /// </code>
    /// </example>
    public static async Task<Result<T, TError>> DoAsync<T, TError>(
        this Task<Result<T, TError>> resultTask,
        Func<T, Task> onSuccessAsync,
        Func<TError, Task> onFailureAsync)
    {
        var result = await resultTask;
        return await result.DoAsync(onSuccessAsync, onFailureAsync);
    }

    /// <summary>
    /// Executes an async side effect on success and a sync side effect on failure,
    /// returning the result unchanged.
    /// Use this when success handling requires async operations but failure handling doesn't.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="onSuccessAsync">The async side effect to execute if successful.</param>
    /// <param name="onFailure">The synchronous side effect to execute if failed.</param>
    /// <returns>A task containing the original result unchanged.</returns>
    /// <example>
    /// <code>
    /// var result = await ValidateData(input)
    ///     .DoAsync(
    ///         async data => await _db.LogSuccessAsync(data),
    ///         error => Console.WriteLine($"Validation error: {error}")
    ///     )
    ///     .BindAsync(async data => await ProcessDataAsync(data));
    /// </code>
    /// </example>
    public static async Task<Result<T, TError>> DoAsync<T, TError>(
        this Result<T, TError> result,
        Func<T, Task> onSuccessAsync,
        Action<TError> onFailure)
    {
        if (result.IsSuccess)
            await onSuccessAsync(result.Value);
        else
            onFailure(result.Error);

        return result;
    }

    /// <summary>
    /// Executes an async side effect on success and a sync side effect on failure
    /// for an async result, returning the result unchanged.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resultTask">The async result to inspect.</param>
    /// <param name="onSuccessAsync">The async side effect to execute if successful.</param>
    /// <param name="onFailure">The synchronous side effect to execute if failed.</param>
    /// <returns>A task containing the original result unchanged.</returns>
    public static async Task<Result<T, TError>> DoAsync<T, TError>(
        this Task<Result<T, TError>> resultTask,
        Func<T, Task> onSuccessAsync,
        Action<TError> onFailure)
    {
        var result = await resultTask;
        return await result.DoAsync(onSuccessAsync, onFailure);
    }

    /// <summary>
    /// Executes a sync side effect on success and an async side effect on failure,
    /// returning the result unchanged.
    /// Use this when failure handling requires async operations but success handling doesn't.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="onSuccess">The synchronous side effect to execute if successful.</param>
    /// <param name="onFailureAsync">The async side effect to execute if failed.</param>
    /// <returns>A task containing the original result unchanged.</returns>
    /// <example>
    /// <code>
    /// var result = await ValidateData(input)
    ///     .DoAsync(
    ///         data => Console.WriteLine($"Valid: {data}"),
    ///         async error => await _alerting.NotifyAdminAsync(error)
    ///     )
    ///     .BindAsync(async data => await ProcessDataAsync(data));
    /// </code>
    /// </example>
    public static async Task<Result<T, TError>> DoAsync<T, TError>(
        this Result<T, TError> result,
        Action<T> onSuccess,
        Func<TError, Task> onFailureAsync)
    {
        if (result.IsSuccess)
            onSuccess(result.Value);
        else
            await onFailureAsync(result.Error);

        return result;
    }

    /// <summary>
    /// Executes a sync side effect on success and an async side effect on failure
    /// for an async result, returning the result unchanged.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="resultTask">The async result to inspect.</param>
    /// <param name="onSuccess">The synchronous side effect to execute if successful.</param>
    /// <param name="onFailureAsync">The async side effect to execute if failed.</param>
    /// <returns>A task containing the original result unchanged.</returns>
    public static async Task<Result<T, TError>> DoAsync<T, TError>(
        this Task<Result<T, TError>> resultTask,
        Action<T> onSuccess,
        Func<TError, Task> onFailureAsync)
    {
        var result = await resultTask;
        return await result.DoAsync(onSuccess, onFailureAsync);
    }
}