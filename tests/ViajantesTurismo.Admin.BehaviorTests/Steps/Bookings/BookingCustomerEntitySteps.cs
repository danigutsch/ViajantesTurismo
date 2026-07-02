using ViajantesTurismo.Admin.Domain.Shared;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps.Bookings;

[Binding]
public sealed class BookingCustomerEntitySteps(BookingContext bookingContext)
{
    [When(@"I create a booking customer with id (.*), bike type ""(.*)"", and bike price (.*)")]
    public void WhenICreateABookingCustomerWithIdBikeTypeAndBikePrice(int customerId, string bikeType,
        decimal bikePrice)
    {
        var type = Enum.Parse<BikeType>(bikeType);
        bookingContext.BookingCustomerResult = BookingCustomer.Create(Guid.CreateVersion7(), type, bikePrice);
    }

    [When("I try to create a booking customer with bike price (.*)")]
    public void WhenITryToCreateABookingCustomerWithBikePrice(decimal bikePrice)
    {
        bookingContext.BookingCustomerResult = BookingCustomer.Create(Guid.CreateVersion7(), BikeType.Regular, bikePrice);
    }

    [When(@"I try to create a booking customer with bike type ""(.*)""")]
    public void WhenITryToCreateABookingCustomerWithBikeType(string bikeType)
    {
        var type = Enum.Parse<BikeType>(bikeType);
        bookingContext.BookingCustomerResult = BookingCustomer.Create(Guid.CreateVersion7(), type, 0m);
    }

    [When("I try to create a booking customer with invalid bike type (.*)")]
    public void WhenITryToCreateABookingCustomerWithInvalidBikeType(int invalidBikeType)
    {
        bookingContext.BookingCustomerResult = BookingCustomer.Create(Guid.CreateVersion7(), (BikeType)invalidBikeType, 50m);
    }

    [Then("the booking customer should be created successfully")]
    public void ThenTheBookingCustomerShouldBeCreatedSuccessfully()
    {
        global::SharedKernel.Testing.Assertions.TestAssert.NotNull(bookingContext.BookingCustomerResult);
        global::SharedKernel.Testing.Assertions.TestAssert.True(bookingContext.BookingCustomerResult.Value.IsSuccess);
    }

    [Then(@"the booking customer should have bike type ""(.*)""")]
    public void ThenTheBookingCustomerShouldHaveBikeType(string expectedBikeType)
    {
        global::SharedKernel.Testing.Assertions.TestAssert.NotNull(bookingContext.BookingCustomerResult);
        var type = Enum.Parse<BikeType>(expectedBikeType);
        global::SharedKernel.Testing.Assertions.TestAssert.Equal(type, bookingContext.BookingCustomerResult.Value.Value.BikeType);
    }

    [Then("the booking customer should have bike price (.*)")]
    public void ThenTheBookingCustomerShouldHaveBikePrice(decimal expectedPrice)
    {
        global::SharedKernel.Testing.Assertions.TestAssert.NotNull(bookingContext.BookingCustomerResult);
        global::SharedKernel.Testing.Assertions.TestAssert.Equal(expectedPrice, bookingContext.BookingCustomerResult.Value.Value.BikePrice);
    }

    [Then("the booking customer creation should fail")]
    public void ThenTheBookingCustomerCreationShouldFail()
    {
        global::SharedKernel.Testing.Assertions.TestAssert.NotNull(bookingContext.BookingCustomerResult);
        global::SharedKernel.Testing.Assertions.TestAssert.False(bookingContext.BookingCustomerResult.Value.IsSuccess);
    }
}
