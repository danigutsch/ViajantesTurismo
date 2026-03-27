using ViajantesTurismo.Common.Monies;

namespace ViajantesTurismo.Admin.Domain.Tours;

/// <summary>
/// Defines the pricing requested for a tour.
/// </summary>
/// <param name="BasePrice">The base price for the tour.</param>
/// <param name="SingleRoomSupplementPrice">The single room supplement price.</param>
/// <param name="RegularBikePrice">The regular bike rental price.</param>
/// <param name="EBikePrice">The e-bike rental price.</param>
/// <param name="Currency">The pricing currency.</param>
public sealed record TourPricingDefinition(
    decimal BasePrice,
    decimal SingleRoomSupplementPrice,
    decimal RegularBikePrice,
    decimal EBikePrice,
    Currency Currency);
