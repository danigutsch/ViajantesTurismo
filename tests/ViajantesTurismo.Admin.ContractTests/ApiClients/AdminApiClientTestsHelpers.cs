using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.ContractTests.ApiClients;

internal static class AdminApiClientTestsHelpers
{
    public const string TourJson = """
        {
          "id":"11111111-1111-1111-1111-111111111111",
          "identifier":"TOUR-1",
          "name":"First tour",
          "startDate":"2026-08-01T00:00:00",
          "endDate":"2026-08-08T00:00:00",
          "price":1200,
          "singleRoomSupplementPrice":300,
          "regularBikePrice":80,
          "eBikePrice":160,
          "currency":1,
          "includedServices":["Guide"],
          "minCustomers":4,
          "maxCustomers":12,
          "currentCustomerCount":3
        }
        """;

    public const string BookingJson = """
        {
          "id":"11111111-1111-1111-1111-111111111111",
          "tourId":"22222222-2222-2222-2222-222222222222",
          "tourIdentifier":"TOUR-1",
          "tourName":"First tour",
          "customerId":"33333333-3333-3333-3333-333333333333",
          "customerName":"Ada Lovelace",
          "roomType":0,
          "principalBikeType":1,
          "companionBikeType":null,
          "bookingDate":"2026-07-01T00:00:00",
          "status":1,
          "paymentStatus":0,
          "totalPrice":1200,
          "discountType":0,
          "discountAmount":0,
          "currency":1,
          "payments":[],
          "amountPaid":0,
          "remainingBalance":1200
        }
        """;

    public static CreateTourDto CreateTour() =>
        new()
        {
            Identifier = "TOUR-1",
            Name = "First tour",
            StartDate = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 8, 8, 0, 0, 0, DateTimeKind.Utc),
            Price = 1200,
            SingleRoomSupplementPrice = 300,
            RegularBikePrice = 80,
            EBikePrice = 160,
            Currency = CurrencyDto.Euro,
            IncludedServices = ["Guide"],
            MinCustomers = 4,
            MaxCustomers = 12
        };

    public static UpdateTourDto UpdateTour() =>
        new()
        {
            Identifier = "TOUR-1",
            Name = "Updated tour",
            StartDate = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 8, 8, 0, 0, 0, DateTimeKind.Utc),
            Price = 1300,
            SingleRoomSupplementPrice = 350,
            RegularBikePrice = 90,
            EBikePrice = 170,
            Currency = CurrencyDto.Euro,
            IncludedServices = ["Guide", "Hotel"],
            MinCustomers = 4,
            MaxCustomers = 12
        };

    public static CreateBookingDto CreateBooking() =>
        new()
        {
            TourId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            PrincipalCustomerId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            PrincipalBikeType = BikeTypeDto.Regular,
            RoomType = RoomTypeDto.DoubleOccupancy
        };

    public static UpdateBookingDetailsDto UpdateBookingDetails() =>
        new()
        {
            RoomType = RoomTypeDto.DoubleOccupancy,
            PrincipalBikeType = BikeTypeDto.Regular
        };

    public static UpdateBookingDiscountDto UpdateBookingDiscount() =>
        new()
        {
            DiscountType = DiscountTypeDto.Percentage,
            DiscountAmount = 10,
            DiscountReason = "Returning customer"
        };

    public static CreatePaymentDto CreatePayment() =>
        new()
        {
            Amount = 200,
            PaymentDate = new DateTime(2026, 7, 2, 0, 0, 0, DateTimeKind.Utc),
            Method = PaymentMethodDto.BankTransfer
        };
}
