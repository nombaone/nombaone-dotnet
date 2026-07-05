using System;

namespace NombaOne;

/// <summary>
/// A three-state value for the handful of request fields the API lets you clear
/// by sending an explicit JSON <c>null</c> (for example a customer's
/// <c>phone</c>). Declare such a field as <c>Optional&lt;T&gt;?</c>:
/// <list type="bullet">
///   <item><description><c>null</c> (the property left unset) — the field is omitted, leaving the value unchanged;</description></item>
///   <item><description><c>value</c> (assigned directly, via the implicit conversion) — the field is sent with that value;</description></item>
///   <item><description><see cref="Null"/> — the field is sent as JSON <c>null</c>, clearing it.</description></item>
/// </list>
/// </summary>
/// <typeparam name="T">The underlying value type.</typeparam>
/// <example>
/// <code>
/// // set a value:
/// await nombaone.Customers.UpdateAsync(id, new CustomerUpdateParams { Phone = "+2348012345678" });
/// // clear it:
/// await nombaone.Customers.UpdateAsync(id, new CustomerUpdateParams { Phone = Optional&lt;string&gt;.Null });
/// // leave it unchanged: simply don't set Phone.
/// </code>
/// </example>
public readonly struct Optional<T>
{
    private readonly T _value;

    private Optional(T value, bool isNull)
    {
        _value = value;
        IsNull = isNull;
    }

    /// <summary>Whether this value serializes to JSON <c>null</c> (clears the field).</summary>
    public bool IsNull { get; }

    /// <summary>The wrapped value (meaningful only when <see cref="IsNull"/> is false).</summary>
    public T Value => _value;

    /// <summary>Wrap a concrete value to send.</summary>
    /// <param name="value">The value to send.</param>
    public static Optional<T> Of(T value) => new(value, isNull: false);

    /// <summary>A value that serializes to JSON <c>null</c>, clearing the field.</summary>
    public static Optional<T> Null => new(default!, isNull: true);

    /// <summary>Implicitly wrap a concrete value, so <c>Field = value</c> just works.</summary>
    /// <param name="value">The value to send.</param>
    public static implicit operator Optional<T>(T value) => Of(value);
}
