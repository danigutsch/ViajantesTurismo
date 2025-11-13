using System.Diagnostics.CodeAnalysis;

namespace ViajantesTurismo.Common.Results;

/// <summary>
/// Represents an optional value that may or may not be present.
/// Used to avoid nullable generic Result types while maintaining type safety.
/// </summary>
/// <typeparam name="T">The type of the optional value.</typeparam>
[SuppressMessage("Design", "CA1000:Do not declare static members on generic types",
    Justification = "Optional<T> exposes static factory methods for discoverability and fluent usage; external factory would reduce ergonomics and clarity.")]
public readonly struct Optional<T> : IEquatable<Optional<T>> where T : class
{
    private readonly T? _value;

    private Optional(T? value, bool hasValue)
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
    /// Creates an Optional with a value.
    /// </summary>
    /// <param name="value">The value to wrap.</param>
    /// <returns>An Optional containing the value.</returns>
    public static Optional<T> Of(T value) => new(value, true);

    /// <summary>
    /// Creates an empty Optional.
    /// </summary>
    /// <returns>An empty Optional.</returns>
    public static Optional<T> Empty() => new(null, false);

    /// <summary>
    /// Creates an Optional from a nullable value.
    /// </summary>
    /// <param name="value">The nullable value.</param>
    /// <returns>An Optional containing the value if not null, otherwise empty.</returns>
    public static Optional<T> FromNullable(T? value) =>
        value is null ? Empty() : Of(value);

    /// <inheritdoc />
    public bool Equals(Optional<T> other) =>
        HasValue == other.HasValue && EqualityComparer<T?>.Default.Equals(_value, other._value);

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is Optional<T> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(HasValue, _value);

    /// <summary>
    /// Determines whether two Optional instances are equal.
    /// </summary>
    public static bool operator ==(Optional<T> left, Optional<T> right) =>
        left.Equals(right);

    /// <summary>
    /// Determines whether two Optional instances are not equal.
    /// </summary>
    public static bool operator !=(Optional<T> left, Optional<T> right) =>
        !left.Equals(right);
}
