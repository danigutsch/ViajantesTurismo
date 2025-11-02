namespace ViajantesTurismo.Common.BuildingBlocks;

/// <summary>
/// Base class for Domain Entities.
/// </summary>
public abstract class Entity<TId>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Entity{TId}"/> class.
    /// </summary>
    /// <remarks>
    /// <see cref="Id"/> should be set by the ORM or factory method.
    /// </remarks>
    protected Entity()
    {
        Id = default!;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Entity{TId}"/> class with the specified identifier.
    /// </summary>
    /// <param name="id">The unique identifier for the entity.</param>
    protected Entity(TId id)
    {
        Id = id;
    }

    /// <summary>
    /// The unique identifier for the entity.
    /// </summary>
    public TId Id { get; private init; }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other)
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

        if (EqualityComparer<TId>.Default.Equals(Id, default!) ||
            EqualityComparer<TId>.Default.Equals(other.Id, default!))
        {
            return false;
        }

        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        ArgumentNullException.ThrowIfNull(Id);
        return EqualityComparer<TId>.Default.GetHashCode(Id);
    }
}
