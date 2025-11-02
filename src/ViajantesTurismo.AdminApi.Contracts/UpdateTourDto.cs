using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.AdminApi.Contracts;

/// <summary>
/// Data Transfer Object for updating an existing tour.
/// </summary>
[MinimumDuration(ContractConstants.MinimumTourDurationDays)]
public sealed record UpdateTourDto
{
    /// <summary>
    /// External or business identifier for the tour.
    /// </summary>
    [Required, MaxLength(ContractConstants.MaxNameLength)]
    public required string Identifier { get; init; }

    /// <summary>
    /// Name of the tour.
    /// </summary>
    [Required, MaxLength(ContractConstants.MaxNameLength)]
    public required string Name { get; init; }

    /// <summary>
    /// Start date of the tour.
    /// </summary>
    [Required]
    public required DateTime StartDate { get; init; }

    /// <summary>
    /// End date of the tour.
    /// </summary>
    [Required]
    public required DateTime EndDate { get; init; }

    /// <summary>
    /// Base price for the tour.
    /// </summary>
    [Required, Range(0.01, ContractConstants.MaxPrice)]
    public required decimal Price { get; init; }

    /// <summary>
    /// Additional price for a single room supplement.
    /// </summary>
    [Required, Range(0.01, ContractConstants.MaxPrice)]
    public required decimal SingleRoomSupplementPrice { get; init; }

    /// <summary>
    /// Price for renting a regular bike.
    /// </summary>
    [Required, Range(0.01, ContractConstants.MaxPrice)]
    public required decimal RegularBikePrice { get; init; }

    /// <summary>
    /// Price for renting an e-bike.
    /// </summary>
    [Required, Range(0.01, ContractConstants.MaxPrice)]
    public required decimal EBikePrice { get; init; }

    /// <summary>
    /// Currency for the tour prices.
    /// </summary>
    [Required]
    public required CurrencyDto Currency { get; init; }

    /// <summary>
    /// List of services included in the tour.
    /// </summary>
    [Required]
    public required ICollection<string> IncludedServices { get; init; }
}