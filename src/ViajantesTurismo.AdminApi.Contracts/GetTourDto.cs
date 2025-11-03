namespace ViajantesTurismo.AdminApi.Contracts;

/// <summary>
/// Data Transfer Object representing a tour with all its details.
/// Used for retrieving tour information in the Admin API.
/// </summary>
public sealed record GetTourDto
{
    /// <summary>
    /// Unique identifier of the tour.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// External or business identifier for the tour.
    /// </summary>
    public required string Identifier { get; init; }

    /// <summary>
    /// Name of the tour.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Start date of the tour.
    /// </summary>
    public required DateTime StartDate { get; init; }

    /// <summary>
    /// End date of the tour.
    /// </summary>
    public required DateTime EndDate { get; init; }

    /// <summary>
    /// Base price for a single room (not per person).
    /// </summary>
    public required decimal Price { get; init; }

    /// <summary>
    /// Additional price for a double room (larger space).
    /// </summary>
    public required decimal DoubleRoomSupplementPrice { get; init; }

    /// <summary>
    /// Price for renting a regular bike.
    /// </summary>
    public required decimal RegularBikePrice { get; init; }

    /// <summary>
    /// Price for renting an e-bike.
    /// </summary>
    public required decimal EBikePrice { get; init; }

    /// <summary>
    /// Currency information for the tour prices.
    /// </summary>
    public required CurrencyDto Currency { get; init; }

    /// <summary>
    /// List of services included in the tour.
    /// </summary>
    public required ICollection<string> IncludedServices { get; init; }
}