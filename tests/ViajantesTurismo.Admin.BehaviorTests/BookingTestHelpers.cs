using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.BehaviorTests;

/// <summary>
/// Helper methods for creating and managing bookings in behavior tests.
/// Use these to reduce duplication in step definitions.
/// </summary>
public static class BookingTestHelpers
{
    /// <summary>
    /// Creates a single-customer booking with default values.
    /// </summary>
    public static Result<Booking> AddSingleCustomerBooking(
        Tour tour,
        Guid? customerId = null,
        BikeType bikeType = BikeType.Regular,
        RoomType roomType = RoomType.DoubleOccupancy,
        DiscountType discountType = DiscountType.None,
        decimal discountPercentage = 0m,
        string? notes = null,
        string? specialRequests = null)
    {
        return tour.AddBooking(
            customerId ?? Guid.CreateVersion7(),
            bikeType,
            null,
            null,
            roomType,
            discountType,
            discountPercentage,
            notes,
            specialRequests);
    }

    /// <summary>
    /// Creates a double-customer booking (principal + companion) with default values.
    /// </summary>
    public static Result<Booking> AddDoubleCustomerBooking(
        Tour tour,
        Guid? principalCustomerId = null,
        Guid? companionCustomerId = null,
        BikeType principalBikeType = BikeType.Regular,
        BikeType companionBikeType = BikeType.Regular,
        RoomType roomType = RoomType.DoubleOccupancy,
        DiscountType discountType = DiscountType.None,
        decimal discountPercentage = 0m,
        string? notes = null,
        string? specialRequests = null)
    {
        return tour.AddBooking(
            principalCustomerId ?? Guid.CreateVersion7(),
            principalBikeType,
            companionCustomerId ?? Guid.CreateVersion7(),
            companionBikeType,
            roomType,
            discountType,
            discountPercentage,
            notes,
            specialRequests);
    }

    /// <summary>
    /// Creates multiple confirmed single-customer bookings for a tour.
    /// Returns the list of customers created for reference.
    /// </summary>
    public static IReadOnlyList<Customer> CreateConfirmedSingleBookings(Tour tour, int count, string namePrefix = "Customer")
    {
        var customers = new List<Customer>();

        for (var i = 0; i < count; i++)
        {
            var customer = EntityBuilders.BuildCustomer(firstName: $"{namePrefix}{i}", lastName: $"Test{i}");
            customers.Add(customer);

            var bookingResult = AddSingleCustomerBooking(tour, customer.Id);
            if (bookingResult.IsSuccess)
            {
                bookingResult.Value.Confirm();
            }
        }

        return customers;
    }

    /// <summary>
    /// Creates multiple confirmed double-customer bookings for a tour.
    /// Returns the list of customers created (both principals and companions).
    /// </summary>
    public static IReadOnlyList<Customer> CreateConfirmedDoubleBookings(
        Tour tour,
        int count,
        string principalPrefix = "Principal",
        string companionPrefix = "Companion")
    {
        var customers = new List<Customer>();

        for (var i = 0; i < count; i++)
        {
            var principal = EntityBuilders.BuildCustomer(firstName: $"{principalPrefix}{i}", lastName: $"Test{i}");
            var companion = EntityBuilders.BuildCustomer(firstName: $"{companionPrefix}{i}", lastName: $"Test{i}");
            customers.Add(principal);
            customers.Add(companion);

            var bookingResult = AddDoubleCustomerBooking(tour, principal.Id, companion.Id);
            if (bookingResult.IsSuccess)
            {
                bookingResult.Value.Confirm();
            }
        }

        return customers;
    }

    /// <summary>
    /// Creates multiple pending single-customer bookings for a tour.
    /// </summary>
    public static IReadOnlyList<Customer> CreatePendingSingleBookings(Tour tour, int count, string namePrefix = "PendingCustomer")
    {
        var customers = new List<Customer>();

        for (var i = 0; i < count; i++)
        {
            var customer = EntityBuilders.BuildCustomer(firstName: $"{namePrefix}{i}", lastName: $"Test{i}");
            customers.Add(customer);

            AddSingleCustomerBooking(tour, customer.Id);
            // Bookings are pending by default, no need to confirm
        }

        return customers;
    }

    /// <summary>
    /// Creates multiple cancelled single-customer bookings for a tour.
    /// </summary>
    public static IReadOnlyList<Customer> CreateCancelledSingleBookings(Tour tour, int count, string namePrefix = "CancelledCustomer")
    {
        var customers = new List<Customer>();

        for (var i = 0; i < count; i++)
        {
            var customer = EntityBuilders.BuildCustomer(firstName: $"{namePrefix}{i}", lastName: $"Test{i}");
            customers.Add(customer);

            var bookingResult = AddSingleCustomerBooking(tour, customer.Id);
            if (bookingResult.IsSuccess)
            {
                bookingResult.Value.Cancel();
            }
        }

        return customers;
    }
}
