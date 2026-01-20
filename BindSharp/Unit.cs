// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

namespace BindSharp;

/// <summary>
/// Represents the absence of a meaningful value in functional programming patterns.
/// Use <see cref="Unit"/> as the success type in <see cref="Result{T, TError}"/> when an operation
/// succeeds but produces no value (similar to void, but composable).
/// </summary>
/// <remarks>
/// <para>
/// The Unit type is a zero-sized struct with a single value, making it ideal for representing
/// "no value" without any memory overhead. It enables consistent <see cref="Result{T, TError}"/>
/// signatures throughout your codebase, even for operations that don't produce meaningful output.
/// </para>
/// <para>
/// Use Unit for operations like database inserts/updates/deletes, sending notifications,
/// validation checks, or any operation where success/failure matters but the return value doesn't.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Instead of void or bool, use Result&lt;Unit, TError&gt;
/// public async Task&lt;Result&lt;Unit, string&gt;&gt; DeleteUserAsync(int id) =>
///     await ResultExtensions.TryAsync(
///         async () => {
///             await _repository.DeleteAsync(id);
///             return Unit.Value;
///         },
///         ex => $"Delete failed: {ex.Message}"
///     );
///
/// // Composable in functional chains
/// public async Task&lt;Result&lt;Unit, string&gt;&gt; ProcessOrderAsync(Order order) =>
///     await ValidateOrder(order)
///         .BindAsync(o => SaveOrderAsync(o))      // Result&lt;Unit, string&gt;
///         .BindAsync(_ => UpdateInventoryAsync()) // Result&lt;Unit, string&gt;
///         .TapAsync(_ => NotifyCustomerAsync());  // Result&lt;Unit, string&gt;
/// </code>
/// </example>
public readonly struct Unit
{
    /// <summary>
    /// Gets the singleton instance of <see cref="Unit"/>.
    /// Use this value to represent successful operations with no meaningful return value.
    /// </summary>
    /// <remarks>
    /// This is the only instance of Unit you'll ever need. Since Unit carries no data,
    /// all instances are semantically equivalent.
    /// </remarks>
    public static readonly Unit Value = new();
}