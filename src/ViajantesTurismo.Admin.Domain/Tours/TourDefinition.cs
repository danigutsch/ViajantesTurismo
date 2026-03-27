using ViajantesTurismo.Common.Monies;

namespace ViajantesTurismo.Admin.Domain.Tours;

/// <summary>
/// Defines the input required to create a tour aggregate.
/// </summary>
/// <param name="Identifier">The unique business identifier for the tour.</param>
/// <param name="Name">The display name of the tour.</param>
/// <param name="Schedule">The requested tour schedule.</param>
/// <param name="Pricing">The requested tour pricing.</param>
/// <param name="Capacity">The requested tour capacity.</param>
/// <param name="IncludedServices">The services included in the tour package.</param>
public sealed record TourDefinition(
    string Identifier,
    string Name,
    TourScheduleDefinition Schedule,
    TourPricingDefinition Pricing,
    TourCapacityDefinition Capacity,
    IEnumerable<string> IncludedServices)
{
    /// <summary>
    /// Creates a tour definition from flat values.
    /// </summary>
    /// <param name="identifier">The unique business identifier for the tour.</param>
    /// <param name="name">The display name of the tour.</param>
    /// <param name="startDate">The scheduled start date.</param>
    /// <param name="endDate">The scheduled end date.</param>
    /// <param name="basePrice">The base tour price.</param>
    /// <param name="singleRoomSupplementPrice">The single-room supplement price.</param>
    /// <param name="regularBikePrice">The regular-bike rental price.</param>
    /// <param name="eBikePrice">The e-bike rental price.</param>
    /// <param name="currency">The pricing currency.</param>
    /// <param name="minCustomers">The minimum customer count.</param>
    /// <param name="maxCustomers">The maximum customer count.</param>
    /// <param name="includedServices">The services included in the tour package.</param>
    public TourDefinition(
        string identifier,
        string name,
        DateTime startDate,
        DateTime endDate,
        decimal basePrice,
        decimal singleRoomSupplementPrice,
        decimal regularBikePrice,
        decimal eBikePrice,
        Currency currency,
        int minCustomers,
        int maxCustomers,
        IEnumerable<string> includedServices)
        : this(
            identifier,
            name,
            new TourScheduleDefinition(startDate, endDate),
            new TourPricingDefinition(basePrice, singleRoomSupplementPrice, regularBikePrice, eBikePrice, currency),
            new TourCapacityDefinition(minCustomers, maxCustomers),
            includedServices)
    {
    }
}
