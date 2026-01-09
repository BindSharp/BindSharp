using System;
using System.Threading.Tasks;

namespace BindSharp.Extensions;

/// <summary>
/// Provides resource management extension methods for <see cref="BindSharp.Result{T, TError}"/> types.
/// Implements the "bracket" pattern for safe IDisposable resource handling in functional pipelines.
/// </summary>
public static class ResourceManagementExtensions
{
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
}