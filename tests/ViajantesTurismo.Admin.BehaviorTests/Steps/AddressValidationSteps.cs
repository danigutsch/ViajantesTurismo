using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Admin.Domain.Customers;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
public sealed class AddressValidationSteps(AddressContext context)
{
    [When(@"I create address with street ""([^""]*)"", city ""([^""]*)"", state ""([^""]*)"", country ""([^""]*)"", postal code ""([^""]*)""")]
    public void WhenICreateAddressWithStreetCityStateCountryPostalCodeCommas(string street, string city, string state, string country, string postalCode)
    {
        context.Street = street;
        context.City = city;
        context.State = state;
        context.Country = country;
        context.PostalCode = postalCode;
        context.Address = new Address(street, null, "Downtown", postalCode, city, state, country);
    }

    [When(@"I create address with street ""([^""]*)"" and city ""([^""]*)"" and state ""([^""]*)"" and country ""([^""]*)"" and postal code ""([^""]*)""")]
    public void WhenICreateAddressWithStreetCityStateCountryPostalCode(string street, string city, string state, string country, string postalCode)
    {
        context.Street = street;
        context.City = city;
        context.State = state;
        context.Country = country;
        context.PostalCode = postalCode;
        context.Address = new Address(street, null, "Downtown", postalCode, city, state, country);
    }

    [When(@"I create address with street ""([^""]*)""")]
    public void WhenICreateAddressWithStreet(string street)
    {
        context.Street = street;
        context.Address = new Address(street, null, "Downtown", "10001", "New York", "NY", "USA");
    }

    [When(@"I create address with city ""(.*)""")]
    public void WhenICreateAddressWithCity(string city)
    {
        context.City = city;
        context.Address = new Address("123 Main St", null, "Downtown", "10001", city, "NY", "USA");
    }

    [When(@"I create address with state ""(.*)""")]
    public void WhenICreateAddressWithState(string state)
    {
        context.State = state;
        context.Address = new Address("123 Main St", null, "Downtown", "10001", "New York", state, "USA");
    }

    [When(@"I create address with country ""(.*)""")]
    public void WhenICreateAddressWithCountry(string country)
    {
        context.Country = country;
        context.Address = new Address("123 Main St", null, "Downtown", "10001", "New York", "NY", country);
    }

    [When(@"I create address with postal code ""(.*)""")]
    public void WhenICreateAddressWithPostalCode(string postalCode)
    {
        context.PostalCode = postalCode;
        context.Address = new Address("123 Main St", null, "Downtown", postalCode, "New York", "NY", "USA");
    }

    [When(@"I create address with complement null")]
    public void WhenICreateAddressWithComplementNull()
    {
        context.Complement = null!;
        context.Address = new Address("123 Main St", null, "Downtown", "10001", "New York", "NY", "USA");
    }

    [When(@"I create address with complement ""(.*)""")]
    public void WhenICreateAddressWithComplement(string complement)
    {
        context.Complement = complement;
        context.Address = new Address("123 Main St", complement, "Downtown", "10001", "New York", "NY", "USA");
    }

    [When(@"I create address with neighborhood ""(.*)""")]
    public void WhenICreateAddressWithNeighborhood(string neighborhood)
    {
        context.Neighborhood = neighborhood;
        context.Address = new Address("123 Main St", null, neighborhood, "10001", "New York", "NY", "USA");
    }

    [When(@"I create address with all fields having extra whitespace")]
    public void WhenICreateAddressWithAllFieldsHavingExtraWhitespace()
    {
        context.Address = new Address(
            "  123    Main    St  ",
            "  Apt   5B  ",
            "  Downtown    Area  ",
            "  10001  ",
            "  New York  ",
            "  NY  ",
            "  USA  ");
    }

    [Then(@"the address should be created successfully")]
    public void ThenTheAddressShouldBeCreatedSuccessfully()
    {
        Assert.NotNull(context.Address);
    }

    [Then(@"the street should be ""(.*)""")]
    public void ThenTheStreetShouldBe(string expectedStreet)
    {
        Assert.Equal(expectedStreet, context.Address.Street);
    }

    [Then(@"the city should be ""(.*)""")]
    public void ThenTheCityShouldBe(string expectedCity)
    {
        Assert.Equal(expectedCity, context.Address.City);
    }

    [Then(@"the state should be ""(.*)""")]
    public void ThenTheStateShouldBe(string expectedState)
    {
        Assert.Equal(expectedState, context.Address.State);
    }

    [Then(@"the country should be ""(.*)""")]
    public void ThenTheCountryShouldBe(string expectedCountry)
    {
        Assert.Equal(expectedCountry, context.Address.Country);
    }

    [Then(@"the postal code should be ""(.*)""")]
    public void ThenThePostalCodeShouldBe(string expectedPostalCode)
    {
        Assert.Equal(expectedPostalCode, context.Address.PostalCode);
    }

    [Then(@"the complement should be null")]
    public void ThenTheComplementShouldBeNull()
    {
        Assert.Null(context.Address.Complement);
    }

    [Then(@"the complement should be ""(.*)""")]
    public void ThenTheComplementShouldBe(string expectedComplement)
    {
        Assert.Equal(expectedComplement, context.Address.Complement);
    }

    [Then(@"the neighborhood should be ""(.*)""")]
    public void ThenTheNeighborhoodShouldBe(string expectedNeighborhood)
    {
        Assert.Equal(expectedNeighborhood, context.Address.Neighborhood);
    }

    [Then(@"all address fields should be properly trimmed and normalized")]
    public void ThenAllAddressFieldsShouldBeProperlyTrimmedAndNormalized()
    {
        Assert.Equal("123 Main St", context.Address.Street);
        Assert.Equal("Apt 5B", context.Address.Complement);
        Assert.Equal("Downtown Area", context.Address.Neighborhood);
        Assert.Equal("10001", context.Address.PostalCode);
        Assert.Equal("New York", context.Address.City);
        Assert.Equal("NY", context.Address.State);
        Assert.Equal("USA", context.Address.Country);
    }
}
