using System;
using System.Collections.Generic;

namespace BindSharp;

/// <summary>
/// Represents the result of an operation that can either succeed with a value or fail with an error.
/// This type enables railway-oriented programming by explicitly handling success and failure cases.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
/// <typeparam name="TError">The type of the error value.</typeparam>
/// <example>
/// <code>
/// // Success case
/// var success = Result&lt;int, string&gt;.Success(42);
/// 
/// // Failure case
/// var failure = Result&lt;int, string&gt;.Failure("Something went wrong");
/// </code>
/// </example>
public sealed class Result<T, TError> : IEquatable<Result<T, TError>>
{
    private T? _value;
    private TError? _error;
    
    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }
    
    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;
    
    /// <summary>
    /// Gets the success value if the operation was successful.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when attempting to access the value of a failed result.
    /// Use <see cref="IsSuccess"/> to check before accessing.
    /// </exception>
    public T Value
    {
        get => IsSuccess ? _value!: throw new InvalidOperationException("Result is not successful");
        private set => _value = value;
    }
    
    /// <summary>
    /// Gets the error value if the operation failed.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when attempting to access the error of a successful result.
    /// Use <see cref="IsFailure"/> to check before accessing.
    /// </exception>
    public TError Error
    {
        get => !IsSuccess ? _error! : throw new InvalidOperationException("Result is successful");
        private set => _error = value;
    }
    
    private Result(bool isSuccess, T? value, TError? error) => 
        (IsSuccess, Value, Error) = (isSuccess, value, error);
    
    /// <summary>
    /// Creates a successful result with the specified value.
    /// </summary>
    /// <param name="value">The success value.</param>
    /// <returns>A successful <see cref="Result{T, TError}"/> containing the value.</returns>
    public static Result<T, TError> Success(T value) => new(true, value, default);
    
    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    /// <param name="error">The error value.</param>
    /// <returns>A failed <see cref="Result{T, TError}"/> containing the error.</returns>
    public static Result<T, TError> Failure(TError error) => new(false, default, error);
    
    #region Implicit Conversions
    
    /// <summary>
    /// Implicitly converts a value to a successful result.
    /// Enables cleaner syntax: <c>return value;</c> instead of <c>return Result.Success(value);</c>
    /// </summary>
    /// <param name="value">The success value.</param>
    /// <returns>A successful result containing the value.</returns>
    /// <example>
    /// <code>
    /// // Before: Explicit
    /// public Result&lt;int, string&gt; Divide(int a, int b)
    /// {
    ///     if (b == 0) return Result&lt;int, string&gt;.Failure("Division by zero");
    ///     return Result&lt;int, string&gt;.Success(a / b);
    /// }
    /// 
    /// // After: Implicit (cleaner!)
    /// public Result&lt;int, string&gt; Divide(int a, int b)
    /// {
    ///     if (b == 0) return "Division by zero";
    ///     return a / b;
    /// }
    /// </code>
    /// </example>
    public static implicit operator Result<T, TError>(T value) => Success(value);
    
    /// <summary>
    /// Implicitly converts an error to a failed result.
    /// Enables cleaner syntax: <c>return error;</c> instead of <c>return Result.Failure(error);</c>
    /// </summary>
    /// <param name="error">The error value.</param>
    /// <returns>A failed result containing the error.</returns>
    /// <example>
    /// <code>
    /// // Before: Explicit
    /// public Result&lt;User, string&gt; GetUser(int id)
    /// {
    ///     if (id &lt; 0) return Result&lt;User, string&gt;.Failure("Invalid ID");
    ///     var user = FindUser(id);
    ///     return Result&lt;User, string&gt;.Success(user);
    /// }
    /// 
    /// // After: Implicit (cleaner!)
    /// public Result&lt;User, string&gt; GetUser(int id)
    /// {
    ///     if (id &lt; 0) return "Invalid ID";
    ///     var user = FindUser(id);
    ///     return user;
    /// }
    /// </code>
    /// </example>
    public static implicit operator Result<T, TError>(TError error) => Failure(error);
    
    #endregion
    
    #region Equality
    
    /// <summary>
    /// Determines whether the current result is equal to another result.
    /// Two results are equal if they are both successful with equal values,
    /// or both failed with equal errors.
    /// </summary>
    /// <param name="other">The result to compare with.</param>
    /// <returns>true if the results are equal; otherwise, false.</returns>
    public bool Equals(Result<T, TError>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        
        if (IsSuccess != other.IsSuccess) return false;
        
        return IsSuccess 
            ? EqualityComparer<T>.Default.Equals(Value, other.Value)
            : EqualityComparer<TError>.Default.Equals(Error, other.Error);
    }
    
    /// <summary>
    /// Determines whether the current result is equal to another object.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>true if the object is a Result and is equal; otherwise, false.</returns>
    public override bool Equals(object? obj) => Equals(obj as Result<T, TError>);
    
    /// <summary>
    /// Returns the hash code for this result.
    /// </summary>
    /// <returns>A hash code for the current result.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 23 + IsSuccess.GetHashCode();
            
            if (IsSuccess)
            {
                hash = hash * 23 + (Value?.GetHashCode() ?? 0);
            }
            else
            {
                hash = hash * 23 + (Error?.GetHashCode() ?? 0);
            }
            
            return hash;
        }
    }
    
    /// <summary>
    /// Determines whether two results are equal.
    /// </summary>
    /// <param name="left">The first result to compare.</param>
    /// <param name="right">The second result to compare.</param>
    /// <returns>true if the results are equal; otherwise, false.</returns>
    public static bool operator ==(Result<T, TError>? left, Result<T, TError>? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }
    
    /// <summary>
    /// Determines whether two results are not equal.
    /// </summary>
    /// <param name="left">The first result to compare.</param>
    /// <param name="right">The second result to compare.</param>
    /// <returns>true if the results are not equal; otherwise, false.</returns>
    public static bool operator !=(Result<T, TError>? left, Result<T, TError>? right)
    {
        return !(left == right);
    }
    
    /// <summary>
    /// Returns a string representation of this result.
    /// </summary>
    /// <returns>A string that represents the current result.</returns>
    public override string ToString()
    {
        return IsSuccess 
            ? $"Success({Value})" 
            : $"Failure({Error})";
    }
    
    #endregion
}