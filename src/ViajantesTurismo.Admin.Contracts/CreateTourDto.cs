using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Admin.Contracts;

/// <summary>
/// Represents the data required to create a new tour.
/// </summary>
public sealed record CreateTourDto : IValidatableObject
{
    /// <summary>
    /// A unique identifier for the tour.
    /// </summary>
    [Required, StringLength(ContractConstants.MaxNameLength, MinimumLength = 1)]
    public required string Identifier { get; init; }

    /// <summary>
    /// The name of the tour.
    /// </summary>
    [Required, StringLength(ContractConstants.MaxNameLength, MinimumLength = 1)]
    public required string Name { get; init; }

    /// <summary>
    /// The start date of the tour.
    /// </summary>
    [Required]
    public required DateTime StartDate { get; init; }

    /// <summary>
    /// The end date of the tour.
    /// </summary>
    [Required]
    public required DateTime EndDate { get; init; }

    /// <summary>
    /// The base price of the tour per person.
    /// </summary>
    [Required, Range(0.01, ContractConstants.MaxPrice)]
    public required decimal Price { get; init; }

    /// <summary>
    /// The supplement price for single room occupancy (solo traveler surcharge).
    /// </summary>
    [Required, Range(0.01, ContractConstants.MaxPrice)]
    public required decimal SingleRoomSupplementPrice { get; init; }

    /// <summary>
    /// The price for renting a regular bike.
    /// </summary>
    [Required, Range(0.01, ContractConstants.MaxPrice)]
    public required decimal RegularBikePrice { get; init; }

    /// <summary>
    /// The price for renting an e-bike.
    /// </summary>
    [Required, Range(0.01, ContractConstants.MaxPrice)]
    public required decimal EBikePrice { get; init; }

    /// <summary>
    /// The currency information for all prices.
    /// </summary>
    [Required]
    public required CurrencyDto Currency { get; init; }

    /// <summary>
    /// The list of services included in the tour package.
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
