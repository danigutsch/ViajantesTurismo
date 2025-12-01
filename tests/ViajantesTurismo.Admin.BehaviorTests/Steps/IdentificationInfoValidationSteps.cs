using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Admin.Domain.Customers;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
[Scope(Feature = "Identification Info Validation")]
public sealed class IdentificationInfoValidationSteps(CustomerContext context)
{
    [When("I attempt to create identification info without a national ID")]
    public void WhenIAttemptToCreateIdentificationInfoWithoutANationalId()
    {
        context.IdentificationInfoResult = IdentificationInfo.Create("", "Brazilian");
    }

    [When(@"I attempt to create identification info with a national ID of (\d+) characters")]
    public void WhenIAttemptToCreateIdentificationInfoWithANationalIdOfCharacters(int length)
    {
        context.IdentificationInfoResult = IdentificationInfo.Create(new string('A', length), "Brazilian");
    }

    [When(@"I create identification info with a national ID of (\d+) characters")]
    public void WhenICreateIdentificationInfoWithANationalIdOfCharacters(int length)
    {
        context.IdentificationInfoResult = IdentificationInfo.Create(new string('A', length), "Brazilian");
    }

    [When("I attempt to create identification info without an ID nationality")]
    public void WhenIAttemptToCreateIdentificationInfoWithoutAnIdNationality()
    {
        context.IdentificationInfoResult = IdentificationInfo.Create("12345678", "");
    }

    [When(@"I attempt to create identification info with an ID nationality of (\d+) characters")]
    public void WhenIAttemptToCreateIdentificationInfoWithAnIdNationalityOfCharacters(int length)
    {
        context.IdentificationInfoResult = IdentificationInfo.Create("12345678", new string('B', length));
    }

    [When(@"I create identification info with an ID nationality of (\d+) characters")]
    public void WhenICreateIdentificationInfoWithAnIdNationalityOfCharacters(int length)
    {
        context.IdentificationInfoResult = IdentificationInfo.Create("12345678", new string('B', length));
    }

    [When(@"I create identification info with national ID ""([^""]*)"" and ID nationality ""([^""]*)""")]
    public void WhenICreateIdentificationInfoWithNationalIdAndIdNationality(string nationalId, string idNationality)
    {
        context.IdentificationInfoResult = IdentificationInfo.Create(nationalId, idNationality);
    }

    [When("I create identification info with fields containing extra whitespace")]
    public void WhenICreateIdentificationInfoWithFieldsContainingExtraWhitespace()
    {
        context.IdentificationInfoResult = IdentificationInfo.Create("  12345678  ", "  Brazilian  ");
    }

    [When("I attempt to create identification info without any fields")]
    public void WhenIAttemptToCreateIdentificationInfoWithoutAnyFields()
    {
        context.IdentificationInfoResult = IdentificationInfo.Create("", "");
    }

    [When("I attempt to create identification info with both fields exceeding maximum length")]
    public void WhenIAttemptToCreateIdentificationInfoWithBothFieldsExceedingMaximumLength()
    {
        context.IdentificationInfoResult = IdentificationInfo.Create(new string('A', 65), new string('B', 65));
    }

    [Then("the identification info should be successfully created")]
    public void ThenTheIdentificationInfoShouldBeSuccessfullyCreated()
    {
        Assert.True(context.IdentificationInfoResult.IsSuccess, context.IdentificationInfoResult.ErrorDetails?.Detail ?? "Creation failed");
        Assert.NotNull(context.IdentificationInfoResult.Value);
    }

    [Then("I should be informed that national ID is required")]
    public void ThenIShouldBeInformedThatNationalIdIsRequired()
    {
        Assert.True(context.IdentificationInfoResult.IsFailure, "Expected failure but got success");
        var errors = context.IdentificationInfoResult.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? new List<string>();
        Assert.Contains("National ID is required.", allErrors, StringComparer.Ordinal);
    }

    [Then("I should be informed that national ID cannot exceed 64 characters")]
    public void ThenIShouldBeInformedThatNationalIdCannotExceed64Characters()
    {
        Assert.True(context.IdentificationInfoResult.IsFailure, "Expected failure but got success");
        var errors = context.IdentificationInfoResult.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? new List<string>();
        Assert.Contains("National ID cannot exceed 64 characters.", allErrors, StringComparer.Ordinal);
    }

    [Then("I should be informed that ID nationality is required")]
    public void ThenIShouldBeInformedThatIdNationalityIsRequired()
    {
        Assert.True(context.IdentificationInfoResult.IsFailure, "Expected failure but got success");
        var errors = context.IdentificationInfoResult.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? new List<string>();
        Assert.Contains("ID nationality is required.", allErrors, StringComparer.Ordinal);
    }

    [Then("I should be informed that ID nationality cannot exceed 64 characters")]
    public void ThenIShouldBeInformedThatIdNationalityCannotExceed64Characters()
    {
        Assert.True(context.IdentificationInfoResult.IsFailure, "Expected failure but got success");
        var errors = context.IdentificationInfoResult.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? new List<string>();
        Assert.Contains("ID nationality cannot exceed 64 characters.", allErrors, StringComparer.Ordinal);
    }

    [Then("all identification fields should have normalized whitespace")]
    public void ThenAllIdentificationFieldsShouldHaveNormalizedWhitespace()
    {
        Assert.Equal("12345678", context.IdentificationInfoResult.Value.NationalId, StringComparer.Ordinal);
        Assert.Equal("Brazilian", context.IdentificationInfoResult.Value.IdNationality, StringComparer.Ordinal);
    }
}
