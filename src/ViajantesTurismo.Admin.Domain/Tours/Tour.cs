using ViajantesTurismo.Admin.Domain.Bookings;
using ViajantesTurismo.Common;
using ViajantesTurismo.Common.Monies;

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

    /// <summary>
    /// Initializes a new instance of the <see cref="Tour"/> class.
    /// </summary>
    /// <param name="identifier">The unique business identifier for the tour (e.g., "CUBA2024").</param>
    /// <param name="name">The descriptive name of the tour.</param>
    /// <param name="startDate">The start date of the tour.</param>
    /// <param name="endDate">The end date of the tour.</param>
    /// <param name="price">The base price of the tour per person.</param>
    /// <param name="singleRoomSupplementPrice">The additional price for a single room supplement.</param>
    /// <param name="regularBikePrice">The price for renting a regular bike.</param>
    /// <param name="eBikePrice">The price for renting an e-bike.</param>
    /// <param name="currency">The currency for all pricing.</param>
    /// <param name="includedServices">The collection of services included in the tour package.</param>
    public Tour(string identifier,
        string name,
        DateTime startDate,
        DateTime endDate,
        decimal price,
        decimal singleRoomSupplementPrice,
        decimal regularBikePrice,
        decimal eBikePrice,
        Currency currency,
        IEnumerable<string> includedServices)
    {
        if (endDate <= startDate)
        {
            throw new ArgumentException("End date must be after start date.", nameof(endDate));
        }

        Identifier = identifier;
        Name = name;
        StartDate = startDate;
        EndDate = endDate;
        Price = price;
        SingleRoomSupplementPrice = singleRoomSupplementPrice;
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
    /// Gets the base price of the tour per person.
    /// </summary>
    public decimal Price { get; private set; }

    /// <summary>
    /// Gets the additional price for a single room supplement.
    /// </summary>
    public decimal SingleRoomSupplementPrice { get; private set; }

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
    /// Updates the tour's basic information (identifier and name).
    /// </summary>
    /// <param name="identifier">The unique business identifier for the tour.</param>
    /// <param name="name">The descriptive name of the tour.</param>
    public void UpdateBasicInfo(string identifier, string name)
    {
        Identifier = identifier;
        Name = name;
    }

    /// <summary>
    /// Updates the tour's schedule (start and end dates).
    /// </summary>
    /// <param name="startDate">The start date of the tour.</param>
    /// <param name="endDate">The end date of the tour.</param>
    public void UpdateSchedule(DateTime startDate, DateTime endDate)
    {
        if (endDate <= startDate)
        {
            throw new ArgumentException("End date must be after start date.", nameof(endDate));
        }

        StartDate = startDate;
        EndDate = endDate;
    }

    /// <summary>
    /// Updates all pricing for the tour including base price, supplements, and bike rentals.
    /// </summary>
    /// <param name="price">The base price of the tour per person.</param>
    /// <param name="singleRoomSupplementPrice">The additional price for a single room supplement.</param>
    /// <param name="regularBikePrice">The price for renting a regular bike.</param>
    /// <param name="eBikePrice">The price for renting an e-bike.</param>
    /// <param name="currency">The currency for all pricing.</param>
    public void UpdatePricing(decimal price,
        decimal singleRoomSupplementPrice,
        decimal regularBikePrice,
        decimal eBikePrice,
        Currency currency)
    {
        Price = price;
        SingleRoomSupplementPrice = singleRoomSupplementPrice;
        RegularBikePrice = regularBikePrice;
        EBikePrice = eBikePrice;
        Currency = currency;
    }

    /// <summary>
    /// Updates the base price of the tour.
    /// </summary>
    /// <param name="price">The new base price per person.</param>
    public void UpdateBasePrice(decimal price)
    {
        Price = price;
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
        _includedServices = [..includedServices];
    }

    /// <summary>
    /// Adds a new booking to this tour.
    /// </summary>
    /// <param name="customerId">The ID of the customer making the booking.</param>
    /// <param name="companionId">The ID of the companion, if any.</param>
    /// <param name="totalPrice">The total price of the booking.</param>
    /// <param name="notes">Optional notes about the booking.</param>
    /// <returns>The created booking.</returns>
    public Booking AddBooking(int customerId, int? companionId, decimal totalPrice, string? notes)
    {
        var booking = new Booking(Id, customerId, companionId);
        booking.UpdatePrice(totalPrice);
        booking.UpdateNotes(notes);
        _bookings.Add(booking);
        return booking;
    }

    /// <summary>
    /// Updates the payment status of a booking.
    /// </summary>
    /// <param name="bookingId">The ID of the booking to update.</param>
    /// <param name="paymentStatus">The payment status.</param>
    public void UpdateBookingPaymentStatus(long bookingId, PaymentStatus paymentStatus)
    {
        var booking = _bookings.FirstOrDefault(b => b.Id == bookingId)
                      ?? throw new InvalidOperationException($"Booking with ID {bookingId} not found in this tour.");

        booking.UpdatePaymentStatus(paymentStatus);
    }

    /// <summary>
    /// Cancels a booking.
    /// </summary>
    /// <param name="bookingId">The ID of the booking to cancel.</param>
    public void CancelBooking(long bookingId)
    {
        var booking = _bookings.FirstOrDefault(b => b.Id == bookingId)
                      ?? throw new InvalidOperationException($"Booking with ID {bookingId} not found in this tour.");

        booking.Cancel();
    }

    /// <summary>
    /// Confirms a booking.
    /// </summary>
    /// <param name="bookingId">The ID of the booking to confirm.</param>
    public void ConfirmBooking(long bookingId)
    {
        var booking = _bookings.FirstOrDefault(b => b.Id == bookingId)
                      ?? throw new InvalidOperationException($"Booking with ID {bookingId} not found in this tour.");

        booking.Confirm();
    }

    /// <summary>
    /// Completes a booking.
    /// </summary>
    /// <param name="bookingId">The ID of the booking to complete.</param>
    public void CompleteBooking(long bookingId)
    {
        var booking = _bookings.FirstOrDefault(b => b.Id == bookingId)
                      ?? throw new InvalidOperationException($"Booking with ID {bookingId} not found in this tour.");

        booking.Complete();
    }

    /// <summary>
    /// Updates the price of a booking.
    /// </summary>
    /// <param name="bookingId">The ID of the booking to update.</param>
    /// <param name="newPrice">The new total price.</param>
    public void UpdateBookingPrice(long bookingId, decimal newPrice)
    {
        var booking = _bookings.FirstOrDefault(b => b.Id == bookingId)
                      ?? throw new InvalidOperationException($"Booking with ID {bookingId} not found in this tour.");

        booking.UpdatePrice(newPrice);
    }

    /// <summary>
    /// Updates the notes of a booking.
    /// </summary>
    /// <param name="bookingId">The ID of the booking to update.</param>
    /// <param name="notes">The updated notes.</param>
    public void UpdateBookingNotes(long bookingId, string? notes)
    {
        var booking = _bookings.FirstOrDefault(b => b.Id == bookingId)
                      ?? throw new InvalidOperationException($"Booking with ID {bookingId} not found in this tour.");

        booking.UpdateNotes(notes);
    }

    /// <summary>
    /// Removes a booking from this tour.
    /// </summary>
    /// <param name="bookingId">The ID of the booking to remove.</param>
    public void RemoveBooking(long bookingId)
    {
        var booking = _bookings.FirstOrDefault(b => b.Id == bookingId)
                      ?? throw new InvalidOperationException($"Booking with ID {bookingId} not found in this tour.");

        _bookings.Remove(booking);
    }

    /// <summary>
    /// DO NOT USE. This constructor is required by Entity Framework Core for materialization.
    /// </summary>
#pragma warning disable CS8618
    public Tour()
    {
    }
}
