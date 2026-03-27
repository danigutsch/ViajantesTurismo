using ViajantesTurismo.Common.Monies;

namespace ViajantesTurismo.Admin.Tests.Shared.Behavior;

/// <summary>
/// Options for overriding tour pricing values in tests.
/// </summary>
/// <param name="BasePrice">The base price override.</param>
/// <param name="SingleRoomSupplementPrice">The single room supplement override.</param>
/// <param name="RegularBikePrice">The regular bike price override.</param>
/// <param name="EBikePrice">The e-bike price override.</param>
/// <param name="Currency">The currency override.</param>
public sealed record TourPricingOptions(
    decimal? BasePrice = null,
    decimal? SingleRoomSupplementPrice = null,
    decimal? RegularBikePrice = null,
    decimal? EBikePrice = null,
    Currency? Currency = null);
