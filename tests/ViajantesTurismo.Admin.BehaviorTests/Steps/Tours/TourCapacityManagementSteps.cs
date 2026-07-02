using ViajantesTurismo.Common.Monies;

using ViajantesTurismo.Admin.Domain.Shared;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps.Tours;

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
        tourContext.Tour = Tour.Create(new TourDefinition(
            "TEST2024",
            "Test Tour",
            DateTime.UtcNow.AddMonths(1),
            DateTime.UtcNow.AddMonths(1).AddDays(7),
            2000.00m,
            500.00m,
            100.00m,
            200.00m,
            Currency.UsDollar,
            minCustomers,
            maxCustomers,
            ["Hotel", "Breakfast"])).Value;
    }

    [Given("the tour has (.*) confirmed bookings? with (.*) customers? each")]
    [Given("the tour has (.*) confirmed booking with (.*) customers?")]
    public void GivenTheTourHasConfirmedBookingsWithCustomersEach(int bookingCount, int customersPerBooking)
    {
        var customers = customersPerBooking switch
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
        var customer = EntityBuilders.BuildCustomer(new CustomerOptions(
            FirstName: $"AdditionalCustomer{customerContext.Customers.Count}",
            LastName: "Test"));
        customerContext.Customers.Add(customer);
    }

    [When("I create a tour with minimum (.*) and maximum (.*) customers")]
    [When("I try to create a tour with minimum (.*) and maximum (.*) customers")]
    public void WhenICreateATourWithMinimumAndMaximumCustomers(int minCustomers, int maxCustomers)
    {
        var result = Tour.Create(new TourDefinition(
            tourContext.Identifier,
            tourContext.Name,
            tourContext.StartDate,
            tourContext.EndDate,
            tourContext.BasePrice,
            tourContext.SingleRoomSupplementPrice,
            tourContext.RegularBikePrice,
            tourContext.EBikePrice,
            Currency.UsDollar,
            minCustomers,
            maxCustomers,
            ["Hotel", "Breakfast"]));

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
        WhenIUpdateTheCapacityToMinimumAndMaximum(minCustomers, maxCustomers);
    }

    [When("I try to add a booking for the third customer")]
    public void WhenITryToAddABookingForTheThirdCustomer()
    {
        var customer = customerContext.Customers.ElementAt(2);

        var result = tourContext.Tour.AddBooking(new TourBookingRequest(
            customer.Id,
            BikeType.Regular,
            RoomType.DoubleOccupancy,
            DiscountType.None));

        TestAssert.True(result.IsSuccess);
        result.Value.Confirm();

        bookingContext.BookingCreationResult = result;
    }

    [When("I try to add a booking for a fourth customer")]
    public void WhenITryToAddABookingForAFourthCustomer()
    {
        if (customerContext.Customers.Count < 4)
        {
            var newCustomer = EntityBuilders.BuildCustomer(new CustomerOptions(
                FirstName: $"AdditionalCustomer{customerContext.Customers.Count}",
                LastName: "Test"));
            customerContext.Customers.Add(newCustomer);
        }

        var customer = customerContext.Customers.ElementAt(3);

        var result = tourContext.Tour.AddBooking(new TourBookingRequest(
            customer.Id,
            BikeType.Regular,
            RoomType.DoubleOccupancy,
            DiscountType.None));

        bookingContext.BookingCreationResult = result;
    }

    [Then("the minimum capacity should be (.*)")]
    public void ThenTheMinimumCapacityShouldBe(int expected)
    {
        TestAssert.Equal(expected, tourContext.Tour.Capacity.MinCustomers);
    }

    [Then("the maximum capacity should be (.*)")]
    public void ThenTheMaximumCapacityShouldBe(int expected)
    {
        TestAssert.Equal(expected, tourContext.Tour.Capacity.MaxCustomers);
    }

    [Then("the capacity update should succeed")]
    public void ThenTheCapacityUpdateShouldSucceed()
    {
        TestAssert.NotNull(tourContext.CapacityUpdateResult);
        TestAssert.True(tourContext.CapacityUpdateResult.Value.IsSuccess);
    }

    [Then("the capacity update should fail")]
    public void ThenTheCapacityUpdateShouldFail()
    {
        TestAssert.NotNull(tourContext.CapacityUpdateResult);
        TestAssert.True(tourContext.CapacityUpdateResult.Value.IsFailure);
    }

    [Then("the error should indicate cannot reduce capacity below current bookings")]
    public void ThenTheErrorShouldIndicateCannotReduceCapacityBelowCurrentBookings()
    {
        TestAssert.NotNull(tourContext.CapacityUpdateResult);
        TestAssert.True(tourContext.CapacityUpdateResult.Value.IsFailure);

        var error = tourContext.CapacityUpdateResult.Value.ErrorDetails;
        TestAssert.NotNull(error);
        TestAssert.Contains("capacity", error.Detail, StringComparison.OrdinalIgnoreCase);
        TestAssert.Contains("current", error.Detail, StringComparison.OrdinalIgnoreCase);
        TestAssert.Contains("booking", error.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Then("the tour creation should fail")]
    public void ThenTheTourCreationShouldFail()
    {
        TestAssert.NotNull(tourContext.CreationResult);
        TestAssert.True(tourContext.CreationResult.Value.IsFailure);
    }

    [Then("the error should indicate max must be at least min")]
    public void ThenTheErrorShouldIndicateMaxMustBeAtLeastMin()
    {
        TestAssert.NotNull(tourContext.CreationResult);
        TestAssert.True(tourContext.CreationResult.Value.IsFailure);
        var errors = tourContext.CreationResult.Value.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? [];
        var combinedErrors = string.Join(" ", allErrors);
        TestAssert.Contains("maximum", combinedErrors, StringComparison.OrdinalIgnoreCase);
        TestAssert.Contains("minimum", combinedErrors, StringComparison.OrdinalIgnoreCase);
    }

    [Then("the error should indicate minimum must be at least 1")]
    public void ThenTheErrorShouldIndicateMinimumMustBeAtLeast()
    {
        TestAssert.NotNull(tourContext.CreationResult);
        TestAssert.True(tourContext.CreationResult.Value.IsFailure);
        var errors = tourContext.CreationResult.Value.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? [];
        var combinedErrors = string.Join(" ", allErrors);
        TestAssert.Contains("Minimum", combinedErrors, StringComparison.OrdinalIgnoreCase);
        TestAssert.Contains("1", combinedErrors, StringComparison.Ordinal);
    }

    [Then("the error should indicate maximum cannot exceed 20")]
    public void ThenTheErrorShouldIndicateMaximumCannotExceed()
    {
        TestAssert.NotNull(tourContext.CreationResult);
        TestAssert.True(tourContext.CreationResult.Value.IsFailure);
        var errors = tourContext.CreationResult.Value.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? [];
        var combinedErrors = string.Join(" ", allErrors);
        TestAssert.Contains("Maximum", combinedErrors, StringComparison.OrdinalIgnoreCase);
        TestAssert.Contains("20", combinedErrors, StringComparison.Ordinal);
    }

    [Then("the error should indicate the tour is fully booked")]
    public void ThenTheErrorShouldIndicateTheTourIsFullyBooked()
    {
        TestAssert.NotNull(bookingContext.BookingCreationResult);
        TestAssert.True(bookingContext.BookingCreationResult.Value.IsFailure);
        var error = bookingContext.BookingCreationResult.Value.ErrorDetails;
        TestAssert.NotNull(error);
        TestAssert.Contains("fully booked", error.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Then("the current customer count should be (.*)")]
    public void ThenTheCurrentCustomerCountShouldBe(int expected)
    {
        TestAssert.Equal(expected, tourContext.Tour.CurrentCustomerCount);
    }

    [Then("the available spots should be (.*)")]
    public void ThenTheAvailableSpotsShouldBe(int expected)
    {
        TestAssert.Equal(expected, tourContext.Tour.AvailableSpots);
    }

    [Then("the tour should not be at minimum capacity")]
    public void ThenTheTourShouldNotBeAtMinimumCapacity()
    {
        TestAssert.False(tourContext.Tour.IsAtMinimumCapacity);
    }

    [Then("the tour should not be fully booked")]
    public void ThenTheTourShouldNotBeFullyBooked()
    {
        TestAssert.False(tourContext.Tour.IsFullyBooked);
    }
}
