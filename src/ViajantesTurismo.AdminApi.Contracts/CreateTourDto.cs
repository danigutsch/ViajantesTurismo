using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.AdminApi.Contracts;

/// <summary>
/// Represents the data required to create a new tour.
/// </summary>
[MinimumDuration(5)]
public sealed record CreateTourDto
{
    /// <summary>
    /// A unique identifier for the tour.
    /// </summary>
    [Required, MaxLength(ContractConstants.MaxTourNameLength)]
    public required string Identifier { get; init; }

    /// <summary>
    /// The name of the tour.
    /// </summary>
    [Required, MaxLength(ContractConstants.MaxTourNameLength)]
    public required string Name { get; init; }

    /// <summary>
    /// The start date of the tour.
    /// </summary>
    public required DateTime StartDate { get; init; }

    /// <summary>
    /// The end date of the tour.
    /// </summary>
    public required DateTime EndDate { get; init; }

    /// <summary>
    /// The base price of the tour per person.
    /// </summary>
    [Required, Range(0, 10_000)]
    public required decimal Price { get; init; }

    /// <summary>
    /// The additional price for a single room supplement.
    /// </summary>
    [Required, Range(0, 10_000)]
    public required decimal SingleRoomSupplementPrice { get; init; }

    /// <summary>
    /// The price for renting a regular bike.
    /// </summary>
    [Required, Range(0, 10_000)]
    public required decimal RegularBikePrice { get; init; }

    /// <summary>
    /// The price for renting an e-bike.
    /// </summary>
    [Required, Range(0, 10_000)]
    public required decimal EBikePrice { get; init; }

    /// <summary>
    /// The currency information for all prices.
    /// </summary>
    [Required]
    public required CurrencyDto Currency { get; init; }

    /// <summary>
    /// The list of services included in the tour package.
    /// </summary>
    public required ICollection<string> IncludedServices { get; init; }
}
