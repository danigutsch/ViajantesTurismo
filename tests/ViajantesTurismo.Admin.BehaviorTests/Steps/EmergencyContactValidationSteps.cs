using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Admin.Domain.Customers;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
[Scope(Feature = "Emergency Contact Validation")]
public sealed class EmergencyContactValidationSteps(CustomerContext context)
{
    [When("I attempt to create an emergency contact without a name")]
    public void WhenIAttemptToCreateAnEmergencyContactWithoutAName()
    {
        context.EmergencyContactResult = EmergencyContact.Create("", "+1234567890");
    }

    [When(@"I attempt to create an emergency contact with a name of (\d+) characters")]
    public void WhenIAttemptToCreateAnEmergencyContactWithANameOfCharacters(int length)
    {
        context.EmergencyContactResult = EmergencyContact.Create(new string('A', length), "+1234567890");
    }

    [When(@"I create an emergency contact with a name of (\d+) characters")]
    public void WhenICreateAnEmergencyContactWithANameOfCharacters(int length)
    {
        context.EmergencyContactResult = EmergencyContact.Create(new string('A', length), "+1234567890");
    }

    [When("I attempt to create an emergency contact without a mobile")]
    public void WhenIAttemptToCreateAnEmergencyContactWithoutAMobile()
    {
        context.EmergencyContactResult = EmergencyContact.Create("Jane Doe", "");
    }

    [When(@"I attempt to create an emergency contact with a mobile of (\d+) characters")]
    public void WhenIAttemptToCreateAnEmergencyContactWithAMobileOfCharacters(int length)
    {
        context.EmergencyContactResult = EmergencyContact.Create("Jane Doe", new string('1', length));
    }

    [When(@"I create an emergency contact with a mobile of (\d+) characters")]
    public void WhenICreateAnEmergencyContactWithAMobileOfCharacters(int length)
    {
        context.EmergencyContactResult = EmergencyContact.Create("Jane Doe", new string('1', length));
    }

    [When(@"I create an emergency contact with name ""([^""]*)"" and mobile ""([^""]*)""")]
    public void WhenICreateAnEmergencyContactWithNameAndMobile(string name, string mobile)
    {
        context.EmergencyContactResult = EmergencyContact.Create(name, mobile);
    }

    [When("I create an emergency contact with fields containing extra whitespace")]
    public void WhenICreateAnEmergencyContactWithFieldsContainingExtraWhitespace()
    {
        context.EmergencyContactResult = EmergencyContact.Create("  Jane    Doe  ", "  +1234    567890  ");
    }

    [When("I attempt to create an emergency contact without any fields")]
    public void WhenIAttemptToCreateAnEmergencyContactWithoutAnyFields()
    {
        context.EmergencyContactResult = EmergencyContact.Create("", "");
    }

    [Then("the emergency contact should be successfully created")]
    public void ThenTheEmergencyContactShouldBeSuccessfullyCreated()
    {
        Assert.True(context.EmergencyContactResult.IsSuccess, context.EmergencyContactResult.ErrorDetails?.Detail ?? "Creation failed");
        Assert.NotNull(context.EmergencyContactResult.Value);
    }

    [Then("I should be informed that emergency contact name is required")]
    public void ThenIShouldBeInformedThatEmergencyContactNameIsRequired()
    {
        Assert.True(context.EmergencyContactResult.IsFailure, "Expected failure but got success");
        var errors = context.EmergencyContactResult.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? new List<string>();
        Assert.Contains("Emergency contact name is required.", allErrors);
    }

    [Then("I should be informed that emergency contact name cannot exceed 128 characters")]
    public void ThenIShouldBeInformedThatEmergencyContactNameCannotExceed128Characters()
    {
        Assert.True(context.EmergencyContactResult.IsFailure, "Expected failure but got success");
        var errors = context.EmergencyContactResult.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? new List<string>();
        Assert.Contains("Emergency contact name cannot exceed 128 characters.", allErrors);
    }

    [Then("I should be informed that emergency contact mobile is required")]
    public void ThenIShouldBeInformedThatEmergencyContactMobileIsRequired()
    {
        Assert.True(context.EmergencyContactResult.IsFailure, "Expected failure but got success");
        var errors = context.EmergencyContactResult.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? new List<string>();
        Assert.Contains("Emergency contact mobile is required.", allErrors);
    }

    [Then("I should be informed that emergency contact mobile cannot exceed 64 characters")]
    public void ThenIShouldBeInformedThatEmergencyContactMobileCannotExceed64Characters()
    {
        Assert.True(context.EmergencyContactResult.IsFailure, "Expected failure but got success");
        var errors = context.EmergencyContactResult.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? new List<string>();
        Assert.Contains("Emergency contact mobile cannot exceed 64 characters.", allErrors);
    }

    [Then("all emergency contact fields should have normalized whitespace")]
    public void ThenAllEmergencyContactFieldsShouldHaveNormalizedWhitespace()
    {
        Assert.Equal("Jane Doe", context.EmergencyContactResult.Value.Name);
        Assert.Equal("+1234 567890", context.EmergencyContactResult.Value.Mobile);
    }
}
