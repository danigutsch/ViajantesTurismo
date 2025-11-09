using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Admin.Domain.Customers;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
[Scope(Feature = "Address Validation")]
public sealed class AddressValidationSteps(AddressContext context)
{
    [When(@"I create address with street ""([^""]*)""")]
    public void WhenICreateAddressWithStreet(string street)
    {
        context.Street = street;
        context.AddressResult = Address.Create(street, null, "Downtown", "10001", "New York", "NY", "USA");
    }

    [When(@"I create address with city ""(.*)""")]
    public void WhenICreateAddressWithCity(string city)
    {
        context.City = city;
        context.AddressResult = Address.Create("123 Main St", null, "Downtown", "10001", city, "NY", "USA");
    }

    [When(@"I create address with state ""(.*)""")]
    public void WhenICreateAddressWithState(string state)
    {
        context.State = state;
        context.AddressResult = Address.Create("123 Main St", null, "Downtown", "10001", "New York", state, "USA");
    }

    [When(@"I create address with country ""(.*)""")]
    public void WhenICreateAddressWithCountry(string country)
    {
        context.Country = country;
        context.AddressResult = Address.Create("123 Main St", null, "Downtown", "10001", "New York", "NY", country);
    }

    [When(@"I create address with postal code ""(.*)""")]
    public void WhenICreateAddressWithPostalCode(string postalCode)
    {
        context.PostalCode = postalCode;
        context.AddressResult = Address.Create("123 Main St", null, "Downtown", postalCode, "New York", "NY", "USA");
    }

    [When(@"I create address with complement null")]
    public void WhenICreateAddressWithComplementNull()
    {
        context.Complement = null!;
        context.AddressResult = Address.Create("123 Main St", null, "Downtown", "10001", "New York", "NY", "USA");
    }

    [When(@"I create address with complement ""(.*)""")]
    public void WhenICreateAddressWithComplement(string complement)
    {
        context.Complement = complement;
        context.AddressResult = Address.Create("123 Main St", complement, "Downtown", "10001", "New York", "NY", "USA");
    }

    [When(@"I create address with neighborhood ""(.*)""")]
    public void WhenICreateAddressWithNeighborhood(string neighborhood)
    {
        context.Neighborhood = neighborhood;
        context.AddressResult = Address.Create("123 Main St", null, neighborhood, "10001", "New York", "NY", "USA");
    }

    [When(@"I create address with all fields having extra whitespace")]
    public void WhenICreateAddressWithAllFieldsHavingExtraWhitespace()
    {
        context.AddressResult = Address.Create(
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

    [When(@"I create address with null street and city ""([^""]*)"" and state ""([^""]*)"" and country ""([^""]*)"" and postal code ""([^""]*)""")]
    public void WhenICreateAddressWithNullStreetAndCityStateCountryPostalCode(string city, string state, string country, string postalCode)
    {
        context.AddressResult = Address.Create(null, null, "Downtown", postalCode, city, state, country);
    }

    [When(@"I create address with street ""([^""]*)"" and neighborhood ""([^""]*)"" and postal code ""([^""]*)"" and city ""([^""]*)"" and state ""([^""]*)"" and country ""([^""]*)""")]
    public void WhenICreateAddressWithStreetNeighborhoodPostalCodeCityStateCountry(string street, string neighborhood, string postalCode, string city, string state, string country)
    {
        context.AddressResult = Address.Create(street, null, neighborhood, postalCode, city, state, country);
    }

    [When(@"I create address with street of (\d+) characters")]
    public void WhenICreateAddressWithStreetOfCharacters(int length)
    {
        var street = new string('A', length);
        context.AddressResult = Address.Create(street, null, "Downtown", "10001", "New York", "NY", "USA");
    }

    [When(@"I create address with complement of (\d+) characters")]
    public void WhenICreateAddressWithComplementOfCharacters(int length)
    {
        var complement = new string('B', length);
        context.AddressResult = Address.Create("123 Main St", complement, "Downtown", "10001", "New York", "NY", "USA");
    }

    [When(@"I create address with neighborhood of (\d+) characters")]
    public void WhenICreateAddressWithNeighborhoodOfCharacters(int length)
    {
        var neighborhood = new string('C', length);
        context.AddressResult = Address.Create("123 Main St", null, neighborhood, "10001", "New York", "NY", "USA");
    }

    [When(@"I create address with postal code of (\d+) characters")]
    public void WhenICreateAddressWithPostalCodeOfCharacters(int length)
    {
        var postalCode = new string('D', length);
        context.AddressResult = Address.Create("123 Main St", null, "Downtown", postalCode, "New York", "NY", "USA");
    }

    [When(@"I create address with city of (\d+) characters")]
    public void WhenICreateAddressWithCityOfCharacters(int length)
    {
        var city = new string('E', length);
        context.AddressResult = Address.Create("123 Main St", null, "Downtown", "10001", city, "NY", "USA");
    }

    [When(@"I create address with state of (\d+) characters")]
    public void WhenICreateAddressWithStateOfCharacters(int length)
    {
        var state = new string('F', length);
        context.AddressResult = Address.Create("123 Main St", null, "Downtown", "10001", "New York", state, "USA");
    }

    [When(@"I create address with country of (\d+) characters")]
    public void WhenICreateAddressWithCountryOfCharacters(int length)
    {
        var country = new string('G', length);
        context.AddressResult = Address.Create("123 Main St", null, "Downtown", "10001", "New York", "NY", country);
    }

    [Then(@"the address creation should fail")]
    public void ThenTheAddressCreationShouldFail()
    {
        Assert.False(context.AddressResult.IsSuccess);
    }

    [Then(@"the error should be ""(.*)""")]
    public void ThenTheAddressErrorShouldBe(string expectedError)
    {
        Assert.True(context.AddressResult.IsFailure, "Expected failure but got success");
        var errors = context.AddressResult.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? new List<string>();
        Assert.Contains(expectedError, allErrors);
    }
}
