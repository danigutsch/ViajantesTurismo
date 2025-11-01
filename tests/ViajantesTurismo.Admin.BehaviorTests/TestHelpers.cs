using ViajantesTurismo.Admin.Domain.Bookings;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Common.Monies;

namespace ViajantesTurismo.Admin.BehaviorTests;

/// <summary>
/// Provides shared helper methods for behavior tests.
/// </summary>
public static class TestHelpers
{
    /// <summary>
    /// Creates a test tour with default values.
    /// </summary>
    public static Tour CreateTestTour()
    {
        return new Tour(
            identifier: "TEST2024",
            name: "Test Tour",
            startDate: DateTime.UtcNow.AddMonths(1),
            endDate: DateTime.UtcNow.AddMonths(1).AddDays(7),
            price: 2000.00m,
            singleRoomSupplementPrice: 500.00m,
            regularBikePrice: 100.00m,
            eBikePrice: 200.00m,
            currency: Currency.UsDollar,
            includedServices: ["Hotel", "Breakfast"]);
    }

    /// <summary>
    /// Creates a test tour with specific dates.
    /// </summary>
    public static Tour CreateTestTourWithDates(DateTime startDate, DateTime endDate)
    {
        return new Tour(
            identifier: "TEST2024",
            name: "Test Tour",
            startDate: startDate,
            endDate: endDate,
            price: 2000.00m,
            singleRoomSupplementPrice: 500.00m,
            regularBikePrice: 100.00m,
            eBikePrice: 200.00m,
            currency: Currency.UsDollar,
            includedServices: ["Hotel", "Breakfast"]);
    }

    /// <summary>
    /// Creates a test tour with specific identifier and name.
    /// </summary>
    public static Tour CreateTestTourWithIdentifierAndName(string identifier, string name)
    {
        return new Tour(
            identifier: identifier,
            name: name,
            startDate: DateTime.UtcNow.AddMonths(1),
            endDate: DateTime.UtcNow.AddMonths(1).AddDays(7),
            price: 2000.00m,
            singleRoomSupplementPrice: 500.00m,
            regularBikePrice: 100.00m,
            eBikePrice: 200.00m,
            currency: Currency.UsDollar,
            includedServices: ["Hotel", "Breakfast"]);
    }

    /// <summary>
    /// Parses a booking status string to BookingStatus enum.
    /// </summary>
    public static BookingStatus ParseBookingStatus(string statusString)
    {
        return statusString switch
        {
            "Pending" => BookingStatus.Pending,
            "Confirmed" => BookingStatus.Confirmed,
            "Cancelled" => BookingStatus.Cancelled,
            "Completed" => BookingStatus.Completed,
            _ => throw new ArgumentException($"Unknown status: {statusString}", nameof(statusString))
        };
    }

    /// <summary>
    /// Parses a payment status string to PaymentStatus enum.
    /// </summary>
    public static PaymentStatus ParsePaymentStatus(string statusString)
    {
        return statusString switch
        {
            "Paid" => PaymentStatus.Paid,
            "PartiallyPaid" => PaymentStatus.PartiallyPaid,
            "Unpaid" => PaymentStatus.Unpaid,
            "Refunded" => PaymentStatus.Refunded,
            _ => throw new ArgumentException($"Unknown payment status: {statusString}", nameof(statusString))
        };
    }

    /// <summary>
    /// Parses a currency code string to Currency enum.
    /// </summary>
    public static Currency ParseCurrency(string currencyCode)
    {
        return currencyCode switch
        {
            "USD" => Currency.UsDollar,
            "EUR" => Currency.Euro,
            "BRL" => Currency.Real,
            _ => throw new ArgumentException($"Unknown currency: {currencyCode}", nameof(currencyCode))
        };
    }
}