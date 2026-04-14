using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.Tests.Shared.Behavior;

/// <summary>
/// Helper methods for creating and managing bookings in behavior tests.
/// Use these to reduce duplication in step definitions.
/// </summary>
public static class BookingTestHelpers
{
    /// <summary>
    /// Creates a single-customer booking with default values.
    /// </summary>
    public static Result<Booking> AddSingleCustomerBooking(Tour tour, SingleBookingOptions? options = null)
    {
        options ??= new SingleBookingOptions();
        var discount = options.Discount ?? BookingDiscountDefinition.None;

        return tour.AddBooking(TourBookingRequest.CreateSingle(
            options.CustomerId ?? Guid.CreateVersion7(),
            options.BikeType,
            options.RoomType,
            discount,
            options.SpecialRequests));
    }

    /// <summary>
    /// Creates a double-customer booking (principal + companion) with default values.
    /// </summary>
    public static Result<Booking> AddDoubleCustomerBooking(Tour tour, DoubleBookingOptions? options)
    {
        options ??= new DoubleBookingOptions();
        var discount = options.Discount ?? BookingDiscountDefinition.None;

        return tour.AddBooking(TourBookingRequest.CreateDouble(
            options.PrincipalCustomerId ?? Guid.CreateVersion7(),
            options.PrincipalBikeType,
            options.CompanionCustomerId ?? Guid.CreateVersion7(),
            options.CompanionBikeType,
            options.RoomType,
            discount,
            options.SpecialRequests));
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
            var customer = EntityBuilders.BuildCustomer(new CustomerOptions(
                FirstName: $"{namePrefix}{i}",
                LastName: $"Test{i}"));
            customers.Add(customer);

            var bookingResult = AddSingleCustomerBooking(tour, new SingleBookingOptions(CustomerId: customer.Id));
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
            var principal = EntityBuilders.BuildCustomer(new CustomerOptions(
                FirstName: $"{principalPrefix}{i}",
                LastName: $"Test{i}"));
            var companion = EntityBuilders.BuildCustomer(new CustomerOptions(
                FirstName: $"{companionPrefix}{i}",
                LastName: $"Test{i}"));
            customers.Add(principal);
            customers.Add(companion);

            var bookingResult = AddDoubleCustomerBooking(tour, new DoubleBookingOptions(
                PrincipalCustomerId: principal.Id,
                CompanionCustomerId: companion.Id));
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
            var customer = EntityBuilders.BuildCustomer(new CustomerOptions(
                FirstName: $"{namePrefix}{i}",
                LastName: $"Test{i}"));
            customers.Add(customer);

            AddSingleCustomerBooking(tour, new SingleBookingOptions(CustomerId: customer.Id));
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
            var customer = EntityBuilders.BuildCustomer(new CustomerOptions(
                FirstName: $"{namePrefix}{i}",
                LastName: $"Test{i}"));
            customers.Add(customer);

            var bookingResult = AddSingleCustomerBooking(tour, new SingleBookingOptions(CustomerId: customer.Id));
            if (bookingResult.IsSuccess)
            {
                bookingResult.Value.Cancel();
            }
        }

        return customers;
    }
}
