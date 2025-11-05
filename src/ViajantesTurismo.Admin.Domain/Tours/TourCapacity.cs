using ViajantesTurismo.Common.BuildingBlocks;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.Domain.Tours;

/// <summary>
/// Represents tour capacity constraints with validation.
/// </summary>
public sealed class TourCapacity : ValueObject
{
    private TourCapacity(int minCustomers, int maxCustomers)
    {
        MinCustomers = minCustomers;
        MaxCustomers = maxCustomers;
    }

    /// <summary>
    /// Gets the minimum number of customers required for the tour.
    /// </summary>
    public int MinCustomers { get; }

    /// <summary>
    /// Gets the maximum number of customers allowed on the tour.
    /// </summary>
    public int MaxCustomers { get; }

    /// <summary>
    /// Creates a new tour capacity with validation.
    /// </summary>
    /// <param name="minCustomers">The minimum number of customers required.</param>
    /// <param name="maxCustomers">The maximum number of customers allowed.</param>
    /// <returns>A Result containing the TourCapacity if valid, or errors if validation fails.</returns>
    public static Result<TourCapacity> Create(int minCustomers, int maxCustomers)
    {
        var errors = new ValidationErrors();

        if (minCustomers is < 1 or > 20)
        {
            errors.Add(TourErrors.InvalidMinCustomers(minCustomers));
        }

        if (maxCustomers is < 1 or > 20)
        {
            errors.Add(TourErrors.InvalidMaxCustomers(maxCustomers));
        }

        if (maxCustomers < minCustomers)
        {
            errors.Add(TourErrors.MaxCustomersLessThanMin(minCustomers, maxCustomers));
        }

        if (errors.HasErrors)
        {
            return errors.ToResult<TourCapacity>();
        }

        return new TourCapacity(minCustomers, maxCustomers);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return MinCustomers;
        yield return MaxCustomers;
    }
}
