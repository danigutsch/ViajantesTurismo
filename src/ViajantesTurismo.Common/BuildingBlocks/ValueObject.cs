using System.Diagnostics.CodeAnalysis;

namespace ViajantesTurismo.Common.BuildingBlocks;

/// <summary>
/// Base class for Value Objects in Domain-Driven Design.
/// </summary>
/// <remarks>
/// <para>
/// Value Objects are immutable objects that are defined by their attributes rather than a unique identity.
/// Two value objects are considered equal if all their attributes have the same values.
/// </para>
/// <para>
/// <strong>Characteristics:</strong>
/// </para>
/// <list type="bullet">
/// <item><description>Immutable: Once created, their state cannot be changed.</description></item>
/// <item><description>Equality by value: Two instances are equal if their properties are equal.</description></item>
/// <item><description>No identity: Unlike entities, they don't have a unique identifier.</description></item>
/// <item><description>Pure functions: Operations on value objects should return new instances.</description></item>
/// </list>
/// <para>
/// <strong>Examples:</strong> Address, Money, DateRange, ContactInfo, PhysicalInfo
/// </para>
/// </remarks>
[SuppressMessage(SuppressConstants.CategoryDesign, SuppressConstants.CheckIdS4035,
    Justification = "Abstract ValueObject uses Template Method pattern via GetEqualityComponents(); derived classes control equality by overriding that method, not Equals itself.")]
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <inheritdoc />
    public bool Equals(ValueObject? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (GetType() != other.GetType())
        {
            return false;
        }

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is ValueObject other && Equals(other);
    }

    /// <summary>
    /// Gets the atomic values that define this value object for equality comparison.
    /// </summary>
    /// <returns>An enumerable of objects representing the equality components.</returns>
    /// <remarks>
    /// Derived classes must implement this method to return all the properties
    /// that should be used for equality comparison. The order matters.
    /// </remarks>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x?.GetHashCode() ?? 0)
            .Aggregate((x, y) => x ^ y);
    }

    /// <summary>
    /// Determines whether two value objects are equal.
    /// </summary>
    /// <param name="left">The left value object.</param>
    /// <param name="right">The right value object.</param>
    /// <returns>True if the value objects are equal; otherwise, false.</returns>
    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        if (left is null && right is null)
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two value objects are not equal.
    /// </summary>
    /// <param name="left">The left value object.</param>
    /// <param name="right">The right value object.</param>
    /// <returns>True if the value objects are not equal; otherwise, false.</returns>
    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !(left == right);
    }
}
