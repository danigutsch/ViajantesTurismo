using JetBrains.Annotations;
using ViajantesTurismo.Admin.Domain.Payments;
using ViajantesTurismo.Admin.Domain.Shared;
using PandaBear.Domain;
using PandaBear.Results;
using PandaBear.InputNormalization;

namespace ViajantesTurismo.Admin.Domain.Tours;

/// <summary>
/// Represents a booking made by a customer for a tour.
/// </summary>
/// <remarks>
/// Part of the Tour aggregate. In production code, modify through <c>Tour</c> methods only (e.g., <c>Tour.ConfirmBooking()</c>, <c>Tour.UpdateBookingNotes()</c>).
/// </remarks>
[GenerateModelSupport(Identity = true)]
public sealed partial class Booking : IEntity<Guid>
{

    /// <summary>
    /// Initialises a new instance of the <see cref="Booking"/> class.
    /// </summary>
    /// <param name="tourId">The ID of the tour that was booked.</param>
    /// <param name="basePrice">The base price for double occupancy (not per person).</param>
    /// <param name="room">The room selection and associated room cost for the booking.</param>
    /// <param name="principalCustomer">The principal customer's booking details.</param>
    /// <param name="companionCustomer">The companion customer's booking details, if any.</param>
    /// <param name="discount">The discount applied to this booking.</param>
    /// <param name="notes">Optional notes about the booking.</param>
    /// <param name="createdAt">The captured UTC booking creation instant.</param>
    private Booking(
        Guid tourId,
        decimal basePrice,
        BookingRoom room,
        BookingCustomer principalCustomer,
        BookingCustomer? companionCustomer,
        Discount discount,
        string? notes,
        DateTime createdAt)
    {
        Id = Guid.CreateVersion7();
        TourId = tourId;
        BasePrice = basePrice;
        RoomType = room.RoomType;
        RoomAdditionalCost = room.AdditionalCost;
        PrincipalCustomer = principalCustomer;
        CompanionCustomer = companionCustomer;
        Discount = discount;
        Notes = notes;
        BookingDate = createdAt;
    }

    /// <summary>
    /// DO NOT USE. This constructor is required by Entity Framework Core for materialisation.
    /// </summary>
    [UsedImplicitly]
    private Booking()
    {
    }

    /// <summary>
    /// Gets the unique booking identifier.
    /// </summary>
    public Guid Id
    {
        get; private init;
    }

    /// <summary>
    /// The ID of the tour that was booked.
    /// </summary>
    public Guid TourId
    {
        get; private init;
    }

    /// <summary>
    /// The base price per person at the time of booking.
    /// </summary>
    public decimal BasePrice
    {
        get;
    }

    /// <summary>
    /// The room type for the booking (shared by all participants).
    /// </summary>
    public RoomType RoomType
    {
        get; private set;
    }

    /// <summary>
    /// The additional cost for the room (e.g. single room supplement).
    /// </summary>
    public decimal RoomAdditionalCost
    {
        get; private set;
    }

    /// <summary>
    /// The principal customer's booking details including bike selection and price.
    /// </summary>
    public BookingCustomer PrincipalCustomer { get; private set; } = default!;

    /// <summary>
    /// The companion customer's booking details including bike selection and price, if any.
    /// </summary>
    public BookingCustomer? CompanionCustomer
    {
        get; private set;
    }

    /// <summary>
    /// The discount applied to this booking.
    /// </summary>
    public Discount Discount { get; private set; } = default!;

    /// <summary>
    /// The date when the booking was made.
    /// </summary>
    public DateTime BookingDate
    {
        get; private init;
    }

    /// <summary>
    /// The start date of the tour.
    /// </summary>
    public BookingStatus Status { get; private set; } = BookingStatus.Pending;

    /// <summary>
    /// Gets whether the booking is pending and can still be removed from the tour.
    /// </summary>
    public bool IsPending => Status == BookingStatus.Pending;

    /// <summary>
    /// Gets whether the booking is confirmed and should count toward tour capacity.
    /// </summary>
    public bool IsConfirmed => Status == BookingStatus.Confirmed;

    /// <summary>
    /// Gets the number of customers represented by this booking.
    /// </summary>
    public int ParticipantCount => CompanionCustomer is null ? 1 : 2;

    /// <summary>
    /// The payment status of the booking (calculated from payments).
    /// </summary>
    public PaymentStatus PaymentStatus
    {
        get
        {
            if (FinancialHistory?.ApprovedSchedule is null)
            {
                return PaymentStatus.Unpaid;
            }
            var amountPaid = AmountPaid;
            var totalPrice = FinancialHistory.ApprovedSchedule.Obligation.Amount;

            return amountPaid switch
            {
                _ when totalPrice == 0 => PaymentStatus.Paid,
                0 => PaymentStatus.Unpaid,
                _ when amountPaid >= totalPrice => PaymentStatus.Paid,
                _ => PaymentStatus.PartiallyPaid
            };
        }
    }

    /// <summary>
    /// The subtotal before discount (base price + room cost + bike prices).
    /// </summary>
    public decimal Subtotal => BookingPriceBreakdown.CalculateSubtotal(BasePrice, RoomAdditionalCost, PrincipalCustomer.BikePrice, CompanionCustomer?.BikePrice ?? 0m);

    /// <summary>
    /// The total price of the booking (subtotal minus discount).
    /// </summary>
    public decimal TotalPrice => Subtotal - Discount.CalculateDiscountAmount(Subtotal);

    /// <summary>
    /// Additional notes for the booking.
    /// </summary>
    public string? Notes
    {
        get; private set;
    }

    /// <summary>
    /// The total amount paid so far.
    /// </summary>
    public decimal AmountPaid => FinancialHistory?.AllocatedEuroCredit ?? 0m;

    /// <summary>
    /// The remaining balance to be paid.
    /// </summary>
    public decimal RemainingBalance => FinancialHistory?.OutstandingEuro ?? 0m;

    /// <summary>Gets protected, immutable financial history loaded through the Tour aggregate.</summary>
    public BookingFinancialHistory? FinancialHistory
    {
        get; private set;
    }

    /// <summary>Gets whether deleting the booking would erase financial history.</summary>
    public bool HasFinancialHistory => FinancialHistory is not null;

    /// <summary>Attaches loaded financial history to this booking; throws when the history belongs to another booking.</summary>
    internal void RestoreFinancialHistory(BookingFinancialHistory history)
    {
        if (history.BookingId != Id)
        {
            throw new InvalidOperationException("Financial history belongs to another booking.");
        }
        FinancialHistory = history;
    }

    /// <summary>
    /// Gets whether the booked services (room, bikes, companion, and discount) can still be changed;
    /// cancelled and completed bookings are closed to such changes.
    /// </summary>
    public bool CanModifyBookedServices => Status is not (BookingStatus.Cancelled or BookingStatus.Completed);

    /// <summary>Gets whether confirming the booking would change its status; only pending bookings qualify.</summary>
    public bool CanBeConfirmed => Status == BookingStatus.Pending;

    /// <summary>Gets whether completing the booking would change its status; only confirmed bookings qualify.</summary>
    public bool CanBeCompleted => Status == BookingStatus.Confirmed;

    /// <summary>Gets whether cancelling the booking would change its status; pending and confirmed bookings qualify.</summary>
    public bool CanBeCancelled => Status is BookingStatus.Pending or BookingStatus.Confirmed;

    /// <summary>
    /// Gets whether the booking can be removed from its tour; only a pending booking without financial
    /// history qualifies.
    /// </summary>
    public bool CanBeRemoved => IsPending && !HasFinancialHistory;

    /// <summary>
    /// Creates a new booking with validation and sanitisation.
    /// </summary>
    /// <param name="tourId">The ID of the tour that was booked.</param>
    /// <param name="basePrice">The base price for double occupancy (not per person).</param>
    /// <param name="room">The room selection and associated room cost for the booking.</param>
    /// <param name="principalCustomer">The principal customer's booking details.</param>
    /// <param name="companionCustomer">The companion customer's booking details, if any.</param>
    /// <param name="discount">The discount applied to this booking.</param>
    /// <param name="notes">Optional notes about the booking.</param>
    /// <param name="createdAt">The captured UTC booking creation instant.</param>
    /// <returns>A Result containing the booking if successful, or validation errors.</returns>
    internal static Result<Booking> Create(
        Guid tourId,
        decimal basePrice,
        BookingRoom room,
        BookingCustomer principalCustomer,
        BookingCustomer? companionCustomer,
        Discount discount,
        string? notes,
        DateTime createdAt)
    {
        ArgumentNullException.ThrowIfNull(principalCustomer);
        ArgumentNullException.ThrowIfNull(discount);

        basePrice = NumericSanitizer.SanitizePrice(basePrice);
        var roomType = room.RoomType;
        var roomAdditionalCost = NumericSanitizer.SanitizePrice(room.AdditionalCost);
        notes = StringSanitizer.SanitizeNotes(notes);

        var errors = new ValidationErrors();

        if (!Enum.IsDefined(roomType))
        {
            errors.Add(BookingErrors.InvalidRoomType(roomType));
        }

        var priceComponents = BookingPriceBreakdown.ValidateComponents(basePrice, roomAdditionalCost, principalCustomer.BikePrice, companionCustomer?.BikePrice ?? 0m);
        if (priceComponents.IsFailure)
        {
            errors.Add(priceComponents);
        }

        if (notes?.Length > AdminDomainLimits.MaxBookingNotesLength)
        {
            errors.Add(BookingErrors.InvalidNotesLength(AdminDomainLimits.MaxBookingNotesLength, notes.Length));
        }

        if (companionCustomer is not null && principalCustomer.CustomerId == companionCustomer.CustomerId)
        {
            errors.Add(BookingErrors.PrincipalAndCompanionCannotBeSame());
        }

        if (errors.HasErrors)
        {
            return errors.ToResult<Booking>();
        }

        var pricing = BookingPriceBreakdown.Create(basePrice, roomAdditionalCost, principalCustomer.BikePrice, companionCustomer?.BikePrice ?? 0m, discount);
        if (pricing.IsFailure)
        {
            return pricing.ConvertError<BookingPriceBreakdown, Booking>();
        }

        return Result.Ok(new Booking(tourId, basePrice, new BookingRoom(roomType, roomAdditionalCost), principalCustomer, companionCustomer,
            discount, notes, createdAt));
    }


    /// <summary>
    /// Confirms the booking, setting its status to Confirmed. Is idempotent.
    /// </summary>
    /// <returns>A result indicating success or failure.</returns>
    internal Result Confirm()
    {
        return Status switch
        {
            BookingStatus.Confirmed => Result.Ok(),
            BookingStatus.Cancelled or BookingStatus.Completed => BookingErrors.InvalidStatusTransition(Status,
                BookingStatus.Confirmed),
            BookingStatus.Pending => ConfirmInternal(),
            _ => throw new InvalidOperationException($"Invalid booking status: {Status}")
        };

        Result ConfirmInternal()
        {
            Status = BookingStatus.Confirmed;
            return Result.Ok();
        }
    }

    /// <summary>
    /// Cancels the booking, setting its status to Cancelled. Is idempotent.
    /// </summary>
    /// <returns>A result indicating success or failure.</returns>
    internal Result Cancel()
    {
        return Status switch
        {
            BookingStatus.Cancelled => Result.Ok(),
            BookingStatus.Completed => BookingErrors.InvalidStatusTransition(Status, BookingStatus.Cancelled),
            BookingStatus.Pending or BookingStatus.Confirmed => CancelInternal(),
            _ => throw new InvalidOperationException($"Invalid booking status: {Status}")
        };

        Result CancelInternal()
        {
            Status = BookingStatus.Cancelled;
            return Result.Ok();
        }
    }

    /// <summary>
    /// Marks the booking as completed. Is idempotent.
    /// </summary>
    /// <returns>A result indicating success or failure.</returns>
    internal Result Complete()
    {
        return Status switch
        {
            BookingStatus.Completed => Result.Ok(),
            BookingStatus.Cancelled => BookingErrors.InvalidStatusTransition(Status, BookingStatus.Completed),
            BookingStatus.Pending => BookingErrors.CannotCompleteWithoutConfirmation(),
            BookingStatus.Confirmed => CompleteInternal(),
            _ => throw new InvalidOperationException($"Invalid booking status: {Status}")
        };

        Result CompleteInternal()
        {
            Status = BookingStatus.Completed;
            return Result.Ok();
        }
    }


    /// <summary>
    /// Updates the notes for the booking.
    /// </summary>
    /// <param name="notes">The updated notes.</param>
    /// <returns>A result indicating success or failure.</returns>
    internal Result UpdateNotes(string? notes)
    {
        notes = StringSanitizer.SanitizeNotes(notes);

        if (notes?.Length > AdminDomainLimits.MaxBookingNotesLength)
        {
            return BookingErrors.InvalidNotesLength(AdminDomainLimits.MaxBookingNotesLength, notes.Length);
        }

        Notes = notes;
        return Result.Ok();
    }

    /// <summary>
    /// Updates the discount for the booking.
    /// </summary>
    /// <param name="discount">The new discount.</param>
    /// <returns>A result indicating success or failure.</returns>
    internal Result UpdateDiscount(Discount discount)
    {
        ArgumentNullException.ThrowIfNull(discount);

        if (!CanModifyBookedServices)
        {
            return BookingErrors.CannotModifyCancelledOrCompletedBooking(Status);
        }

        var priceValidation = BookingPriceBreakdown.Create(BasePrice, RoomAdditionalCost, PrincipalCustomer.BikePrice, CompanionCustomer?.BikePrice ?? 0m, discount);
        if (priceValidation.IsFailure)
        {
            return priceValidation.ConvertError();
        }

        Discount = discount;
        return Result.Ok();
    }

    /// <summary>
    /// Updates the room details for the booking.
    /// </summary>
    /// <param name="roomType">The new room type.</param>
    /// <param name="roomAdditionalCost">The new room additional cost.</param>
    /// <param name="companionCustomer">The companion customer details (null if no companion).</param>
    /// <returns>A result indicating success or failure.</returns>
    internal Result UpdateRoom(RoomType roomType, decimal roomAdditionalCost, BookingCustomer? companionCustomer) =>
        UpdateServices(new BookingRoom(roomType, roomAdditionalCost), PrincipalCustomer, companionCustomer);

    /// <summary>Updates the companion only when the whole proposed service selection remains valid.</summary>
    internal Result UpdateCompanion(BookingCustomer? companionCustomer) =>
        UpdateServices(new BookingRoom(RoomType, RoomAdditionalCost), PrincipalCustomer, companionCustomer);

    /// <summary>Updates the principal bike only when the whole proposed service selection remains valid.</summary>
    internal Result UpdatePrincipalBike(BikeType principalBikeType, decimal principalBikePrice)
    {
        if (!CanModifyBookedServices)
        {
            return BookingErrors.CannotModifyCancelledOrCompletedBooking(Status);
        }

        var principal = BookingCustomer.Create(PrincipalCustomer.CustomerId, principalBikeType, principalBikePrice);
        return principal.IsFailure
            ? principal.ConvertError()
            : UpdateServices(new BookingRoom(RoomType, RoomAdditionalCost), principal.Value, CompanionCustomer);
    }

    /// <summary>Applies room and traveler changes atomically after validating their combined price.</summary>
    internal Result UpdateServices(BookingRoom room, BookingCustomer principalCustomer, BookingCustomer? companionCustomer)
    {
        ArgumentNullException.ThrowIfNull(principalCustomer);
        if (!CanModifyBookedServices)
        {
            return BookingErrors.CannotModifyCancelledOrCompletedBooking(Status);
        }

        var roomAdditionalCost = NumericSanitizer.SanitizePrice(room.AdditionalCost);
        var errors = new ValidationErrors();
        if (!Enum.IsDefined(room.RoomType))
        {
            errors.Add(BookingErrors.InvalidRoomType(room.RoomType));
        }

        if (companionCustomer is not null && principalCustomer.CustomerId == companionCustomer.CustomerId)
        {
            errors.Add(BookingErrors.PrincipalAndCompanionCannotBeSame());
        }

        if (room.RoomType == RoomType.SingleOccupancy && companionCustomer is not null)
        {
            errors.Add(BookingErrors.SingleRoomCannotHaveCompanion());
        }

        var pricing = BookingPriceBreakdown.Create(BasePrice, roomAdditionalCost, principalCustomer.BikePrice, companionCustomer?.BikePrice ?? 0m, Discount);
        if (pricing.IsFailure)
        {
            errors.Add(pricing);
        }

        if (errors.HasErrors)
        {
            return errors.ToResult();
        }

        RoomType = room.RoomType;
        RoomAdditionalCost = roomAdditionalCost;
        PrincipalCustomer = principalCustomer;
        CompanionCustomer = companionCustomer;
        return Result.Ok();
    }
}
