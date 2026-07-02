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

        (result.IsSuccess).ShouldBeTrue();
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
        (tourContext.Tour.Capacity.MinCustomers).ShouldBe(expected);
    }

    [Then("the maximum capacity should be (.*)")]
    public void ThenTheMaximumCapacityShouldBe(int expected)
    {
        (tourContext.Tour.Capacity.MaxCustomers).ShouldBe(expected);
    }

    [Then("the capacity update should succeed")]
    public void ThenTheCapacityUpdateShouldSucceed()
    {
        (tourContext.CapacityUpdateResult).ShouldNotBeNull();
        (tourContext.CapacityUpdateResult.Value.IsSuccess).ShouldBeTrue();
    }

    [Then("the capacity update should fail")]
    public void ThenTheCapacityUpdateShouldFail()
    {
        (tourContext.CapacityUpdateResult).ShouldNotBeNull();
        (tourContext.CapacityUpdateResult.Value.IsFailure).ShouldBeTrue();
    }

    [Then("the error should indicate cannot reduce capacity below current bookings")]
    public void ThenTheErrorShouldIndicateCannotReduceCapacityBelowCurrentBookings()
    {
        (tourContext.CapacityUpdateResult).ShouldNotBeNull();
        (tourContext.CapacityUpdateResult.Value.IsFailure).ShouldBeTrue();

        var error = tourContext.CapacityUpdateResult.Value.ErrorDetails;
        (error).ShouldNotBeNull();
        (error.Detail).ShouldContain("capacity", StringComparison.OrdinalIgnoreCase);
        (error.Detail).ShouldContain("current", StringComparison.OrdinalIgnoreCase);
        (error.Detail).ShouldContain("booking", StringComparison.OrdinalIgnoreCase);
    }

    [Then("the tour creation should fail")]
    public void ThenTheTourCreationShouldFail()
    {
        (tourContext.CreationResult).ShouldNotBeNull();
        (tourContext.CreationResult.Value.IsFailure).ShouldBeTrue();
    }

    [Then("the error should indicate max must be at least min")]
    public void ThenTheErrorShouldIndicateMaxMustBeAtLeastMin()
    {
        (tourContext.CreationResult).ShouldNotBeNull();
        (tourContext.CreationResult.Value.IsFailure).ShouldBeTrue();
        var errors = tourContext.CreationResult.Value.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? [];
        var combinedErrors = string.Join(" ", allErrors);
        (combinedErrors).ShouldContain("maximum", StringComparison.OrdinalIgnoreCase);
        (combinedErrors).ShouldContain("minimum", StringComparison.OrdinalIgnoreCase);
    }

    [Then("the error should indicate minimum must be at least 1")]
    public void ThenTheErrorShouldIndicateMinimumMustBeAtLeast()
    {
        (tourContext.CreationResult).ShouldNotBeNull();
        (tourContext.CreationResult.Value.IsFailure).ShouldBeTrue();
        var errors = tourContext.CreationResult.Value.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? [];
        var combinedErrors = string.Join(" ", allErrors);
        (combinedErrors).ShouldContain("Minimum", StringComparison.OrdinalIgnoreCase);
        (combinedErrors).ShouldContain("1", StringComparison.Ordinal);
    }

    [Then("the error should indicate maximum cannot exceed 20")]
    public void ThenTheErrorShouldIndicateMaximumCannotExceed()
    {
        (tourContext.CreationResult).ShouldNotBeNull();
        (tourContext.CreationResult.Value.IsFailure).ShouldBeTrue();
        var errors = tourContext.CreationResult.Value.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? [];
        var combinedErrors = string.Join(" ", allErrors);
        (combinedErrors).ShouldContain("Maximum", StringComparison.OrdinalIgnoreCase);
        (combinedErrors).ShouldContain("20", StringComparison.Ordinal);
    }

    [Then("the error should indicate the tour is fully booked")]
    public void ThenTheErrorShouldIndicateTheTourIsFullyBooked()
    {
        (bookingContext.BookingCreationResult).ShouldNotBeNull();
        (bookingContext.BookingCreationResult.Value.IsFailure).ShouldBeTrue();
        var error = bookingContext.BookingCreationResult.Value.ErrorDetails;
        (error).ShouldNotBeNull();
        (error.Detail).ShouldContain("fully booked", StringComparison.OrdinalIgnoreCase);
    }

    [Then("the current customer count should be (.*)")]
    public void ThenTheCurrentCustomerCountShouldBe(int expected)
    {
        (tourContext.Tour.CurrentCustomerCount).ShouldBe(expected);
    }

    [Then("the available spots should be (.*)")]
    public void ThenTheAvailableSpotsShouldBe(int expected)
    {
        (tourContext.Tour.AvailableSpots).ShouldBe(expected);
    }

    [Then("the tour should not be at minimum capacity")]
    public void ThenTheTourShouldNotBeAtMinimumCapacity()
    {
        (tourContext.Tour.IsAtMinimumCapacity).ShouldBeFalse();
    }

    [Then("the tour should not be fully booked")]
    public void ThenTheTourShouldNotBeFullyBooked()
    {
        (tourContext.Tour.IsFullyBooked).ShouldBeFalse();
    }
}
