using JetBrains.Annotations;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Common.Results;
using ViajantesTurismo.Common.Sanitizers;

namespace ViajantesTurismo.Admin.Domain.Tours;

/// <summary>
/// Represents a customer's participation in a booking, including their bike selection and price.
/// </summary>
public sealed class BookingCustomer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BookingCustomer"/> class.
    /// </summary>
    /// <param name="customerId">The ID of the customer.</param>
    /// <param name="bikeType">The type of bike selected.</param>
    /// <param name="bikePrice">The price of the bike rental.</param>
    private BookingCustomer(int customerId, BikeType bikeType, decimal bikePrice)
    {
        CustomerId = customerId;
        BikeType = bikeType;
        BikePrice = bikePrice;
    }

    /// <summary>
    /// DO NOT USE. This constructor is required by Entity Framework Core for materialization.
    /// </summary>
    [UsedImplicitly]
    private BookingCustomer()
    {
    }

    /// <summary>
    /// The ID of the customer.
    /// </summary>
    public int CustomerId { get; private init; }

    /// <summary>
    /// The type of bike selected by the customer.
    /// </summary>
    public BikeType BikeType { get; private init; }

    /// <summary>
    /// The price of the bike rental at the time of booking.
    /// </summary>
    public decimal BikePrice { get; private init; }

    /// <summary>
    /// Creates a new instance of <see cref="BookingCustomer"/> with validation.
    /// </summary>
    /// <param name="customerId">The ID of the customer.</param>
    /// <param name="bikeType">The type of bike selected.</param>
    /// <param name="bikePrice">The price of the bike rental.</param>
    /// <returns>A Result containing the BookingCustomer if successful, or validation errors.</returns>
    public static Result<BookingCustomer> Create(int customerId, BikeType bikeType, decimal bikePrice)
    {
        bikePrice = NumericSanitizer.SanitizePrice(bikePrice);

        var errors = new ValidationErrors();

        if (!Enum.IsDefined(bikeType))
        {
            errors.Add(BookingErrors.InvalidBikeType(bikeType));
        }

        if (bikeType == BikeType.None)
        {
            errors.Add(BookingErrors.BikeTypeNotSelected());
        }

        if (bikePrice < 0)
        {
            errors.Add(BookingErrors.NegativeBikePrice(bikePrice));
        }

        if (bikePrice > ContractConstants.MaxPrice)
        {
            errors.Add(BookingErrors.BikePriceExceedsMaximum(bikePrice, ContractConstants.MaxPrice));
        }

        if (errors.HasErrors)
        {
            return errors.ToResult<BookingCustomer>();
        }

        return new BookingCustomer(customerId, bikeType, bikePrice);
    }
}