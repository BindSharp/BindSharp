// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Threading.Tasks;

namespace BindSharp;

/// <summary>
/// Provides asynchronous extension methods for working with <see cref="Result{T, TError}"/> types.
/// Enables composing async operations in a railway-oriented programming style.
/// </summary>
public static class AsyncFunctionalResult
{
    /// <summary>
    /// Transforms the success value of an async result using a synchronous mapping function.
    /// Use this when you have a Task&lt;Result&gt; and want to apply a sync transformation.
    /// </summary>
    /// <typeparam name="T1">The type of the input success value.</typeparam>
    /// <typeparam name="T2">The type of the output success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="result">The async result to transform.</param>
    /// <param name="map">The synchronous function to apply to the success value.</param>
    /// <returns>A task containing the transformed result.</returns>
    /// <example>
    /// <code>
    /// Task&lt;Result&lt;int, string&gt;&gt; asyncResult = GetValueAsync();
    /// var doubled = await asyncResult.MapAsync(x => x * 2);
    /// </code>
    /// </example>
    public static async Task<Result<T2, TError>> MapAsync<T1, T2, TError>
        (this Task<Result<T1, TError>> result, Func<T1, T2> map) =>
        (await result).Map(map);
    
    /// <summary>
    /// Transforms the success value of a result using an asynchronous mapping function.
    /// Use this when you have a Result and want to apply an async transformation.
    /// </summary>
    /// <typeparam name="T1">The type of the input success value.</typeparam>
    /// <typeparam name="T2">The type of the output success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="result">The result to transform.</param>
    /// <param name="mapAsync">The asynchronous function to apply to the success value.</param>
    /// <returns>A task containing the transformed result.</returns>
    /// <example>
    /// <code>
    /// var result = Result&lt;int, string&gt;.Success(5);
    /// var fetched = await result.MapAsync(async id => await FetchUserAsync(id));
    /// </code>
    /// </example>
    public static async Task<Result<T2, TError>> MapAsync<T1, T2, TError>
        (this Result<T1, TError> result, Func<T1, Task<T2>> mapAsync) =>
        result.IsSuccess
            ? Result<T2, TError>.Success(await mapAsync(result.Value))
            : Result<T2, TError>.Failure(result.Error);
    
    /// <summary>
    /// Transforms the success value of an async result using an asynchronous mapping function.
    /// Use this when you have a Task&lt;Result&gt; and want to apply an async transformation.
    /// </summary>
    /// <typeparam name="T1">The type of the input success value.</typeparam>
    /// <typeparam name="T2">The type of the output success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="result">The async result to transform.</param>
    /// <param name="mapAsync">The asynchronous function to apply to the success value.</param>
    /// <returns>A task containing the transformed result.</returns>
    /// <example>
    /// <code>
    /// Task&lt;Result&lt;int, string&gt;&gt; asyncResult = GetIdAsync();
    /// var user = await asyncResult.MapAsync(async id => await FetchUserAsync(id));
    /// </code>
    /// </example>
    public static async Task<Result<T2, TError>> MapAsync<T1, T2, TError>
        (this Task<Result<T1, TError>> result, Func<T1, Task<T2>> mapAsync) =>
        await (await result).MapAsync(mapAsync);

    /// <summary>
    /// Chains a synchronous result-producing operation to an async result.
    /// Use this when you have a Task&lt;Result&gt; and want to bind with a sync function.
    /// </summary>
    /// <typeparam name="T1">The type of the input success value.</typeparam>
    /// <typeparam name="T2">The type of the output success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="result">The async result to bind.</param>
    /// <param name="bind">The synchronous function that produces a new result.</param>
    /// <returns>A task containing the bound result.</returns>
    /// <example>
    /// <code>
    /// Task&lt;Result&lt;int, string&gt;&gt; asyncResult = GetValueAsync();
    /// var validated = await asyncResult.BindAsync(x => 
    ///     x > 0 ? Result&lt;int, string&gt;.Success(x) 
    ///           : Result&lt;int, string&gt;.Failure("Must be positive"));
    /// </code>
    /// </example>
    public static async Task<Result<T2, TError>> BindAsync<T1, T2, TError>
        (this Task<Result<T1, TError>> result, Func<T1, Result<T2, TError>> bind) =>
        (await result).Bind(bind);
    
    /// <summary>
    /// Chains an asynchronous result-producing operation to a result.
    /// Use this when you have a Result and want to bind with an async function.
    /// </summary>
    /// <typeparam name="T1">The type of the input success value.</typeparam>
    /// <typeparam name="T2">The type of the output success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="result">The result to bind.</param>
    /// <param name="bindAsync">The asynchronous function that produces a new result.</param>
    /// <returns>A task containing the bound result.</returns>
    /// <example>
    /// <code>
    /// var result = Result&lt;int, string&gt;.Success(5);
    /// var saved = await result.BindAsync(async x => await SaveToDbAsync(x));
    /// </code>
    /// </example>
    public static async Task<Result<T2, TError>> BindAsync<T1, T2, TError>
        (this Result<T1, TError> result, Func<T1, Task<Result<T2, TError>>> bindAsync) =>
        result.IsSuccess
            ? await bindAsync(result.Value)
            : Result<T2, TError>.Failure(result.Error);
    
    /// <summary>
    /// Chains an asynchronous result-producing operation to an async result.
    /// Use this when you have a Task&lt;Result&gt; and want to bind with an async function.
    /// </summary>
    /// <typeparam name="T1">The type of the input success value.</typeparam>
    /// <typeparam name="T2">The type of the output success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="result">The async result to bind.</param>
    /// <param name="bindAsync">The asynchronous function that produces a new result.</param>
    /// <returns>A task containing the bound result.</returns>
    /// <example>
    /// <code>
    /// Task&lt;Result&lt;int, string&gt;&gt; asyncResult = GetIdAsync();
    /// var processed = await asyncResult.BindAsync(async id => await ProcessAsync(id));
    /// </code>
    /// </example>
    public static async Task<Result<T2, TError>> BindAsync<T1, T2, TError>
        (this Task<Result<T1, TError>> result, Func<T1, Task<Result<T2, TError>>> bindAsync) =>
        await (await result).BindAsync(bindAsync);

    /// <summary>
    /// Transforms the error value of an async result using a synchronous mapping function.
    /// Use this when you have a Task&lt;Result&gt; and want to transform errors synchronously.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the input error value.</typeparam>
    /// <typeparam name="TNewError">The type of the output error value.</typeparam>
    /// <param name="result">The async result to transform.</param>
    /// <param name="map">The synchronous function to apply to the error value.</param>
    /// <returns>A task containing the result with transformed error.</returns>
    public static async Task<Result<T, TNewError>> MapErrorAsync<T, TError, TNewError>
        (this Task<Result<T, TError>> result, Func<TError, TNewError> map) =>
        (await result).MapError(map);
    
    /// <summary>
    /// Transforms the error value of a result using an asynchronous mapping function.
    /// Use this when you have a Result and want to transform errors asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the input error value.</typeparam>
    /// <typeparam name="TNewError">The type of the output error value.</typeparam>
    /// <param name="result">The result to transform.</param>
    /// <param name="mapAsync">The asynchronous function to apply to the error value.</param>
    /// <returns>A task containing the result with transformed error.</returns>
    /// <example>
    /// <code>
    /// var result = Result&lt;int, string&gt;.Failure("Error code: 404");
    /// var logged = await result.MapErrorAsync(async err => await LogErrorAsync(err));
    /// </code>
    /// </example>
    public static async Task<Result<T, TNewError>> MapErrorAsync<T, TError, TNewError>
        (this Result<T, TError> result, Func<TError, Task<TNewError>> mapAsync) =>
        result.IsSuccess
            ? Result<T, TNewError>.Success(result.Value)
            : Result<T, TNewError>.Failure(await mapAsync(result.Error));
    
    /// <summary>
    /// Transforms the error value of an async result using an asynchronous mapping function.
    /// Use this when you have a Task&lt;Result&gt; and want to transform errors asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the input error value.</typeparam>
    /// <typeparam name="TNewError">The type of the output error value.</typeparam>
    /// <param name="result">The async result to transform.</param>
    /// <param name="mapAsync">The asynchronous function to apply to the error value.</param>
    /// <returns>A task containing the result with transformed error.</returns>
    public static async Task<Result<T, TNewError>> MapErrorAsync<T, TError, TNewError>
        (this Task<Result<T, TError>> result, Func<TError, Task<TNewError>> mapAsync) =>
        await (await result).MapErrorAsync(mapAsync);
    
    /// <summary>
    /// Matches an async result using synchronous handler functions.
    /// Use this when you have a Task&lt;Result&gt; and want to handle both cases synchronously.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TResult">The type of the value to return.</typeparam>
    /// <param name="result">The async result to match.</param>
    /// <param name="onSuccess">The synchronous function to apply if successful.</param>
    /// <param name="onFailure">The synchronous function to apply if failed.</param>
    /// <returns>A task containing the matched value.</returns>
    public static async Task<TResult> MatchAsync<T, TError, TResult>
        (this Task<Result<T, TError>> result, Func<T, TResult> onSuccess, Func<TError, TResult> onFailure) =>
        (await result).Match(onSuccess, onFailure);
        
    /// <summary>
    /// Matches a result with an asynchronous success handler and synchronous failure handler.
    /// Use this when success requires async processing but failure doesn't.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TResult">The type of the value to return.</typeparam>
    /// <param name="result">The result to match.</param>
    /// <param name="onSuccessAsync">The asynchronous function to apply if successful.</param>
    /// <param name="onFailure">The synchronous function to apply if failed.</param>
    /// <returns>A task containing the matched value.</returns>
    /// <example>
    /// <code>
    /// var result = Result&lt;int, string&gt;.Success(42);
    /// var output = await result.MatchAsync(
    ///     async value => await FormatSuccessAsync(value),
    ///     error => $"Error: {error}"
    /// );
    /// </code>
    /// </example>
    public static async Task<TResult> MatchAsync<T, TError, TResult>
        (this Result<T, TError> result, Func<T, Task<TResult>> onSuccessAsync, Func<TError, TResult> onFailure) =>
        result.IsSuccess
            ?  await onSuccessAsync(result.Value)
            : onFailure(result.Error);
    
    /// <summary>
    /// Matches a result with a synchronous success handler and asynchronous failure handler.
    /// Use this when failure requires async processing but success doesn't.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TResult">The type of the value to return.</typeparam>
    /// <param name="result">The result to match.</param>
    /// <param name="onSuccess">The synchronous function to apply if successful.</param>
    /// <param name="onFailureAsync">The asynchronous function to apply if failed.</param>
    /// <returns>A task containing the matched value.</returns>
    public static async Task<TResult> MatchAsync<T, TError, TResult>
        (this Result<T, TError> result, Func<T, TResult> onSuccess, Func<TError, Task<TResult>> onFailureAsync) =>
        result.IsSuccess
            ? onSuccess(result.Value)
            : await onFailureAsync(result.Error);
    
    /// <summary>
    /// Matches an async result with an asynchronous success handler and synchronous failure handler.
    /// Use this when you have a Task&lt;Result&gt; and success requires async processing.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TResult">The type of the value to return.</typeparam>
    /// <param name="result">The async result to match.</param>
    /// <param name="onSuccessAsync">The asynchronous function to apply if successful.</param>
    /// <param name="onFailure">The synchronous function to apply if failed.</param>
    /// <returns>A task containing the matched value.</returns>
    public static async Task<TResult> MatchAsync<T, TError, TResult>
        (this Task<Result<T, TError>> result, Func<T, Task<TResult>> onSuccessAsync, Func<TError, TResult> onFailure) =>
        await (await result).MatchAsync(onSuccessAsync, onFailure);
    
    /// <summary>
    /// Matches an async result with a synchronous success handler and asynchronous failure handler.
    /// Use this when you have a Task&lt;Result&gt; and failure requires async processing.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TResult">The type of the value to return.</typeparam>
    /// <param name="result">The async result to match.</param>
    /// <param name="onSuccess">The synchronous function to apply if successful.</param>
    /// <param name="onFailureAsync">The asynchronous function to apply if failed.</param>
    /// <returns>A task containing the matched value.</returns>
    public static async Task<TResult> MatchAsync<T, TError, TResult>
        (this Task<Result<T, TError>> result, Func<T, TResult> onSuccess, Func<TError, Task<TResult>> onFailureAsync) =>
        await (await result).MatchAsync(onSuccess, onFailureAsync);
    
    /// <summary>
    /// Matches a result using asynchronous handler functions for both success and failure.
    /// Use this when both success and failure cases require async processing.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TResult">The type of the value to return.</typeparam>
    /// <param name="result">The result to match.</param>
    /// <param name="onSuccessAsync">The asynchronous function to apply if successful.</param>
    /// <param name="onFailureAsync">The asynchronous function to apply if failed.</param>
    /// <returns>A task containing the matched value.</returns>
    /// <example>
    /// <code>
    /// var result = Result&lt;int, string&gt;.Success(42);
    /// var saved = await result.MatchAsync(
    ///     async value => await SaveSuccessAsync(value),
    ///     async error => await LogErrorAsync(error)
    /// );
    /// </code>
    /// </example>
    public static async Task<TResult> MatchAsync<T, TError, TResult>
        (this Result<T, TError> result, Func<T, Task<TResult>> onSuccessAsync, Func<TError, Task<TResult>> onFailureAsync) =>
        result.IsSuccess
            ? await onSuccessAsync(result.Value)
            : await onFailureAsync(result.Error);
    
    /// <summary>
    /// Matches an async result using asynchronous handler functions for both success and failure.
    /// Use this when you have a Task&lt;Result&gt; and both cases require async processing.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TResult">The type of the value to return.</typeparam>
    /// <param name="result">The async result to match.</param>
    /// <param name="onSuccessAsync">The asynchronous function to apply if successful.</param>
    /// <param name="onFailureAsync">The asynchronous function to apply if failed.</param>
    /// <returns>A task containing the matched value.</returns>
    public static async Task<TResult> MatchAsync<T, TError, TResult>
        (this Task<Result<T, TError>> result, Func<T, Task<TResult>> onSuccessAsync, Func<TError, Task<TResult>> onFailureAsync) =>
        await (await result).MatchAsync(onSuccessAsync, onFailureAsync);
    
    #region BindIf - Conditional Processing

    /// <summary>
    /// Conditionally applies a continuation function based on a predicate.
    /// Evaluates an async result with synchronous predicate and continuation.
    /// Use this when you have a Task&lt;Result&gt; and want to apply conditional logic synchronously.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="result">The async result to evaluate.</param>
    /// <param name="predicate">The condition to check against the success value.</param>
    /// <param name="continuation">The function to apply if the predicate returns true.</param>
    /// <returns>
    /// A task containing the result of the continuation if the predicate returns true,
    /// the original result if the predicate returns false,
    /// or the original error if the result was already failed.
    /// </returns>
    /// <example>
    /// <code>
    /// var result = await FetchDataAsync()
    ///     .BindIfAsync(
    ///         data => data.RequiresValidation,
    ///         data => ValidateData(data)
    ///     );
    /// </code>
    /// </example>
    public static async Task<Result<T, TError>> BindIfAsync<T, TError>(
        this Task<Result<T, TError>> result,
        Func<T, bool> predicate,
        Func<T, Result<T, TError>> continuation) =>
        (await result).BindIf(predicate, continuation);

    /// <summary>
    /// Conditionally applies an asynchronous continuation function based on a predicate.
    /// Evaluates a result with synchronous predicate and asynchronous continuation.
    /// Use this when you have a Result and the continuation requires async processing.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="result">The result to evaluate.</param>
    /// <param name="predicate">The condition to check against the success value.</param>
    /// <param name="continuationAsync">The async function to apply if the predicate returns true.</param>
    /// <returns>
    /// A task containing the result of the continuation if the predicate returns true,
    /// the original result if the predicate returns false,
    /// or the original error if the result was already failed.
    /// </returns>
    /// <example>
    /// <code>
    /// var user = Result&lt;User, string&gt;.Success(currentUser);
    /// var enriched = await user.BindIfAsync(
    ///     u => !u.IsComplete,
    ///     async u => await FetchAdditionalDataAsync(u)
    /// );
    /// </code>
    /// </example>
    public static async Task<Result<T, TError>> BindIfAsync<T, TError>(
        this Result<T, TError> result,
        Func<T, bool> predicate,
        Func<T, Task<Result<T, TError>>> continuationAsync) =>
        result.IsFailure
            ? result
            : predicate(result.Value)
                ? await continuationAsync(result.Value)
                : result;

    /// <summary>
    /// Conditionally applies an asynchronous continuation function based on a predicate.
    /// Evaluates an async result with synchronous predicate and asynchronous continuation.
    /// Use this when you have a Task&lt;Result&gt; and the continuation requires async processing.
    /// This is the most common async pattern for conditional processing.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="result">The async result to evaluate.</param>
    /// <param name="predicate">The condition to check against the success value.</param>
    /// <param name="continuationAsync">The async function to apply if the predicate returns true.</param>
    /// <returns>
    /// A task containing the result of the continuation if the predicate returns true,
    /// the original result if the predicate returns false,
    /// or the original error if the result was already failed.
    /// </returns>
    /// <example>
    /// <code>
    /// // Complete async pipeline with conditional processing
    /// var result = await FetchDataAsync()
    ///     .MapAsync(data => NormalizeData(data))
    ///     .BindIfAsync(
    ///         data => data.RequiresEnrichment,
    ///         async data => await EnrichDataAsync(data)
    ///     )
    ///     .BindAsync(async data => await SaveAsync(data));
    /// </code>
    /// </example>
    public static async Task<Result<T, TError>> BindIfAsync<T, TError>(
        this Task<Result<T, TError>> result,
        Func<T, bool> predicate,
        Func<T, Task<Result<T, TError>>> continuationAsync) =>
        await (await result).BindIfAsync(predicate, continuationAsync);

    /// <summary>
    /// Conditionally applies a continuation function based on an asynchronous predicate.
    /// Evaluates a result with async predicate and synchronous continuation.
    /// Use this when the condition check itself requires async operations (e.g., database lookup)
    /// but the continuation can be handled synchronously.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="result">The result to evaluate.</param>
    /// <param name="predicateAsync">The async condition to check against the success value.</param>
    /// <param name="continuation">The synchronous function to apply if the predicate returns true.</param>
    /// <returns>
    /// A task containing the result of the continuation if the predicate returns true,
    /// the original result if the predicate returns false,
    /// or the original error if the result was already failed.
    /// </returns>
    /// <example>
    /// <code>
    /// var user = Result&lt;User, string&gt;.Success(currentUser);
    /// var processed = await user.BindIfAsync(
    ///     async u => await RequiresUpdateAsync(u.Id),
    ///     u => UpdateUser(u)  // Sync update
    /// );
    /// </code>
    /// </example>
    public static async Task<Result<T, TError>> BindIfAsync<T, TError>(
        this Result<T, TError> result,
        Func<T, Task<bool>> predicateAsync,
        Func<T, Result<T, TError>> continuation) =>
        result.IsFailure
            ? result
            : await predicateAsync(result.Value)
                ? continuation(result.Value)
                : result;

    /// <summary>
    /// Conditionally applies a continuation function based on an asynchronous predicate.
    /// Evaluates an async result with async predicate and synchronous continuation.
    /// Use this when you have a Task&lt;Result&gt; and the condition check requires async operations
    /// but the continuation can be handled synchronously.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="result">The async result to evaluate.</param>
    /// <param name="predicateAsync">The async condition to check against the success value.</param>
    /// <param name="continuation">The synchronous function to apply if the predicate returns true.</param>
    /// <returns>
    /// A task containing the result of the continuation if the predicate returns true,
    /// the original result if the predicate returns false,
    /// or the original error if the result was already failed.
    /// </returns>
    /// <example>
    /// <code>
    /// var result = await FetchUserAsync()
    ///     .BindIfAsync(
    ///         async u => await HasPermissionAsync(u.Id, "admin"),
    ///         u => CreateAdminView(u)
    ///     );
    /// </code>
    /// </example>
    public static async Task<Result<T, TError>> BindIfAsync<T, TError>(
        this Task<Result<T, TError>> result,
        Func<T, Task<bool>> predicateAsync,
        Func<T, Result<T, TError>> continuation) =>
        await (await result).BindIfAsync(predicateAsync, continuation);

    /// <summary>
    /// Conditionally applies an asynchronous continuation function based on an asynchronous predicate.
    /// Evaluates a result with async predicate and async continuation.
    /// Use this when the condition check itself requires async operations (e.g., database lookup)
    /// and the continuation also requires async processing.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="result">The result to evaluate.</param>
    /// <param name="predicateAsync">The async condition to check against the success value.</param>
    /// <param name="continuationAsync">The async function to apply if the predicate returns true.</param>
    /// <returns>
    /// A task containing the result of the continuation if the predicate returns true,
    /// the original result if the predicate returns false,
    /// or the original error if the result was already failed.
    /// </returns>
    /// <example>
    /// <code>
    /// var user = Result&lt;User, string&gt;.Success(currentUser);
    /// var enriched = await user.BindIfAsync(
    ///     async u => await RequiresEnrichmentAsync(u.Id),
    ///     async u => await FetchAdditionalDataAsync(u)
    /// );
    /// </code>
    /// </example>
    public static async Task<Result<T, TError>> BindIfAsync<T, TError>(
        this Result<T, TError> result,
        Func<T, Task<bool>> predicateAsync,
        Func<T, Task<Result<T, TError>>> continuationAsync) =>
        result.IsFailure
            ? result
            : await predicateAsync(result.Value)
                ? await continuationAsync(result.Value)
                : result;

    /// <summary>
    /// Conditionally applies an asynchronous continuation function based on an asynchronous predicate.
    /// Evaluates an async result with async predicate and async continuation.
    /// Use this when you have a Task&lt;Result&gt; and both the condition check and continuation
    /// require async operations. This is the most flexible async pattern for conditional processing.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="result">The async result to evaluate.</param>
    /// <param name="predicateAsync">The async condition to check against the success value.</param>
    /// <param name="continuationAsync">The async function to apply if the predicate returns true.</param>
    /// <returns>
    /// A task containing the result of the continuation if the predicate returns true,
    /// the original result if the predicate returns false,
    /// or the original error if the result was already failed.
    /// </returns>
    /// <example>
    /// <code>
    /// // Complete async pipeline with async conditional check
    /// var result = await FetchUserAsync()
    ///     .MapAsync(async u => await NormalizeUserAsync(u))
    ///     .BindIfAsync(
    ///         async u => await RequiresEnrichmentAsync(u.Id),
    ///         async u => await EnrichFromExternalApiAsync(u)
    ///     )
    ///     .BindAsync(async u => await SaveUserAsync(u));
    /// </code>
    /// </example>
    public static async Task<Result<T, TError>> BindIfAsync<T, TError>(
        this Task<Result<T, TError>> result,
        Func<T, Task<bool>> predicateAsync,
        Func<T, Task<Result<T, TError>>> continuationAsync) =>
        await (await result).BindIfAsync(predicateAsync, continuationAsync);

    #endregion
}