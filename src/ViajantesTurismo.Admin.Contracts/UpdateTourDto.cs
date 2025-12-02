using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Admin.Contracts;

/// <summary>
/// Data Transfer Object for updating an existing tour.
/// </summary>
public sealed record UpdateTourDto : IValidatableObject
{
    /// <summary>
    /// External or business identifier for the tour.
    /// </summary>
    [Required, StringLength(ContractConstants.MaxNameLength, MinimumLength = 1)]
    public required string Identifier { get; init; }

    /// <summary>
    /// Name of the tour.
    /// </summary>
    [Required, StringLength(ContractConstants.MaxNameLength, MinimumLength = 1)]
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
    /// Additional price for a double room.
    /// </summary>
    [Required, Range(0.01, ContractConstants.MaxPrice)]
    public required decimal DoubleRoomSupplementPrice { get; init; }

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

    /// <summary>
    /// Minimum number of customers required for the tour to proceed.
    /// </summary>
    [Required, Range(ContractConstants.MinTourCustomers, ContractConstants.MaxTourCustomers)]
    public required int MinCustomers { get; init; }

    /// <summary>
    /// Maximum number of customers allowed on the tour.
    /// </summary>
    [Required, Range(ContractConstants.MinTourCustomers, ContractConstants.MaxTourCustomers)]
    public required int MaxCustomers { get; init; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var result = TourValidation.ValidateDuration(
            StartDate,
            EndDate,
            ContractConstants.MinimumTourDurationDays,
            nameof(StartDate),
            nameof(EndDate));

        if (result is not null)
        {
            yield return result;
        }
    }
}
