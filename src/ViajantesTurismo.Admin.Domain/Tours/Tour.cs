using JetBrains.Annotations;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Domain.Customers;
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
public sealed class Tour : Entity<Guid>
{
    private readonly List<Booking> _bookings = [];
    private string[] _includedServices = [];

    private Tour(
        string identifier,
        string name,
        DateRange schedule,
        TourPricing pricing,
        TourCapacity capacity,
        IEnumerable<string> includedServices)
        : base(Guid.CreateVersion7())
    {
        Identifier = identifier;
        Name = name;
        Schedule = schedule;
        Pricing = pricing;
        Capacity = capacity;
        _includedServices = [.. includedServices];
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
    /// Gets the schedule (date range) for the tour.
    /// </summary>
    public DateRange Schedule { get; private set; }

    /// <summary>
    /// Gets the pricing information for the tour.
    /// </summary>
    public TourPricing Pricing { get; private set; }

    /// <summary>
    /// Gets the capacity constraints for the tour.
    /// </summary>
    public TourCapacity Capacity { get; private set; }

    /// <summary>
    /// Gets the array of services included in the tour package (e.g. "Hotel", "Breakfast", "City Tour").
    /// </summary>
    public IReadOnlyList<string> IncludedServices => _includedServices.AsReadOnly();

    /// <summary>
    /// Gets the current count of customers across all confirmed bookings (principal and companions).
    /// </summary>
    public int CurrentCustomerCount =>
        _bookings
            .Where(b => b.Status == BookingStatus.Confirmed)
            .Sum(b => b.CompanionCustomer is null ? 1 : 2);

    /// <summary>
    /// Gets the number of available spots remaining.
    /// </summary>
    public int AvailableSpots => Capacity.MaxCustomers - CurrentCustomerCount;

    /// <summary>
    /// Gets whether the tour has reached its minimum capacity.
    /// </summary>
    public bool IsAtMinimumCapacity => CurrentCustomerCount >= Capacity.MinCustomers;

    /// <summary>
    /// Gets whether the tour is fully booked (at maximum capacity).
    /// </summary>
    public bool IsFullyBooked => CurrentCustomerCount >= Capacity.MaxCustomers;

    /// <summary>
    /// Gets the bookings for this tour.
    /// </summary>
    public IReadOnlyList<Booking> Bookings => _bookings.AsReadOnly();

    /// <summary>
    /// Creates a new instance of the <see cref="Tour"/> class with validation.
    /// </summary>
    /// <param name="identifier">The unique business identifier for the tour (e.g. "CUBA2024").</param>
    /// <param name="name">The descriptive name of the tour.</param>
    /// <param name="startDate">The start date of the tour.</param>
    /// <param name="endDate">The end date of the tour.</param>
    /// <param name="basePrice">The base price for a single room.</param>
    /// <param name="doubleRoomSupplementPrice">The additional price for a double room.</param>
    /// <param name="regularBikePrice">The price for renting a regular bike.</param>
    /// <param name="eBikePrice">The price for renting an e-bike.</param>
    /// <param name="currency">The currency for all prices.</param>
    /// <param name="minCustomers">The minimum number of customers required.</param>
    /// <param name="maxCustomers">The maximum number of customers allowed.</param>
    /// <param name="includedServices">The collection of services included in the tour package.</param>
    /// <returns>A Result containing the Tour if validation succeeds, or an error if validation fails.</returns>
    public static Result<Tour> Create(
        string identifier,
        string name,
        DateTime startDate,
        DateTime endDate,
        decimal basePrice,
        decimal doubleRoomSupplementPrice,
        decimal regularBikePrice,
        decimal eBikePrice,
        Currency currency,
        int minCustomers,
        int maxCustomers,
        IEnumerable<string> includedServices)
    {
        identifier = StringSanitizer.Sanitize(identifier);
        name = StringSanitizer.Sanitize(name);
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

        var scheduleResult = DateRange.Create(startDate, endDate);
        if (scheduleResult.IsFailure)
        {
            errors.Add(scheduleResult);
        }
        else if (scheduleResult.Value.DurationDays < ContractConstants.MinimumTourDurationDays)
        {
            errors.Add(TourErrors.DurationTooShort(ContractConstants.MinimumTourDurationDays,
                scheduleResult.Value.DurationDays));
        }

        var pricingResult =
            TourPricing.Create(basePrice, doubleRoomSupplementPrice, regularBikePrice, eBikePrice, currency);
        if (pricingResult.IsFailure)
        {
            errors.Add(pricingResult);
        }

        var capacityResult = TourCapacity.Create(minCustomers, maxCustomers);
        if (capacityResult.IsFailure)
        {
            errors.Add(capacityResult);
        }

        if (errors.HasErrors)
        {
            return errors.ToResult<Tour>();
        }

        return new Tour(
            identifier,
            name,
            scheduleResult.Value,
            pricingResult.Value,
            capacityResult.Value,
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
        var scheduleResult = DateRange.Create(startDate, endDate);
        if (scheduleResult.IsFailure)
        {
            return scheduleResult.ConvertError();
        }

        if (scheduleResult.Value.DurationDays < ContractConstants.MinimumTourDurationDays)
        {
            return TourErrors.DurationTooShortUpdate(ContractConstants.MinimumTourDurationDays,
                scheduleResult.Value.DurationDays);
        }

        Schedule = scheduleResult.Value;
        return Result.Ok();
    }

    /// <summary>
    /// Updates all pricing for the tour except for base price but including supplements, and bike rentals.
    /// </summary>
    /// <param name="doubleRoomSupplementPrice">The additional price for a double room.</param>
    /// <param name="regularBikePrice">The price for renting a regular bike.</param>
    /// <param name="eBikePrice">The price for renting an e-bike.</param>
    /// <param name="currency">The currency for all prices.</param>
    /// <returns>A Result indicating success or failure.</returns>
    public Result UpdatePricing(
        decimal doubleRoomSupplementPrice,
        decimal regularBikePrice,
        decimal eBikePrice,
        Currency currency)
    {
        var pricingResult = TourPricing.Create(
            Pricing.BasePrice,
            doubleRoomSupplementPrice,
            regularBikePrice,
            eBikePrice,
            currency);

        if (pricingResult.IsFailure)
        {
            return pricingResult.ConvertError();
        }

        Pricing = pricingResult.Value;
        return Result.Ok();
    }

    /// <summary>
    /// Updates the base price of the tour.
    /// </summary>
    /// <param name="price">The new base price per person.</param>
    /// <returns>A Result indicating success or failure.</returns>
    public Result UpdateBasePrice(decimal price)
    {
        var pricingResult = TourPricing.Create(
            price,
            Pricing.DoubleRoomSupplementPrice,
            Pricing.RegularBikePrice,
            Pricing.EBikePrice,
            Pricing.Currency);

        if (pricingResult.IsFailure)
        {
            return pricingResult.ConvertError();
        }

        Pricing = pricingResult.Value;
        return Result.Ok();
    }

    /// <summary>
    /// Updates the currency used for all tour prices.
    /// </summary>
    /// <param name="currency">The new currency.</param>
    public void UpdateCurrency(Currency currency)
    {
        var pricingResult = TourPricing.Create(
            Pricing.BasePrice,
            Pricing.DoubleRoomSupplementPrice,
            Pricing.RegularBikePrice,
            Pricing.EBikePrice,
            currency);

        if (pricingResult.IsSuccess)
        {
            Pricing = pricingResult.Value;
        }
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
    /// Updates the minimum and maximum customer capacity for the tour.
    /// </summary>
    /// <param name="minCustomers">The minimum number of customers required.</param>
    /// <param name="maxCustomers">The maximum number of customers allowed.</param>
    /// <returns>A Result indicating success or failure.</returns>
    public Result UpdateCapacity(int minCustomers, int maxCustomers)
    {
        var capacityResult = TourCapacity.Create(minCustomers, maxCustomers);
        if (capacityResult.IsFailure)
        {
            return capacityResult.ConvertError();
        }

        Capacity = capacityResult.Value;
        return Result.Ok();
    }

    /// <summary>
    /// Adds a new booking to this tour.
    /// Validates that BikeType.None is not used per US-11 requirement (None is a placeholder value, not a valid booking choice).
    /// </summary>
    /// <param name="principalCustomerId">The ID of the principal customer making the booking.</param>
    /// <param name="principalBikeType">The bike type selected by the principal customer.</param>
    /// <param name="companionCustomerId">The ID of the companion customer, if any.</param>
    /// <param name="companionBikeType">The bike type selected by the companion, if any.</param>
    /// <param name="roomType">The room type for the booking.</param>
    /// <param name="discountType">The type of discount to apply (None, Percentage, or Absolute).</param>
    /// <param name="discountAmount">The discount amount.</param>
    /// <param name="discountReason">Optional reason for the discount.</param>
    /// <param name="notes">Optional notes about the booking.</param>
    /// <returns>A Result containing the created booking if successful, or validation errors.</returns>
    public Result<Booking> AddBooking(
        Guid principalCustomerId,
        BikeType principalBikeType,
        Guid? companionCustomerId,
        BikeType? companionBikeType,
        RoomType roomType,
        DiscountType discountType,
        decimal discountAmount,
        string? discountReason,
        string? notes)
    {
        if (IsFullyBooked)
        {
            return TourErrors.TourFullyBooked(Capacity.MaxCustomers, CurrentCustomerCount).ConvertError<Booking>();
        }

        var principalValidation = ValidatePrincipalBikeType(principalBikeType);
        if (principalValidation.IsFailure)
        {
            return principalValidation.ConvertError<Booking>();
        }

        var companionValidation = ValidateCompanionBikeType(companionCustomerId, companionBikeType);
        if (companionValidation.IsFailure)
        {
            return companionValidation.ConvertError<Booking>();
        }

        if (!Enum.IsDefined(roomType))
        {
            return BookingErrors.InvalidRoomType(roomType).ConvertError<Booking>();
        }

        var principalBikePrice = GetBikePrice(principalBikeType);
        var principalCustomerResult =
            BookingCustomer.Create(principalCustomerId, principalBikeType, principalBikePrice);
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

        var discountResult = Discount.Create(discountType, discountAmount, discountReason);
        if (discountResult.IsFailure)
        {
            return discountResult.ConvertError<Discount, Booking>();
        }

        var result = Booking.Create(
            Id,
            Pricing.BasePrice,
            roomType,
            roomAdditionalCost,
            principalCustomerResult.Value,
            companionCustomer,
            discountResult.Value,
            notes);

        if (result.IsFailure)
        {
            return result;
        }

        var booking = result.Value;
        _bookings.Add(booking);
        return booking;
    }

    private static Result ValidatePrincipalBikeType(BikeType principalBikeType)
    {
        if (!Enum.IsDefined(principalBikeType))
        {
            return Result.Invalid(
                $"Invalid bike type: {principalBikeType}. Valid values are: {string.Join(", ", Enum.GetNames<BikeType>())}.",
                "principalBikeType",
                $"Invalid bike type: {principalBikeType}.");
        }

        if (principalBikeType == BikeType.None)
        {
            return Result.Invalid(
                "Bike type must be selected for principal customer. Please choose Regular or EBike.",
                "principalBikeType",
                "Bike type must be selected");
        }

        return Result.Ok();
    }

    private static Result ValidateCompanionBikeType(Guid? companionCustomerId, BikeType? companionBikeType)
    {
        if (!companionCustomerId.HasValue)
        {
            return Result.Ok();
        }

        if (companionBikeType is null or BikeType.None)
        {
            return Result.Invalid(
                "Bike type must be selected for companion. Please choose Regular or EBike.",
                "companionBikeType",
                "Bike type must be selected");
        }

        if (!Enum.IsDefined(companionBikeType.Value))
        {
            return Result.Invalid(
                $"Invalid bike type: {companionBikeType.Value}. Valid values are: {string.Join(", ", Enum.GetNames<BikeType>())}.",
                "companionBikeType",
                $"Invalid bike type: {companionBikeType.Value}.");
        }

        return Result.Ok();
    }

    private decimal GetBikePrice(BikeType bikeType) => bikeType switch
    {
        BikeType.Regular => Pricing.RegularBikePrice,
        BikeType.EBike => Pricing.EBikePrice,
        _ => throw new ArgumentOutOfRangeException(nameof(bikeType), bikeType, $"Invalid bike type: {bikeType}")
    };

    private Result<BookingCustomer>? CreateCompanionCustomer(Guid? companionCustomerId, BikeType? companionBikeType)
    {
        if (companionCustomerId is null)
        {
            return null;
        }

        var bikeType = companionBikeType ?? BikeType.None;
        var bikePrice = GetBikePrice(bikeType);
        return BookingCustomer.Create(companionCustomerId.Value, bikeType, bikePrice);
    }

    private decimal CalculateRoomAdditionalCost(RoomType roomType) => roomType switch
    {
        RoomType.SingleRoom => 0m,
        RoomType.DoubleRoom => Pricing.DoubleRoomSupplementPrice,
        _ => throw new ArgumentOutOfRangeException(nameof(roomType), roomType, $"Invalid room type: {roomType}")
    };

    /// <summary>
    /// Updates the payment status of a specific booking.
    /// </summary>
    /// <param name="bookingId">The ID of the booking to update.</param>
    /// <param name="paymentStatus">The payment status.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result UpdateBookingPaymentStatus(Guid bookingId, PaymentStatus paymentStatus)
    {
        var booking = _bookings.FirstOrDefault(b => b.Id == bookingId);
        if (booking is null)
        {
            return TourErrors.BookingNotFound(bookingId);
        }

        return booking.UpdatePaymentStatus(paymentStatus);
    }

    /// <summary>
    /// Cancels a booking.
    /// </summary>
    /// <param name="bookingId">The ID of the booking to cancel.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result CancelBooking(Guid bookingId)
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
    public Result ConfirmBooking(Guid bookingId)
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
    public Result CompleteBooking(Guid bookingId)
    {
        var booking = _bookings.FirstOrDefault(b => b.Id == bookingId);
        if (booking is null)
        {
            return TourErrors.BookingNotFound(bookingId);
        }

        return booking.Complete();
    }

    /// <summary>
    /// Updates the notes of a booking.
    /// </summary>
    /// <param name="bookingId">The ID of the booking to update.</param>
    /// <param name="notes">The updated notes.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result UpdateBookingNotes(Guid bookingId, string? notes)
    {
        var booking = _bookings.FirstOrDefault(b => b.Id == bookingId);
        if (booking is null)
        {
            return TourErrors.BookingNotFound(bookingId);
        }

        return booking.UpdateNotes(notes);
    }

    /// <summary>
    /// Updates the discount of a booking.
    /// </summary>
    /// <param name="bookingId">The ID of the booking to update.</param>
    /// <param name="discountType">The discount type.</param>
    /// <param name="discountAmount">The discount amount.</param>
    /// <param name="discountReason">The discount reason.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result UpdateBookingDiscount(
        Guid bookingId,
        DiscountType discountType,
        decimal discountAmount,
        string? discountReason)
    {
        var booking = _bookings.FirstOrDefault(b => b.Id == bookingId);
        if (booking is null)
        {
            return TourErrors.BookingNotFound(bookingId);
        }

        var discountResult = Discount.Create(discountType, discountAmount, discountReason);
        if (discountResult.IsFailure)
        {
            return discountResult.ConvertError();
        }

        return booking.UpdateDiscount(discountResult.Value);
    }

    /// <summary>
    /// Updates the details of a booking (room type, bikes, companion).
    /// </summary>
    /// <param name="bookingId">The ID of the booking to update.</param>
    /// <param name="roomType">The new room type.</param>
    /// <param name="principalBikeType">The new bike type for the principal customer.</param>
    /// <param name="companionCustomerId">The companion customer ID (null to remove the companion).</param>
    /// <param name="companionBikeType">The bike type for the companion (required if a companion present).</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result UpdateBookingDetails(
        Guid bookingId,
        RoomType roomType,
        BikeType principalBikeType,
        Guid? companionCustomerId,
        BikeType? companionBikeType)
    {
        var booking = _bookings.FirstOrDefault(b => b.Id == bookingId);
        if (booking is null)
        {
            return TourErrors.BookingNotFound(bookingId);
        }

        if (!Enum.IsDefined(roomType))
        {
            return BookingErrors.InvalidRoomType(roomType);
        }

        if (!Enum.IsDefined(principalBikeType))
        {
            return BookingErrors.InvalidBikeType(principalBikeType);
        }

        if (companionBikeType.HasValue && !Enum.IsDefined(companionBikeType.Value))
        {
            return BookingErrors.InvalidBikeType(companionBikeType.Value);
        }

        var principalBikePrice = GetBikePrice(principalBikeType);
        var principalCustomerResult = BookingCustomer.Create(
            booking.PrincipalCustomer.CustomerId,
            principalBikeType,
            principalBikePrice);

        if (principalCustomerResult.IsFailure)
        {
            return principalCustomerResult.ConvertError();
        }

        var companionCustomerResult = CreateCompanionCustomerForUpdate(
            booking.PrincipalCustomer.CustomerId,
            companionCustomerId,
            companionBikeType);

        if (companionCustomerResult.IsFailure)
        {
            return companionCustomerResult.ConvertError();
        }

        var roomAdditionalCost = CalculateRoomAdditionalCost(roomType);

        return booking.UpdateDetails(
            roomType,
            roomAdditionalCost,
            principalCustomerResult.Value,
            companionCustomerResult.Value,
            booking.Discount);
    }

    private Result<BookingCustomer?> CreateCompanionCustomerForUpdate(
        Guid principalCustomerId,
        Guid? companionCustomerId,
        BikeType? companionBikeType)
    {
        if (!companionCustomerId.HasValue)
        {
            return Result<BookingCustomer?>.Ok(null);
        }

        if (!companionBikeType.HasValue)
        {
            return Result<BookingCustomer?>.Invalid(
                detail: "Companion bike type is required when companion is present.",
                field: "companionBikeType",
                message: "Companion bike type is required.");
        }

        if (principalCustomerId == companionCustomerId.Value)
        {
            return TourErrors.PrincipalAndCompanionCannotBeSame().ConvertError<BookingCustomer?>();
        }

        var companionBikePrice = GetBikePrice(companionBikeType.Value);
        var companionCustomerResult = BookingCustomer.Create(
            companionCustomerId.Value,
            companionBikeType.Value,
            companionBikePrice);

        return companionCustomerResult.IsSuccess
            ? Result<BookingCustomer?>.Ok(companionCustomerResult.Value)
            : companionCustomerResult.ConvertError<BookingCustomer, BookingCustomer?>();
    }

    /// <summary>
    /// Removes a booking from this tour.
    /// Only pending bookings can be removed (INV-TOUR-019).
    /// </summary>
    /// <param name="bookingId">The ID of the booking to remove.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result RemoveBooking(Guid bookingId)
    {
        var booking = _bookings.FirstOrDefault(b => b.Id == bookingId);
        if (booking is null)
        {
            return TourErrors.BookingNotFound(bookingId);
        }

        if (booking.Status != BookingStatus.Pending)
        {
            return TourErrors.CannotRemoveNonPendingBooking(bookingId, booking.Status);
        }

        _bookings.Remove(booking);
        return Result.Ok();
    }

    /// <summary>
    /// Checks if the tour can be deleted.
    /// Tours with confirmed bookings cannot be deleted (INV-TOUR-015).
    /// </summary>
    /// <returns>A result indicating whether the tour can be deleted.</returns>
    public Result CanBeDeleted()
    {
        var confirmedBookings = _bookings.Count(b => b.Status == BookingStatus.Confirmed);
        if (confirmedBookings > 0)
        {
            return TourErrors.CannotDeleteTourWithConfirmedBookings(confirmedBookings);
        }

        return Result.Ok();
    }

    /// <summary>
    /// Marks the tour for deletion after validating business rules.
    /// Tours with confirmed bookings cannot be deleted (INV-TOUR-015).
    /// This method can raise domain events for later processing.
    /// </summary>
    /// <returns>A result indicating success or validation errors.</returns>
    public Result Delete()
    {
        var canDeleteResult = CanBeDeleted();
        if (canDeleteResult.IsFailure)
        {
            return canDeleteResult;
        }

        return Result.Ok();
    }

    /// <summary>
    /// DO NOT USE. This constructor is required by Entity Framework Core for materialisation.
    /// </summary>
    [UsedImplicitly]
#pragma warning disable CS8618
    private Tour()
    {
    }
}
