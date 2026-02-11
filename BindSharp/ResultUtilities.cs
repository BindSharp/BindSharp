// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Threading.Tasks;

namespace BindSharp;

/// <summary>
/// Provides static factory and utility methods for creating <see cref="Result{T, TError}"/> instances.
/// Primary use case is converting exception-based operations into functional Results using Try/TryAsync.
/// </summary>
/// <remarks>
/// <para>
/// Use <see cref="Result"/> for static utilities that create Results from operations that may throw exceptions.
/// This enables wrapping exception-based APIs in a functional style with full try-catch-finally support.
/// </para>
/// <example>
/// <code>
/// // Exception handling with custom error
/// var parsed = Result.Try(
///     () => int.Parse(input),
///     ex => $"Invalid number: {ex.Message}"
/// );
/// 
/// // With finally block for cleanup
/// var result = Result.Try(
///     () => ProcessData(),
///     ex => $"Processing failed: {ex.Message}",
///     finally: () => CleanupResources()
/// );
/// 
/// // Exception-first (for logging then transforming)
/// var data = Result.Try(() => File.ReadAllText("file.txt"))
///     .TapError(ex => _logger.LogError(ex, "Read failed"))
///     .MapError(ex => "Failed to read file");
/// 
/// // Async with finally
/// var response = await Result.TryAsync(
///     async () => await httpClient.GetStringAsync(url),
///     ex => $"HTTP error: {ex.Message}",
///     finally: async () => await LogAttemptAsync()
/// );
/// </code>
/// </example>
/// </remarks>
public static class Result
{
    /// <summary>
    /// Executes code that may throw exceptions and converts it to a Result.
    /// This provides a functional way to handle exception-based APIs with optional finally block support.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="operation">The operation that may throw an exception.</param>
    /// <param name="errorFactory">A function that converts an exception to an error value.</param>
    /// <param name="@finally">Optional action that always executes, regardless of success or failure.</param>
    /// <returns>
    /// A successful result containing the operation's return value, 
    /// or a failure result containing the converted exception.
    /// </returns>
    /// <example>
    /// <code>
    /// var result = Result.Try(
    ///     () => int.Parse("42"),
    ///     ex => $"Parse failed: {ex.Message}"
    /// ); // Success(42)
    /// 
    /// // With cleanup
    /// var result = Result.Try(
    ///     () => {
    ///         AcquireLock();
    ///         return ProcessData();
    ///     },
    ///     ex => $"Failed: {ex.Message}",
    ///     finally: () => ReleaseLock()  // Always runs
    /// );
    /// </code>
    /// </example>
    public static Result<T, TError> Try<T, TError>(
        Func<T> operation,
        Func<Exception, TError> errorFactory,
        Action? @finally = null)
    {
        try
        {
            return Result<T, TError>.Success(operation());
        }
        catch (Exception ex)
        {
            return Result<T, TError>.Failure(errorFactory(ex));
        }
        finally
        {
            @finally?.Invoke();
        }
    }

    /// <summary>
    /// Asynchronously executes code that may throw exceptions and converts it to a Result.
    /// This provides a functional way to handle exception-based async APIs with optional finally block support.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="operation">The async operation that may throw an exception.</param>
    /// <param name="errorFactory">A function that converts an exception to an error value.</param>
    /// <param name="@finally">Optional async action that always executes, regardless of success or failure.</param>
    /// <returns>
    /// A task containing a successful result with the operation's return value,
    /// or a failure result containing the converted exception.
    /// </returns>
    /// <example>
    /// <code>
    /// var result = await Result.TryAsync(
    ///     async () => await httpClient.GetStringAsync("https://api.example.com"),
    ///     ex => $"HTTP request failed: {ex.Message}",
    ///     finally: async () => await LogRequestAttemptAsync()
    /// );
    /// </code>
    /// </example>
    public static async Task<Result<T, TError>> TryAsync<T, TError>(
        Func<Task<T>> operation,
        Func<Exception, TError> errorFactory,
        Func<Task>? @finally = null)
    {
        try
        {
            return Result<T, TError>.Success(await operation());
        }
        catch (Exception ex)
        {
            return Result<T, TError>.Failure(errorFactory(ex));
        }
        finally
        {
            if (@finally is not null)
                await @finally();
        }
    }
    
    /// <summary>
    /// Executes code that may throw exceptions and converts it to a Result with the exception as the error.
    /// This overload is useful when you want to inspect or log the exception before transforming it to a custom error type.
    /// Use <see cref="SideEffectExtensions.TapError{T,TError}"/> to log the exception, then <see cref="FunctionalResult.MapError{T,TError,TNewError}"/> to convert to your error type.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="operation">The operation that may throw an exception.</param>
    /// <param name="@finally">Optional action that always executes, regardless of success or failure.</param>
    /// <returns>
    /// A successful result containing the operation's return value, 
    /// or a failure result containing the exception.
    /// </returns>
    /// <example>
    /// <code>
    /// // Log exception with full context before transforming
    /// var result = Result.Try(
    ///         () => int.Parse("invalid"),
    ///         finally: () => _metrics.RecordAttempt())
    ///     .TapError(ex => _logger.LogError(ex, "Parse failed"))
    ///     .MapError(ex => $"Invalid number: {ex.Message}");
    /// 
    /// // Pattern match on specific exception types with cleanup
    /// var result = Result.Try(
    ///         () => {
    ///             AcquireLock();
    ///             return File.ReadAllText("file.txt");
    ///         },
    ///         finally: () => ReleaseLock())
    ///     .TapError(ex => {
    ///         if (ex is FileNotFoundException fnf)
    ///             _logger.LogWarning("File missing: {0}", fnf.FileName);
    ///         else
    ///             _logger.LogError(ex, "Read failed");
    ///     })
    ///     .MapError(ex => ex switch {
    ///         FileNotFoundException => "File not found",
    ///         UnauthorizedAccessException => "Permission denied",
    ///         _ => "Failed to read file"
    ///     });
    /// </code>
    /// </example>
    public static Result<T, Exception> Try<T>(
        Func<T> operation,
        Action? @finally = null)
    {
        try
        {
            return Result<T, Exception>.Success(operation());
        }
        catch (Exception ex)
        {
            return Result<T, Exception>.Failure(ex);
        }
        finally
        {
            @finally?.Invoke();
        }
    }

    /// <summary>
    /// Asynchronously executes code that may throw exceptions and converts it to a Result with the exception as the error.
    /// This overload is useful when you want to inspect or log the exception before transforming it to a custom error type.
    /// Use <see cref="SideEffectExtensions.TapErrorAsync{T,TError}"/> to log the exception, then <see cref="AsyncFunctionalResult.MapErrorAsync{T,TError,TNewError}"/> to convert to your error type.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="operation">The async operation that may throw an exception.</param>
    /// <param name="@finally">Optional async action that always executes, regardless of success or failure.</param>
    /// <returns>
    /// A task containing a successful result with the operation's return value,
    /// or a failure result containing the exception.
    /// </returns>
    /// <example>
    /// <code>
    /// // Log exception with full context before transforming
    /// var result = await Result.TryAsync(
    ///         async () => await httpClient.GetStringAsync("https://api.example.com"),
    ///         finally: async () => await RecordMetricsAsync())
    ///     .TapErrorAsync(async ex => await _logger.LogErrorAsync(ex, "HTTP request failed"))
    ///     .MapErrorAsync(ex => $"API error: {ex.Message}");
    /// 
    /// // Async pattern matching with guaranteed cleanup
    /// var result = await Result.TryAsync(
    ///         async () => {
    ///             await AcquireLockAsync();
    ///             return await database.QueryAsync("SELECT * FROM users");
    ///         },
    ///         finally: async () => await ReleaseLockAsync())
    ///     .TapErrorAsync(async ex => {
    ///         if (ex is TimeoutException timeout)
    ///             await _metrics.RecordTimeoutAsync();
    ///         else
    ///             await _logger.LogErrorAsync(ex, "Query failed");
    ///     })
    ///     .MapErrorAsync(ex => ex switch {
    ///         TimeoutException => "Database timeout",
    ///         SqlException => "Database error",
    ///         _ => "Query failed"
    ///     });
    /// </code>
    /// </example>
    public static async Task<Result<T, Exception>> TryAsync<T>(
        Func<Task<T>> operation,
        Func<Task>? @finally = null)
    {
        try
        {
            return Result<T, Exception>.Success(await operation());
        }
        catch (Exception ex)
        {
            return Result<T, Exception>.Failure(ex);
        }
        finally
        {
            if (@finally is not null)
                await @finally();
        }
    }
}