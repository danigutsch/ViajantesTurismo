using JetBrains.Annotations;
using ViajantesTurismo.Admin.Domain.Bookings;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.AdminApi.Contracts;
using ViajantesTurismo.Common.BuildingBlocks;
using ViajantesTurismo.Common.Monies;
using ViajantesTurismo.Common.Results;
using ViajantesTurismo.Common.Sanitizers;

namespace ViajantesTurismo.Admin.Domain.Tours;

/// <summary>
/// Represents a tour entity with details such as identifier, name, dates, pricing, currency, and included services.
/// </summary>
/// <remarks>
/// <para><strong>AGGREGATE ROOT:</strong> Tour is the aggregate root for the Tour-Booking aggregate.</para>
/// <para>All Booking entities must be created and modified through Tour methods to maintain consistency.</para>
/// <para>Tour enforces business rules and invariants for all bookings within its aggregate boundary.</para>
/// </remarks>
public sealed class Tour : Entity<int>
{
    private readonly List<Booking> _bookings = [];
    private string[] _includedServices = [];

    private Tour(string identifier,
        string name,
        DateTime startDate,
        DateTime endDate,
        decimal price,
        decimal doubleRoomSupplementPrice,
        decimal regularBikePrice,
        decimal eBikePrice,
        Currency currency,
        IEnumerable<string> includedServices)
    {
        Identifier = identifier;
        Name = name;
        StartDate = startDate;
        EndDate = endDate;
        Price = price;
        DoubleRoomSupplementPrice = doubleRoomSupplementPrice;
        RegularBikePrice = regularBikePrice;
        EBikePrice = eBikePrice;
        Currency = currency;
        _includedServices = [..includedServices];
    }

    /// <summary>
    /// Gets the unique business identifier for the tour.
    /// </summary>
    public string Identifier { get; private set; }

    /// <summary>
    /// Gets the name of the tour.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the start date of the tour.
    /// </summary>
    public DateTime StartDate { get; private set; }

    /// <summary>
    /// Gets the end date of the tour.
    /// </summary>
    public DateTime EndDate { get; private set; }

    /// <summary>
    /// Gets the base price for a single room (not per person).
    /// </summary>
    public decimal Price { get; private set; }

    /// <summary>
    /// Gets the additional price for a double room (larger space, more expensive than single room).
    /// </summary>
    public decimal DoubleRoomSupplementPrice { get; private set; }

    /// <summary>
    /// Gets the price for renting a regular bike.
    /// </summary>
    public decimal RegularBikePrice { get; private set; }

    /// <summary>
    /// Gets the price for renting an e-bike.
    /// </summary>
    public decimal EBikePrice { get; private set; }

    /// <summary>
    /// Gets the currency used for all pricing in this tour.
    /// </summary>
    public Currency Currency { get; private set; }

    /// <summary>
    /// Gets the array of services included in the tour package (e.g., "Hotel", "Breakfast", "City Tour").
    /// </summary>
    public IReadOnlyList<string> IncludedServices => _includedServices.AsReadOnly();

    /// <summary>
    /// Gets the bookings for this tour.
    /// </summary>
    public IReadOnlyList<Booking> Bookings => _bookings.AsReadOnly();

    /// <summary>
    /// Creates a new instance of the <see cref="Tour"/> class with validation.
    /// </summary>
    /// <param name="identifier">The unique business identifier for the tour (e.g., "CUBA2024").</param>
    /// <param name="name">The descriptive name of the tour.</param>
    /// <param name="startDate">The start date of the tour.</param>
    /// <param name="endDate">The end date of the tour.</param>
    /// <param name="price">The base price for a single room (not per person).</param>
    /// <param name="doubleRoomSupplementPrice">The additional price for a double room.</param>
    /// <param name="regularBikePrice">The price for renting a regular bike.</param>
    /// <param name="eBikePrice">The price for renting an e-bike.</param>
    /// <param name="currency">The currency for all pricing.</param>
    /// <param name="includedServices">The collection of services included in the tour package.</param>
    /// <returns>A Result containing the Tour if validation succeeds, or an error if validation fails.</returns>
    public static Result<Tour> Create(
        string identifier,
        string name,
        DateTime startDate,
        DateTime endDate,
        decimal price,
        decimal doubleRoomSupplementPrice,
        decimal regularBikePrice,
        decimal eBikePrice,
        Currency currency,
        IEnumerable<string> includedServices)
    {
        identifier = StringSanitizer.Sanitize(identifier);
        name = StringSanitizer.Sanitize(name);
        price = NumericSanitizer.SanitizePrice(price);
        doubleRoomSupplementPrice = NumericSanitizer.SanitizePrice(doubleRoomSupplementPrice);
        regularBikePrice = NumericSanitizer.SanitizePrice(regularBikePrice);
        eBikePrice = NumericSanitizer.SanitizePrice(eBikePrice);
        var sanitizedServices = StringSanitizer.SanitizeCollection(includedServices);

        var errors = new ValidationErrors();

        if (string.IsNullOrWhiteSpace(identifier))
        {
            errors.Add(TourErrors.EmptyIdentifier());
        }
        else if (identifier.Length > ContractConstants.MaxNameLength)
        {
            errors.Add(TourErrors.IdentifierTooLong(ContractConstants.MaxNameLength, identifier.Length));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add(TourErrors.EmptyName());
        }
        else if (name.Length > ContractConstants.MaxNameLength)
        {
            errors.Add(TourErrors.NameTooLong(ContractConstants.MaxNameLength, name.Length));
        }

        if (endDate <= startDate)
        {
            errors.Add(TourErrors.InvalidDateRange());
        }
        else
        {
            var duration = (endDate - startDate).TotalDays;
            if (duration <= ContractConstants.MinimumTourDurationDays)
            {
                errors.Add(TourErrors.DurationTooShort(ContractConstants.MinimumTourDurationDays, duration));
            }
        }

        if (price <= 0)
        {
            errors.Add(TourErrors.InvalidPrice("Base price", price));
        }
        else if (price > ContractConstants.MaxPrice)
        {
            errors.Add(TourErrors.PriceTooHigh("Base price", ContractConstants.MaxPrice, price));
        }

        if (doubleRoomSupplementPrice <= 0)
        {
            errors.Add(TourErrors.InvalidPrice("Double room supplement price", doubleRoomSupplementPrice));
        }
        else if (doubleRoomSupplementPrice > ContractConstants.MaxPrice)
        {
            errors.Add(TourErrors.PriceTooHigh("Double room supplement price", ContractConstants.MaxPrice, doubleRoomSupplementPrice));
        }

        if (regularBikePrice <= 0)
        {
            errors.Add(TourErrors.InvalidPrice("Regular bike price", regularBikePrice));
        }
        else if (regularBikePrice > ContractConstants.MaxPrice)
        {
            errors.Add(TourErrors.PriceTooHigh("Regular bike price", ContractConstants.MaxPrice, regularBikePrice));
        }

        if (eBikePrice <= 0)
        {
            errors.Add(TourErrors.InvalidPrice("E-bike price", eBikePrice));
        }
        else if (eBikePrice > ContractConstants.MaxPrice)
        {
            errors.Add(TourErrors.PriceTooHigh("E-bike price", ContractConstants.MaxPrice, eBikePrice));
        }

        if (errors.HasErrors)
        {
            return errors.ToResult<Tour>();
        }

        return new Tour(
            identifier,
            name,
            startDate,
            endDate,
            price,
            doubleRoomSupplementPrice,
            regularBikePrice,
            eBikePrice,
            currency,
            sanitizedServices);
    }

    /// <summary>
    /// Updates the tour's basic information (identifier and name).
    /// </summary>
    /// <param name="identifier">The unique business identifier for the tour.</param>
    /// <param name="name">The descriptive name of the tour.</param>
    /// <returns>A Result indicating success or failure.</returns>
    public Result UpdateDetails(string identifier, string name)
    {
        var sanitizedIdentifier = StringSanitizer.Sanitize(identifier);
        var sanitizedName = StringSanitizer.Sanitize(name);

        var errors = new ValidationErrors();

        if (string.IsNullOrWhiteSpace(sanitizedIdentifier))
        {
            errors.Add(TourErrors.EmptyIdentifier());
        }
        else if (sanitizedIdentifier.Length > ContractConstants.MaxNameLength)
        {
            errors.Add(TourErrors.IdentifierTooLong(ContractConstants.MaxNameLength, sanitizedIdentifier.Length));
        }

        if (string.IsNullOrWhiteSpace(sanitizedName))
        {
            errors.Add(TourErrors.EmptyName());
        }
        else if (sanitizedName.Length > ContractConstants.MaxNameLength)
        {
            errors.Add(TourErrors.NameTooLong(ContractConstants.MaxNameLength, sanitizedName.Length));
        }

        if (errors.HasErrors)
        {
            return errors.ToResult();
        }

        Identifier = sanitizedIdentifier;
        Name = sanitizedName;
        return Result.Ok();
    }

    /// <summary>
    /// Updates the tour's schedule (start and end dates).
    /// </summary>
    /// <param name="startDate">The start date of the tour.</param>
    /// <param name="endDate">The end date of the tour.</param>
    /// <returns>A Result indicating success or failure.</returns>
    public Result UpdateSchedule(DateTime startDate, DateTime endDate)
    {
        var errors = new ValidationErrors();

        if (endDate <= startDate)
        {
            errors.Add(TourErrors.InvalidDateRangeUpdate());
        }
        else
        {
            var duration = (endDate - startDate).TotalDays;
            if (duration <= ContractConstants.MinimumTourDurationDays)
            {
                errors.Add(TourErrors.DurationTooShortUpdate(ContractConstants.MinimumTourDurationDays, duration));
            }
        }

        if (errors.HasErrors)
        {
            return errors.ToResult();
        }

        StartDate = startDate;
        EndDate = endDate;
        return Result.Ok();
    }

    /// <summary>
    /// Updates all pricing for the tour except for base price, but including supplements, and bike rentals.
    /// </summary>
    /// <param name="doubleRoomSupplementPrice">The additional price for a double room.</param>
    /// <param name="regularBikePrice">The price for renting a regular bike.</param>
    /// <param name="eBikePrice">The price for renting an e-bike.</param>
    /// <param name="currency">The currency for all pricing.</param>
    /// <returns>A Result indicating success or failure.</returns>
    public Result UpdatePricing(
        decimal doubleRoomSupplementPrice,
        decimal regularBikePrice,
        decimal eBikePrice,
        Currency currency)
    {
        doubleRoomSupplementPrice = NumericSanitizer.SanitizePrice(doubleRoomSupplementPrice);
        regularBikePrice = NumericSanitizer.SanitizePrice(regularBikePrice);
        eBikePrice = NumericSanitizer.SanitizePrice(eBikePrice);

        var errors = new ValidationErrors();

        if (doubleRoomSupplementPrice <= 0)
        {
            errors.Add(TourErrors.InvalidPrice("Double room supplement price", doubleRoomSupplementPrice));
        }
        else if (doubleRoomSupplementPrice > ContractConstants.MaxPrice)
        {
            errors.Add(TourErrors.PriceTooHigh("Double room supplement price", ContractConstants.MaxPrice, doubleRoomSupplementPrice));
        }

        if (regularBikePrice <= 0)
        {
            errors.Add(TourErrors.InvalidPrice("Regular bike price", regularBikePrice));
        }
        else if (regularBikePrice > ContractConstants.MaxPrice)
        {
            errors.Add(TourErrors.PriceTooHigh("Regular bike price", ContractConstants.MaxPrice, regularBikePrice));
        }

        if (eBikePrice <= 0)
        {
            errors.Add(TourErrors.InvalidPrice("E-bike price", eBikePrice));
        }
        else if (eBikePrice > ContractConstants.MaxPrice)
        {
            errors.Add(TourErrors.PriceTooHigh("E-bike price", ContractConstants.MaxPrice, eBikePrice));
        }

        if (errors.HasErrors)
        {
            return errors.ToResult();
        }

        DoubleRoomSupplementPrice = doubleRoomSupplementPrice;
        RegularBikePrice = regularBikePrice;
        EBikePrice = eBikePrice;
        Currency = currency;
        return Result.Ok();
    }

    /// <summary>
    /// Updates the base price of the tour.
    /// </summary>
    /// <param name="price">The new base price per person.</param>
    /// <returns>A Result indicating success or failure.</returns>
    public Result UpdateBasePrice(decimal price)
    {
        price = NumericSanitizer.SanitizePrice(price);

        if (price <= 0)
        {
            return TourErrors.InvalidPrice("Base price", price).ToResult();
        }

        if (price > ContractConstants.MaxPrice)
        {
            return TourErrors.PriceTooHigh("Base price", ContractConstants.MaxPrice, price).ToResult();
        }

        Price = price;
        return Result.Ok();
    }

    /// <summary>
    /// Updates the currency used for all tour pricing.
    /// </summary>
    /// <param name="currency">The new currency.</param>
    public void UpdateCurrency(Currency currency)
    {
        Currency = currency;
    }

    /// <summary>
    /// Updates the services included in the tour package.
    /// </summary>
    /// <param name="includedServices">The collection of services included in the tour package.</param>
    public void UpdateIncludedServices(IEnumerable<string> includedServices)
    {
        var sanitizedServices = StringSanitizer.SanitizeCollection(includedServices);

        _includedServices = sanitizedServices;
    }

    /// <summary>
    /// Adds a new booking to this tour.
    /// </summary>
    /// <param name="principalCustomerId">The ID of the principal customer making the booking.</param>
    /// <param name="principalBikeType">The bike type selected by the principal customer.</param>
    /// <param name="companionCustomerId">The ID of the companion customer, if any.</param>
    /// <param name="companionBikeType">The bike type selected by the companion, if any.</param>
    /// <param name="roomType">The room type for the booking.</param>
    /// <param name="notes">Optional notes about the booking.</param>
    /// <returns>A Result containing the created booking if successful, or validation errors.</returns>
    public Result<Booking> AddBooking(
        int principalCustomerId,
        BikeType principalBikeType,
        int? companionCustomerId,
        BikeType? companionBikeType,
        RoomType roomType,
        string? notes)
    {
        var principalBikePrice = GetBikePrice(principalBikeType);
        var principalCustomerResult = BookingCustomer.Create(principalCustomerId, principalBikeType, principalBikePrice);
        if (principalCustomerResult.IsFailure)
        {
            return principalCustomerResult.ConvertError<BookingCustomer, Booking>();
        }

        var companionCustomerResult = CreateCompanionCustomer(companionCustomerId, companionBikeType);
        if (companionCustomerResult is { IsFailure: true })
        {
            return companionCustomerResult.Value.ConvertError<BookingCustomer, Booking>();
        }

        var companionCustomer = companionCustomerResult?.Value;

        var roomAdditionalCost = CalculateRoomAdditionalCost(roomType);

        var result = Booking.Create(
            Id,
            Price,
            roomType,
            roomAdditionalCost,
            principalCustomerResult.Value,
            companionCustomer,
            notes);

        if (result.IsFailure)
        {
            return result;
        }

        var booking = result.Value;
        _bookings.Add(booking);
        return booking;
    }

    private decimal GetBikePrice(BikeType bikeType) => bikeType switch
    {
        BikeType.Regular => RegularBikePrice,
        BikeType.EBike => EBikePrice,
        _ => 0m
    };

    private Result<BookingCustomer>? CreateCompanionCustomer(int? companionCustomerId, BikeType? companionBikeType)
    {
        if (companionCustomerId is null)
        {
            return null;
        }

        var bikeType = companionBikeType ?? BikeType.None;
        var bikePrice = GetBikePrice(bikeType);
        return BookingCustomer.Create(companionCustomerId.Value, bikeType, bikePrice);
    }

    private decimal CalculateRoomAdditionalCost(RoomType roomType) =>
        roomType == RoomType.DoubleRoom ? DoubleRoomSupplementPrice : 0m;

    /// <summary>
    /// Updates the payment status of a specific booking.
    /// </summary>
    /// <param name="bookingId">The ID of the booking to update.</param>
    /// <param name="paymentStatus">The payment status.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result UpdateBookingPaymentStatus(long bookingId, PaymentStatus paymentStatus)
    {
        var booking = _bookings.FirstOrDefault(b => b.Id == bookingId);
        if (booking is null)
        {
            return TourErrors.BookingNotFound(bookingId);
        }

        booking.UpdatePaymentStatus(paymentStatus);
        return Result.Ok();
    }

    /// <summary>
    /// Cancels a booking.
    /// </summary>
    /// <param name="bookingId">The ID of the booking to cancel.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result CancelBooking(long bookingId)
    {
        var booking = _bookings.FirstOrDefault(b => b.Id == bookingId);
        if (booking is null)
        {
            return TourErrors.BookingNotFound(bookingId);
        }

        return booking.Cancel();
    }

    /// <summary>
    /// Confirms a booking.
    /// </summary>
    /// <param name="bookingId">The ID of the booking to confirm.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result ConfirmBooking(long bookingId)
    {
        var booking = _bookings.FirstOrDefault(b => b.Id == bookingId);
        if (booking is null)
        {
            return TourErrors.BookingNotFound(bookingId);
        }

        return booking.Confirm();
    }

    /// <summary>
    /// Completes a booking.
    /// </summary>
    /// <param name="bookingId">The ID of the booking to complete.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result CompleteBooking(long bookingId)
    {
        var booking = _bookings.FirstOrDefault(b => b.Id == bookingId);
        if (booking is null)
        {
            return TourErrors.BookingNotFound(bookingId);
        }

        return booking.Complete();
    }

    /// <summary>
    /// Updates the price of a booking.
    /// </summary>
    /// <param name="bookingId">The ID of the booking to update.</param>
    /// <param name="newPrice">The new total price.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result UpdateBookingPrice(long bookingId, decimal newPrice)
    {
        var booking = _bookings.FirstOrDefault(b => b.Id == bookingId);
        if (booking is null)
        {
            return TourErrors.BookingNotFound(bookingId);
        }

        return booking.UpdatePrice(newPrice);
    }

    /// <summary>
    /// Updates the notes of a booking.
    /// </summary>
    /// <param name="bookingId">The ID of the booking to update.</param>
    /// <param name="notes">The updated notes.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result UpdateBookingNotes(long bookingId, string? notes)
    {
        var booking = _bookings.FirstOrDefault(b => b.Id == bookingId);
        if (booking is null)
        {
            return TourErrors.BookingNotFound(bookingId);
        }

        return booking.UpdateNotes(notes);
    }

    /// <summary>
    /// Removes a booking from this tour.
    /// </summary>
    /// <param name="bookingId">The ID of the booking to remove.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result RemoveBooking(long bookingId)
    {
        var booking = _bookings.FirstOrDefault(b => b.Id == bookingId);
        if (booking is null)
        {
            return TourErrors.BookingNotFound(bookingId);
        }

        _bookings.Remove(booking);
        return Result.Ok();
    }

    /// <summary>
    /// DO NOT USE. This constructor is required by Entity Framework Core for materialization.
    /// </summary>
    [UsedImplicitly]
#pragma warning disable CS8618
    private Tour()
    {
    }
}
