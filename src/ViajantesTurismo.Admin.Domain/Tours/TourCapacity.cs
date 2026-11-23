using PandaBear.Results;
using PandaBear.BuildingBlocks;

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
    public int MinCustomers
    {
        get;
    }

    /// <summary>
    /// Gets the maximum number of customers allowed on the tour.
    /// </summary>
    public int MaxCustomers
    {
        get;
    }

    /// <summary>Gets the number of places remaining for the supplied occupancy.</summary>
    internal int AvailableSpotsAt(int customerCount) => MaxCustomers - customerCount;

    /// <summary>Determines whether the supplied occupancy reaches the tour minimum.</summary>
    internal bool HasReachedMinimum(int customerCount) => customerCount >= MinCustomers;

    /// <summary>Determines whether the supplied occupancy has filled the tour.</summary>
    internal bool IsFullAt(int customerCount) => customerCount >= MaxCustomers;

    /// <summary>
    /// Creates a new tour capacity with validation.
    /// </summary>
    /// <param name="minCustomers">The minimum number of customers required.</param>
    /// <param name="maxCustomers">The maximum number of customers allowed.</param>
    /// <returns>A Result containing the TourCapacity if valid, or errors if validation fails.</returns>
    public static Result<TourCapacity> Create(int minCustomers, int maxCustomers)
    {
        var errors = new ValidationErrors();

        if (minCustomers is < AdminDomainLimits.MinTourCustomers or > AdminDomainLimits.MaxTourCustomers)
        {
            errors.Add(TourErrors.InvalidMinCustomers(minCustomers));
        }

        if (maxCustomers is < AdminDomainLimits.MinTourCustomers or > AdminDomainLimits.MaxTourCustomers)
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

        return Result.Ok(new TourCapacity(minCustomers, maxCustomers));
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return MinCustomers;
        yield return MaxCustomers;
    }
}
