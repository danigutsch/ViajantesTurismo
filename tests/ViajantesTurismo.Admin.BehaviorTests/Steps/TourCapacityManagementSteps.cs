using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Common.Monies;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
public sealed class TourCapacityManagementSteps(
    TourContext tourContext,
    CustomerContext customerContext,
    BookingContext bookingContext)
{
    [Given("I have valid tour details")]
    public void GivenIHaveValidTourDetails()
    {
        ContextHelpers.SetupValidTour(tourContext);
    }

    [Given("a tour exists with minimum (.*) and maximum (.*) customers")]
    public void GivenATourExistsWithMinimumAndMaximumCustomers(int minCustomers, int maxCustomers)
    {
        tourContext.Tour = Tour.Create(
            identifier: "TEST2024",
            name: "Test Tour",
            startDate: DateTime.UtcNow.AddMonths(1),
            endDate: DateTime.UtcNow.AddMonths(1).AddDays(7),
            basePrice: 2000.00m,
            singleRoomSupplementPrice: 500.00m,
            regularBikePrice: 100.00m,
            eBikePrice: 200.00m,
            currency: Currency.UsDollar,
            minCustomers: minCustomers,
            maxCustomers: maxCustomers,
            includedServices: ["Hotel", "Breakfast"]).Value;
    }

    [Given("the tour has (.*) confirmed bookings? with (.*) customers? each")]
    [Given("the tour has (.*) confirmed booking with (.*) customers?")]
    public void GivenTheTourHasConfirmedBookingsWithCustomersEach(int bookingCount, int customersPerBooking)
    {
        IReadOnlyList<Customer> customers = customersPerBooking switch
        {
            1 => BookingTestHelpers.CreateConfirmedSingleBookings(tourContext.Tour, bookingCount),
            2 => BookingTestHelpers.CreateConfirmedDoubleBookings(tourContext.Tour, bookingCount),
            _ => throw new ArgumentException($"Unsupported customer count: {customersPerBooking}")
        };

        foreach (var customer in customers)
        {
            customerContext.Customers.Add(customer);
        }
    }

    [Given("the tour has (.*) pending bookings? with (.*) customers? each")]
    [Given("the tour has (.*) pending booking with (.*) customer")]
    public void GivenTheTourHasPendingBookingsWithCustomersEach(int bookingCount, int customersPerBooking)
    {
        var customers = BookingTestHelpers.CreatePendingSingleBookings(tourContext.Tour, bookingCount);
        foreach (var customer in customers)
        {
            customerContext.Customers.Add(customer);
        }
    }

    [Given("the tour has (.*) cancelled bookings? with (.*) customers? each")]
    [Given("the tour has (.*) cancelled booking with (.*) customer")]
    public void GivenTheTourHasCancelledBookingsWithCustomersEach(int bookingCount, int customersPerBooking)
    {
        var customers = BookingTestHelpers.CreateCancelledSingleBookings(tourContext.Tour, bookingCount);
        foreach (var customer in customers)
        {
            customerContext.Customers.Add(customer);
        }
    }

    [Given("a third customer exists")]
    [Given("a fourth customer exists")]
    public void GivenAFourthCustomerExists()
    {
        var customer =
            EntityBuilders.BuildCustomer(firstName: $"AdditionalCustomer{customerContext.Customers.Count}", "Test");
        customerContext.Customers.Add(customer);
    }

    [When("I create a tour with minimum (.*) and maximum (.*) customers")]
    [When("I try to create a tour with minimum (.*) and maximum (.*) customers")]
    public void WhenICreateATourWithMinimumAndMaximumCustomers(int minCustomers, int maxCustomers)
    {
        var result = Tour.Create(
            identifier: tourContext.Identifier,
            name: tourContext.Name,
            startDate: tourContext.StartDate,
            endDate: tourContext.EndDate,
            basePrice: tourContext.BasePrice,
            singleRoomSupplementPrice: tourContext.SingleRoomSupplementPrice,
            regularBikePrice: tourContext.RegularBikePrice,
            eBikePrice: tourContext.EBikePrice,
            currency: Currency.UsDollar,
            minCustomers: minCustomers,
            maxCustomers: maxCustomers,
            includedServices: ["Hotel", "Breakfast"]);

        tourContext.CreationResult = result;

        if (result.IsSuccess)
        {
            tourContext.Tour = result.Value;
        }
    }

    [When("I update the capacity to minimum (.*) and maximum (.*)")]
    public void WhenIUpdateTheCapacityToMinimumAndMaximum(int minCustomers, int maxCustomers)
    {
        var result = tourContext.Tour.UpdateCapacity(minCustomers, maxCustomers);
        tourContext.CapacityUpdateResult = result;
    }

    [When("I try to update the capacity to minimum (.*) and maximum (.*)")]
    public void WhenITryToUpdateTheCapacityToMinimumAndMaximum(int minCustomers, int maxCustomers)
    {
        var result = tourContext.Tour.UpdateCapacity(minCustomers, maxCustomers);
        tourContext.CapacityUpdateResult = result;
    }

    [When("I try to add a booking for the third customer")]
    public void WhenITryToAddABookingForTheThirdCustomer()
    {
        var customer = customerContext.Customers.ElementAt(2);

        var result = tourContext.Tour.AddBooking(
            customer.Id,
            BikeType.Regular,
            null,
            null,
            RoomType.DoubleOccupancy,
            DiscountType.None,
            0m,
            null,
            null);

        Assert.True(result.IsSuccess);
        result.Value.Confirm();

        bookingContext.BookingCreationResult = result;
    }

    [When("I try to add a booking for a fourth customer")]
    public void WhenITryToAddABookingForAFourthCustomer()
    {
        if (customerContext.Customers.Count < 4)
        {
            var newCustomer =
                EntityBuilders.BuildCustomer(firstName: $"AdditionalCustomer{customerContext.Customers.Count}", lastName: "Test");
            customerContext.Customers.Add(newCustomer);
        }

        var customer = customerContext.Customers.ElementAt(3);

        var result = tourContext.Tour.AddBooking(
            customer.Id,
            BikeType.Regular,
            null,
            null,
            RoomType.DoubleOccupancy,
            DiscountType.None,
            0m,
            null,
            null);

        bookingContext.BookingCreationResult = result;
    }

    [Then("the minimum capacity should be (.*)")]
    public void ThenTheMinimumCapacityShouldBe(int expected)
    {
        Assert.Equal(expected, tourContext.Tour.Capacity.MinCustomers);
    }

    [Then("the maximum capacity should be (.*)")]
    public void ThenTheMaximumCapacityShouldBe(int expected)
    {
        Assert.Equal(expected, tourContext.Tour.Capacity.MaxCustomers);
    }

    [Then("the capacity update should succeed")]
    public void ThenTheCapacityUpdateShouldSucceed()
    {
        Assert.NotNull(tourContext.CapacityUpdateResult);
        Assert.True(tourContext.CapacityUpdateResult.Value.IsSuccess);
    }

    [Then("the capacity update should fail")]
    public void ThenTheCapacityUpdateShouldFail()
    {
        Assert.NotNull(tourContext.CapacityUpdateResult);
        Assert.True(tourContext.CapacityUpdateResult.Value.IsFailure);
    }

    [Then("the error should indicate cannot reduce capacity below current bookings")]
    public void ThenTheErrorShouldIndicateCannotReduceCapacityBelowCurrentBookings()
    {
        Assert.NotNull(tourContext.CapacityUpdateResult);
        Assert.True(tourContext.CapacityUpdateResult.Value.IsFailure);

        var error = tourContext.CapacityUpdateResult.Value.ErrorDetails;
        Assert.NotNull(error);
        Assert.Contains("capacity", error.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("current", error.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("booking", error.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Then("the tour creation should fail")]
    public void ThenTheTourCreationShouldFail()
    {
        Assert.NotNull(tourContext.CreationResult);
        Assert.True(tourContext.CreationResult.Value.IsFailure);
    }

    [Then("the error should indicate max must be at least min")]
    public void ThenTheErrorShouldIndicateMaxMustBeAtLeastMin()
    {
        Assert.NotNull(tourContext.CreationResult);
        Assert.True(tourContext.CreationResult.Value.IsFailure);
        var errors = tourContext.CreationResult.Value.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? [];
        var combinedErrors = string.Join(" ", allErrors);
        Assert.Contains("maximum", combinedErrors, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("minimum", combinedErrors, StringComparison.OrdinalIgnoreCase);
    }

    [Then("the error should indicate minimum must be at least 1")]
    public void ThenTheErrorShouldIndicateMinimumMustBeAtLeast()
    {
        Assert.NotNull(tourContext.CreationResult);
        Assert.True(tourContext.CreationResult.Value.IsFailure);
        var errors = tourContext.CreationResult.Value.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? [];
        var combinedErrors = string.Join(" ", allErrors);
        Assert.Contains("Minimum", combinedErrors, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("1", combinedErrors, StringComparison.Ordinal);
    }

    [Then("the error should indicate maximum cannot exceed 20")]
    public void ThenTheErrorShouldIndicateMaximumCannotExceed()
    {
        Assert.NotNull(tourContext.CreationResult);
        Assert.True(tourContext.CreationResult.Value.IsFailure);
        var errors = tourContext.CreationResult.Value.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? [];
        var combinedErrors = string.Join(" ", allErrors);
        Assert.Contains("Maximum", combinedErrors, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("20", combinedErrors, StringComparison.Ordinal);
    }

    [Then("the error should indicate the tour is fully booked")]
    public void ThenTheErrorShouldIndicateTheTourIsFullyBooked()
    {
        Assert.NotNull(bookingContext.BookingCreationResult);
        Assert.True(bookingContext.BookingCreationResult.Value.IsFailure);
        var error = bookingContext.BookingCreationResult.Value.ErrorDetails;
        Assert.NotNull(error);
        Assert.Contains("fully booked", error.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Then("the current customer count should be (.*)")]
    public void ThenTheCurrentCustomerCountShouldBe(int expected)
    {
        Assert.Equal(expected, tourContext.Tour.CurrentCustomerCount);
    }

    [Then("the available spots should be (.*)")]
    public void ThenTheAvailableSpotsShouldBe(int expected)
    {
        Assert.Equal(expected, tourContext.Tour.AvailableSpots);
    }

    [Then("the tour should not be at minimum capacity")]
    public void ThenTheTourShouldNotBeAtMinimumCapacity()
    {
        Assert.False(tourContext.Tour.IsAtMinimumCapacity);
    }

    [Then("the tour should not be fully booked")]
    public void ThenTheTourShouldNotBeFullyBooked()
    {
        Assert.False(tourContext.Tour.IsFullyBooked);
    }
}
