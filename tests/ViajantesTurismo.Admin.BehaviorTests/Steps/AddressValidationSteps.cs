using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Admin.Domain.Customers;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
[Scope(Feature = "Address Validation")]
public sealed class AddressValidationSteps(CustomerContext context)
{
    [When("I attempt to create an address without a street")]
    public void WhenIAttemptToCreateAnAddressWithoutAStreet()
    {
        context.AddressResult = Address.Create("", null, "Downtown", "10001", "New York", "NY", "USA");
    }

    [When(@"I attempt to create an address with a street of (\d+) characters")]
    public void WhenIAttemptToCreateAnAddressWithAStreetOfCharacters(int length)
    {
        context.AddressResult =
            Address.Create(new string('A', length), null, "Downtown", "10001", "New York", "NY", "USA");
    }

    [When(@"I create an address with a street of (\d+) characters")]
    public void WhenICreateAnAddressWithAStreetOfCharacters(int length)
    {
        context.AddressResult =
            Address.Create(new string('A', length), null, "Downtown", "10001", "New York", "NY", "USA");
    }

    [When("I attempt to create an address without a neighborhood")]
    public void WhenIAttemptToCreateAnAddressWithoutANeighborhood()
    {
        context.AddressResult = Address.Create("123 Main St", null, "", "10001", "New York", "NY", "USA");
    }

    [When(@"I attempt to create an address with a neighborhood of (\d+) characters")]
    public void WhenIAttemptToCreateAnAddressWithANeighborhoodOfCharacters(int length)
    {
        context.AddressResult =
            Address.Create("123 Main St", null, new string('C', length), "10001", "New York", "NY", "USA");
    }

    [When(@"I create an address with a neighborhood of (\d+) characters")]
    public void WhenICreateAnAddressWithANeighborhoodOfCharacters(int length)
    {
        context.AddressResult =
            Address.Create("123 Main St", null, new string('C', length), "10001", "New York", "NY", "USA");
    }

    [When("I attempt to create an address without a postal code")]
    public void WhenIAttemptToCreateAnAddressWithoutAPostalCode()
    {
        context.AddressResult = Address.Create("123 Main St", null, "Downtown", "", "New York", "NY", "USA");
    }

    [When(@"I attempt to create an address with a postal code of (\d+) characters")]
    public void WhenIAttemptToCreateAnAddressWithAPostalCodeOfCharacters(int length)
    {
        context.AddressResult = Address.Create("123 Main St", null, "Downtown", new string('D', length), "New York",
            "NY", "USA");
    }

    [When(@"I create an address with a postal code of (\d+) characters")]
    public void WhenICreateAnAddressWithAPostalCodeOfCharacters(int length)
    {
        context.AddressResult = Address.Create("123 Main St", null, "Downtown", new string('D', length), "New York",
            "NY", "USA");
    }

    [When("I attempt to create an address without a city")]
    public void WhenIAttemptToCreateAnAddressWithoutACity()
    {
        context.AddressResult = Address.Create("123 Main St", null, "Downtown", "10001", "", "NY", "USA");
    }

    [When(@"I attempt to create an address with a city of (\d+) characters")]
    public void WhenIAttemptToCreateAnAddressWithACityOfCharacters(int length)
    {
        context.AddressResult =
            Address.Create("123 Main St", null, "Downtown", "10001", new string('E', length), "NY", "USA");
    }

    [When(@"I create an address with a city of (\d+) characters")]
    public void WhenICreateAnAddressWithACityOfCharacters(int length)
    {
        context.AddressResult =
            Address.Create("123 Main St", null, "Downtown", "10001", new string('E', length), "NY", "USA");
    }

    [When("I attempt to create an address without a state")]
    public void WhenIAttemptToCreateAnAddressWithoutAState()
    {
        context.AddressResult = Address.Create("123 Main St", null, "Downtown", "10001", "New York", "", "USA");
    }

    [When(@"I attempt to create an address with a state of (\d+) characters")]
    public void WhenIAttemptToCreateAnAddressWithAStateOfCharacters(int length)
    {
        context.AddressResult = Address.Create("123 Main St", null, "Downtown", "10001", "New York",
            new string('F', length), "USA");
    }

    [When(@"I create an address with a state of (\d+) characters")]
    public void WhenICreateAnAddressWithAStateOfCharacters(int length)
    {
        context.AddressResult = Address.Create("123 Main St", null, "Downtown", "10001", "New York",
            new string('F', length), "USA");
    }

    [When("I attempt to create an address without a country")]
    public void WhenIAttemptToCreateAnAddressWithoutACountry()
    {
        context.AddressResult = Address.Create("123 Main St", null, "Downtown", "10001", "New York", "NY", "");
    }

    [When(@"I attempt to create an address with a country of (\d+) characters")]
    public void WhenIAttemptToCreateAnAddressWithACountryOfCharacters(int length)
    {
        context.AddressResult = Address.Create("123 Main St", null, "Downtown", "10001", "New York", "NY",
            new string('G', length));
    }

    [When(@"I create an address with a country of (\d+) characters")]
    public void WhenICreateAnAddressWithACountryOfCharacters(int length)
    {
        context.AddressResult = Address.Create("123 Main St", null, "Downtown", "10001", "New York", "NY",
            new string('G', length));
    }

    [When(@"I create an address with complement ""(.*)""")]
    public void WhenICreateAnAddressWithComplement(string complement)
    {
        context.AddressResult = Address.Create("123 Main St", complement, "Downtown", "10001", "New York", "NY", "USA");
    }

    [When("I create an address without a complement")]
    public void WhenICreateAnAddressWithoutAComplement()
    {
        context.AddressResult = Address.Create("123 Main St", null, "Downtown", "10001", "New York", "NY", "USA");
    }

    [When(@"I attempt to create an address with a complement of (\d+) characters")]
    public void WhenIAttemptToCreateAnAddressWithAComplementOfCharacters(int length)
    {
        context.AddressResult = Address.Create("123 Main St", new string('B', length), "Downtown", "10001", "New York",
            "NY", "USA");
    }

    [When(@"I create an address with a complement of (\d+) characters")]
    public void WhenICreateAnAddressWithAComplementOfCharacters(int length)
    {
        context.AddressResult = Address.Create("123 Main St", new string('B', length), "Downtown", "10001", "New York",
            "NY", "USA");
    }

    [When(
        @"I create an address with street ""([^""]*)"", city ""([^""]*)"", state ""([^""]*)"", country ""([^""]*)"", postal code ""([^""]*)"", and neighborhood ""([^""]*)""")]
    public void WhenICreateAnAddressWithAllFields(string street, string city, string state, string country,
        string postalCode, string neighborhood)
    {
        context.AddressResult = Address.Create(street, null, neighborhood, postalCode, city, state, country);
    }

    [When("I create an address with fields containing extra whitespace")]
    public void WhenICreateAnAddressWithFieldsContainingExtraWhitespace()
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

    [Then("the address should be successfully created")]
    public void ThenTheAddressShouldBeSuccessfullyCreated()
    {
        Assert.True(context.AddressResult.IsSuccess,
            context.AddressResult.ErrorDetails?.Detail ?? "Address creation failed");
        Assert.NotNull(context.AddressResult.Value);
    }

    [Then("I should be informed that street is required")]
    public void ThenIShouldBeInformedThatStreetIsRequired()
    {
        Assert.True(context.AddressResult.IsFailure, "Expected failure but got success");
        var errors = context.AddressResult.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? new List<string>();
        Assert.Contains("Street is required.", allErrors);
    }

    [Then("I should be informed that street cannot exceed 128 characters")]
    public void ThenIShouldBeInformedThatStreetCannotExceed128Characters()
    {
        Assert.True(context.AddressResult.IsFailure, "Expected failure but got success");
        var errors = context.AddressResult.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? new List<string>();
        Assert.Contains("Street cannot exceed 128 characters.", allErrors);
    }

    [Then("I should be informed that neighborhood is required")]
    public void ThenIShouldBeInformedThatNeighborhoodIsRequired()
    {
        Assert.True(context.AddressResult.IsFailure, "Expected failure but got success");
        var errors = context.AddressResult.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? new List<string>();
        Assert.Contains("Neighborhood is required.", allErrors);
    }

    [Then("I should be informed that neighborhood cannot exceed 128 characters")]
    public void ThenIShouldBeInformedThatNeighborhoodCannotExceed128Characters()
    {
        Assert.True(context.AddressResult.IsFailure, "Expected failure but got success");
        var errors = context.AddressResult.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? new List<string>();
        Assert.Contains("Neighborhood cannot exceed 128 characters.", allErrors);
    }

    [Then("I should be informed that postal code is required")]
    public void ThenIShouldBeInformedThatPostalCodeIsRequired()
    {
        Assert.True(context.AddressResult.IsFailure, "Expected failure but got success");
        var errors = context.AddressResult.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? new List<string>();
        Assert.Contains("Postal code is required.", allErrors);
    }

    [Then("I should be informed that postal code cannot exceed 64 characters")]
    public void ThenIShouldBeInformedThatPostalCodeCannotExceed64Characters()
    {
        Assert.True(context.AddressResult.IsFailure, "Expected failure but got success");
        var errors = context.AddressResult.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? new List<string>();
        Assert.Contains("Postal code cannot exceed 64 characters.", allErrors);
    }

    [Then("I should be informed that city is required")]
    public void ThenIShouldBeInformedThatCityIsRequired()
    {
        Assert.True(context.AddressResult.IsFailure, "Expected failure but got success");
        var errors = context.AddressResult.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? new List<string>();
        Assert.Contains("City is required.", allErrors);
    }

    [Then("I should be informed that city cannot exceed 128 characters")]
    public void ThenIShouldBeInformedThatCityCannotExceed128Characters()
    {
        Assert.True(context.AddressResult.IsFailure, "Expected failure but got success");
        var errors = context.AddressResult.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? new List<string>();
        Assert.Contains("City cannot exceed 128 characters.", allErrors);
    }

    [Then("I should be informed that state is required")]
    public void ThenIShouldBeInformedThatStateIsRequired()
    {
        Assert.True(context.AddressResult.IsFailure, "Expected failure but got success");
        var errors = context.AddressResult.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? new List<string>();
        Assert.Contains("State is required.", allErrors);
    }

    [Then("I should be informed that state cannot exceed 128 characters")]
    public void ThenIShouldBeInformedThatStateCannotExceed128Characters()
    {
        Assert.True(context.AddressResult.IsFailure, "Expected failure but got success");
        var errors = context.AddressResult.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? new List<string>();
        Assert.Contains("State cannot exceed 128 characters.", allErrors);
    }

    [Then("I should be informed that country is required")]
    public void ThenIShouldBeInformedThatCountryIsRequired()
    {
        Assert.True(context.AddressResult.IsFailure, "Expected failure but got success");
        var errors = context.AddressResult.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? new List<string>();
        Assert.Contains("Country is required.", allErrors);
    }

    [Then("I should be informed that country cannot exceed 128 characters")]
    public void ThenIShouldBeInformedThatCountryCannotExceed128Characters()
    {
        Assert.True(context.AddressResult.IsFailure, "Expected failure but got success");
        var errors = context.AddressResult.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? new List<string>();
        Assert.Contains("Country cannot exceed 128 characters.", allErrors);
    }

    [Then("I should be informed that complement cannot exceed 128 characters")]
    public void ThenIShouldBeInformedThatComplementCannotExceed128Characters()
    {
        Assert.True(context.AddressResult.IsFailure, "Expected failure but got success");
        var errors = context.AddressResult.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? new List<string>();
        Assert.Contains("Complement cannot exceed 128 characters.", allErrors);
    }

    [Then(@"the complement should be ""(.*)""")]
    public void ThenTheComplementShouldBe(string expectedComplement)
    {
        Assert.Equal(expectedComplement, context.AddressResult.Value.Complement);
    }

    [Then("the complement should be empty")]
    public void ThenTheComplementShouldBeEmpty()
    {
        Assert.Null(context.AddressResult.Value.Complement);
    }

    [Then("all address fields should have normalized whitespace")]
    public void ThenAllAddressFieldsShouldHaveNormalizedWhitespace()
    {
        Assert.Equal("123 Main St", context.AddressResult.Value.Street);
        Assert.Equal("Apt 5B", context.AddressResult.Value.Complement);
        Assert.Equal("Downtown Area", context.AddressResult.Value.Neighborhood);
        Assert.Equal("10001", context.AddressResult.Value.PostalCode);
        Assert.Equal("New York", context.AddressResult.Value.City);
        Assert.Equal("NY", context.AddressResult.Value.State);
        Assert.Equal("USA", context.AddressResult.Value.Country);
    }
}
