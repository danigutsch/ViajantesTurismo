using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
public sealed class BookingCustomerEntitySteps(BookingCustomerContext context)
{
    [When(@"I create a booking customer with id (.*), bike type ""(.*)"", and bike price (.*)")]
    public void WhenICreateABookingCustomerWithIdBikeTypeAndBikePrice(int customerId, string bikeType, decimal bikePrice)
    {
        var type = Enum.Parse<BikeType>(bikeType);
        context.Result = BookingCustomer.Create(customerId, type, bikePrice);
    }

    [When(@"I try to create a booking customer with bike price (.*)")]
    public void WhenITryToCreateABookingCustomerWithBikePrice(decimal bikePrice)
    {
        context.Result = BookingCustomer.Create(1, BikeType.Regular, bikePrice);
    }

    [When(@"I try to create a booking customer with bike type ""(.*)""")]
    public void WhenITryToCreateABookingCustomerWithBikeType(string bikeType)
    {
        var type = Enum.Parse<BikeType>(bikeType);
        context.Result = BookingCustomer.Create(1, type, 0m);
    }

    [When(@"I try to create a booking customer with invalid bike type (.*)")]
    public void WhenITryToCreateABookingCustomerWithInvalidBikeType(int invalidBikeType)
    {
        context.Result = BookingCustomer.Create(1, (BikeType)invalidBikeType, 50m);
    }

    [Then(@"the booking customer should be created successfully")]
    public void ThenTheBookingCustomerShouldBeCreatedSuccessfully()
    {
        var result = (Result<BookingCustomer>)context.Result;
        Assert.True(result.IsSuccess);
    }

    [Then(@"the booking customer should have bike type ""(.*)""")]
    public void ThenTheBookingCustomerShouldHaveBikeType(string expectedBikeType)
    {
        var result = (Result<BookingCustomer>)context.Result;
        var type = Enum.Parse<BikeType>(expectedBikeType);
        Assert.Equal(type, result.Value.BikeType);
    }

    [Then(@"the booking customer should have bike price (.*)")]
    public void ThenTheBookingCustomerShouldHaveBikePrice(decimal expectedPrice)
    {
        var result = (Result<BookingCustomer>)context.Result;
        Assert.Equal(expectedPrice, result.Value.BikePrice);
    }

    [Then(@"the booking customer creation should fail")]
    public void ThenTheBookingCustomerCreationShouldFail()
    {
        var result = (Result<BookingCustomer>)context.Result;
        Assert.False(result.IsSuccess);
    }
}
