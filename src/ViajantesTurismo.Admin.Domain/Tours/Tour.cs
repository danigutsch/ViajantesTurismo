using ViajantesTurismo.Admin.Domain.Bookings;
using ViajantesTurismo.Common;
using ViajantesTurismo.Common.Monies;

namespace ViajantesTurismo.Admin.Domain.Tours;

/// <summary>
/// Represents a tour entity with details such as identifier, name, dates, pricing, currency, and included services.
/// </summary>
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
    /// Updates the tour with new information.
    /// </summary>
    /// <param name="identifier">The unique business identifier for the tour.</param>
    /// <param name="name">The descriptive name of the tour.</param>
    /// <param name="startDate">The start date of the tour.</param>
    /// <param name="endDate">The end date of the tour.</param>
    /// <param name="price">The base price of the tour per person.</param>
    /// <param name="singleRoomSupplementPrice">The additional price for a single room supplement.</param>
    /// <param name="regularBikePrice">The price for renting a regular bike.</param>
    /// <param name="eBikePrice">The price for renting an e-bike.</param>
    /// <param name="currency">The currency for all pricing.</param>
    /// <param name="includedServices">The collection of services included in the tour package.</param>
    public void Update(string identifier,
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
        booking.Update(totalPrice, notes, BookingStatus.Pending, PaymentStatus.Unpaid);
        _bookings.Add(booking);
        return booking;
    }

    /// <summary>
    /// Updates an existing booking.
    /// </summary>
    /// <param name="bookingId">The ID of the booking to update.</param>
    /// <param name="totalPrice">The total price of the booking.</param>
    /// <param name="notes">Optional notes about the booking.</param>
    /// <param name="status">The booking status.</param>
    /// <param name="paymentStatus">The payment status.</param>
    public void UpdateBooking(long bookingId, decimal totalPrice, string? notes, BookingStatus status, PaymentStatus paymentStatus)
    {
        var booking = _bookings.FirstOrDefault(b => b.Id == bookingId)
                      ?? throw new InvalidOperationException($"Booking with ID {bookingId} not found in this tour.");

        booking.Update(totalPrice, notes, status, paymentStatus);
    }

    /// <summary>
    /// Cancels a booking.
    /// </summary>
    /// <param name="bookingId">The ID of the booking to cancel.</param>
    public void CancelBooking(long bookingId)
    {
        var booking = _bookings.FirstOrDefault(b => b.Id == bookingId)
                      ?? throw new InvalidOperationException($"Booking with ID {bookingId} not found in this tour.");

        booking.Update(booking.TotalPrice, booking.Notes, BookingStatus.Cancelled, booking.PaymentStatus);
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
