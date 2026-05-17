using System.Diagnostics.CodeAnalysis;

namespace ViajantesTurismo.Common.Results;

/// <summary>
/// Represents an optional value that may or may not be present.
/// Used to avoid nullable generic Result types while maintaining type safety.
/// </summary>
/// <typeparam name="T">The type of the optional value.</typeparam>
[SuppressMessage("Design", "CA1000:Do not declare static members on generic types",
    Justification = "Option<T> exposes static factory methods for discoverability and fluent usage; external factory would reduce ergonomics and clarity.")]
public readonly struct Option<T> : IEquatable<Option<T>> where T : class
{
    private readonly T? _value;

    private Option(T? value, bool hasValue)
    {
        _value = value;
        HasValue = hasValue;
    }

    /// <summary>
    /// Gets a value indicating whether a value is present.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(true, nameof(_value))]
    public bool HasValue { get; }

    /// <summary>
    /// Gets the value if present, otherwise null.
    /// </summary>
    public T? Value => _value;

    /// <summary>
    /// Creates an Option with a value.
    /// </summary>
    /// <param name="value">The value to wrap.</param>
    /// <returns>An Option containing the value.</returns>
    public static Option<T> Of(T value) => new(value, true);

    /// <summary>
    /// Creates an empty Option.
    /// </summary>
    /// <returns>An empty Option.</returns>
    public static Option<T> Empty() => new(null, false);

    /// <summary>
    /// Creates an Option from a nullable value.
    /// </summary>
    /// <param name="value">The nullable value.</param>
    /// <returns>An Option containing the value if not null, otherwise empty.</returns>
    public static Option<T> FromNullable(T? value) =>
        value is null ? Empty() : Of(value);

    /// <inheritdoc />
    public bool Equals(Option<T> other) =>
        HasValue == other.HasValue && EqualityComparer<T?>.Default.Equals(_value, other._value);

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is Option<T> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(HasValue, _value);

    /// <summary>
    /// Determines whether two Option instances are equal.
    /// </summary>
    public static bool operator ==(Option<T> left, Option<T> right) =>
        left.Equals(right);

    /// <summary>
    /// Determines whether two Option instances are not equal.
    /// </summary>
    public static bool operator !=(Option<T> left, Option<T> right) =>
        !left.Equals(right);
}
