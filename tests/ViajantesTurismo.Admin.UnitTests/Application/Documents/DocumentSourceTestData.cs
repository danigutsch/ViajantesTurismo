using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.UnitTests.Application.Documents;

/// <summary>Builds booking and tour read models that feed the document source version.</summary>
internal static class DocumentSourceTestData
{
    /// <summary>Creates a confirmed booking read model for the given tour.</summary>
    /// <param name="bookingId">The booking identifier.</param>
    /// <param name="tourId">The tour identifier.</param>
    /// <param name="totalPrice">The total price.</param>
    /// <returns>The booking read model.</returns>
    public static GetBookingDto Booking(Guid bookingId, Guid tourId, decimal totalPrice = 1200m) => new()
    {
        Id = bookingId,
        TourId = tourId,
        TourIdentifier = "andes",
        TourName = "Andes",
        CustomerId = Guid.CreateVersion7(),
        CustomerName = "Ada",
        RoomType = RoomTypeDto.DoubleOccupancy,
        PrincipalBikeType = BikeTypeDto.Regular,
        BookingDate = TimeProvider.System.GetUtcNow().UtcDateTime,
        Status = BookingStatusDto.Confirmed,
        PaymentStatus = default,
        TotalPrice = totalPrice,
        DiscountType = default,
        DiscountAmount = 0m,
        Currency = default,
        AmountPaid = 0m,
        RemainingBalance = totalPrice
    };

    /// <summary>Creates a tour read model.</summary>
    /// <param name="tourId">The tour identifier.</param>
    /// <returns>The tour read model.</returns>
    public static GetTourDto Tour(Guid tourId) => new()
    {
        Id = tourId,
        Identifier = "andes",
        Name = "Andes",
        StartDate = DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime),
        EndDate = DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime.AddDays(7)),
        Price = 1200m,
        SingleRoomSupplementPrice = 0m,
        RegularBikePrice = 0m,
        EBikePrice = 0m,
        Currency = default,
        IncludedServices = ["Guide"],
        MinCustomers = 1,
        MaxCustomers = 10,
        CurrentCustomerCount = 1
    };
}
