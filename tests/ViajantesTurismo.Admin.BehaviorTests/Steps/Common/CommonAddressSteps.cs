using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Admin.Domain.Customers;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps.Common;

[Binding]
public sealed class CommonAddressSteps(CustomerContext context)
{
    [When(@"I create address with street ""([^""]*)"", city ""([^""]*)"", state ""([^""]*)"", country ""([^""]*)"", postal code ""([^""]*)""")]
    public void WhenICreateAddressWithStreetCityStateCountryPostalCode(string street, string city, string state, string country, string postalCode)
    {
        context.AddressResult = Address.Create(street, null, "Downtown", postalCode, city, state, country);
    }

    [When(@"I create address with street ""([^""]*)"", city ""([^""]*)"", state ""([^""]*)"", country ""([^""]*)"", postal code ""([^""]*)"", neighborhood ""([^""]*)""")]
    public void WhenICreateAddressWithAllFields(string street, string city, string state, string country, string postalCode, string neighborhood)
    {
        context.AddressResult = Address.Create(street, null, neighborhood, postalCode, city, state, country);
    }
}
