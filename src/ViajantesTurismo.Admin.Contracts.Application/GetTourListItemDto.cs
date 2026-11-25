using SharedKernel.BuildingBlocks;

namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Provides a tour's pricing, dates, and capacity for the Management overview.</summary>
/// <param name="Id">The tour's stable identifier.</param>
/// <param name="Identifier">The operational identifier.</param>
/// <param name="Name">The tour name.</param>
/// <param name="StartDate">The first tour date.</param>
/// <param name="EndDate">The final tour date.</param>
/// <param name="Price">The base price.</param>
/// <param name="SingleRoomSupplementPrice">The single room supplement.</param>
/// <param name="RegularBikePrice">The regular bicycle rental price.</param>
/// <param name="EBikePrice">The electric bicycle rental price.</param>
/// <param name="Currency">The ISO 4217 currency code for the prices.</param>
/// <param name="MinCustomers">The minimum number of customers for the tour.</param>
/// <param name="MaxCustomers">The maximum tour capacity.</param>
/// <param name="CurrentCustomerCount">The confirmed principals and companions.</param>
/// <param name="AvailableSpots">The remaining capacity.</param>
/// <param name="IsFullyBooked">Whether the tour has reached maximum capacity.</param>
/// <param name="IsAtMinimumCapacity">Whether the tour has reached minimum capacity.</param>
public sealed record GetTourListItemDto(
    Guid Id,
    string Identifier,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal Price,
    decimal SingleRoomSupplementPrice,
    decimal RegularBikePrice,
    decimal EBikePrice,
    string Currency,
    int MinCustomers,
    int MaxCustomers,
    int CurrentCustomerCount,
    int AvailableSpots,
    bool IsFullyBooked,
    bool IsAtMinimumCapacity)
{
    /// <summary>Gets the resource and collection links provided by the Admin API.</summary>
    public IReadOnlyList<HypermediaLink>? Links
    {
        get; init;
    }
}
