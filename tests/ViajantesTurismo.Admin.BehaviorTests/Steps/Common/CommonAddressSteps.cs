using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Admin.Domain.Customers;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps.Common;

[Binding]
public sealed class CommonAddressSteps(AddressContext context)
{
    [When(@"I create address with street ""([^""]*)"", city ""([^""]*)"", state ""([^""]*)"", country ""([^""]*)"", postal code ""([^""]*)""")]
    public void WhenICreateAddressWithStreetStatePostalCode(string street, string city, string state, string country, string postalCode)
    {
        context.Street = street;
        context.City = city;
        context.State = state;
        context.Country = country;
        context.PostalCode = postalCode;
        context.AddressResult = Address.Create(street, null, "Downtown", postalCode, city, state, country);
    }
}
