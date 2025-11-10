using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Admin.Domain.Customers;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
[Scope(Feature = "Medical Info Validation")]
public sealed class MedicalInfoValidationSteps(MedicalInfoContext context)
{
    [When(@"I create medical info with allergies ""([^""]*)"" and additional info ""([^""]*)""")]
    public void WhenICreateMedicalInfoWithAllergiesAndAdditionalInfo(string allergies, string additionalInfo)
    {
        context.Result = MedicalInfo.Create(allergies, additionalInfo);
        if (context.Result.IsSuccess)
        {
            context.MedicalInfo = context.Result.Value;
        }
    }

    [When(@"I create medical info with only allergies ""([^""]*)""")]
    public void WhenICreateMedicalInfoWithOnlyAllergies(string allergies)
    {
        context.Result = MedicalInfo.Create(allergies, null);
        if (context.Result.IsSuccess)
        {
            context.MedicalInfo = context.Result.Value;
        }
    }

    [When(@"I create medical info with only additional info ""([^""]*)""")]
    public void WhenICreateMedicalInfoWithOnlyAdditionalInfo(string additionalInfo)
    {
        context.Result = MedicalInfo.Create(null, additionalInfo);
        if (context.Result.IsSuccess)
        {
            context.MedicalInfo = context.Result.Value;
        }
    }

    [When(@"I create medical info without any information")]
    public void WhenICreateMedicalInfoWithoutAnyInformation()
    {
        context.Result = MedicalInfo.Create(null, null);
        if (context.Result.IsSuccess)
        {
            context.MedicalInfo = context.Result.Value;
        }
    }

    [When(@"I attempt to create medical info with allergies of (\d+) characters")]
    public void WhenIAttemptToCreateMedicalInfoWithAllergiesOfCharacters(int length)
    {
        context.Result = MedicalInfo.Create(new string('A', length), null);
    }

    [When(@"I create medical info with allergies of (\d+) characters")]
    public void WhenICreateMedicalInfoWithAllergiesOfCharacters(int length)
    {
        context.Result = MedicalInfo.Create(new string('A', length), null);
        if (context.Result.IsSuccess)
        {
            context.MedicalInfo = context.Result.Value;
        }
    }

    [When(@"I attempt to create medical info with additional info of (\d+) characters")]
    public void WhenIAttemptToCreateMedicalInfoWithAdditionalInfoOfCharacters(int length)
    {
        context.Result = MedicalInfo.Create(null, new string('B', length));
    }

    [When(@"I create medical info with additional info of (\d+) characters")]
    public void WhenICreateMedicalInfoWithAdditionalInfoOfCharacters(int length)
    {
        context.Result = MedicalInfo.Create(null, new string('B', length));
        if (context.Result.IsSuccess)
        {
            context.MedicalInfo = context.Result.Value;
        }
    }

    [When(@"I create medical info with fields containing extra whitespace")]
    public void WhenICreateMedicalInfoWithFieldsContainingExtraWhitespace()
    {
        context.Result = MedicalInfo.Create("Peanuts,    Shellfish,    Dairy", "Requires    medication    daily");
        if (context.Result.IsSuccess)
        {
            context.MedicalInfo = context.Result.Value;
        }
    }

    [When(@"I attempt to create medical info with both fields exceeding maximum length")]
    public void WhenIAttemptToCreateMedicalInfoWithBothFieldsExceedingMaximumLength()
    {
        context.Result = MedicalInfo.Create(new string('A', 501), new string('B', 501));
    }

    [Then(@"the medical info should be successfully created")]
    public void ThenTheMedicalInfoShouldBeSuccessfullyCreated()
    {
        Assert.True(context.Result.IsSuccess, context.Result.ErrorDetails?.Detail ?? "Creation failed");
        Assert.NotNull(context.MedicalInfo);
    }

    [Then(@"the allergies should be empty")]
    public void ThenTheAllergiesShouldBeEmpty()
    {
        Assert.Null(context.MedicalInfo.Allergies);
    }

    [Then(@"the additional info should be empty")]
    public void ThenTheAdditionalInfoShouldBeEmpty()
    {
        Assert.Null(context.MedicalInfo.AdditionalInfo);
    }

    [Then(@"I should be informed that allergies cannot exceed 500 characters")]
    public void ThenIShouldBeInformedThatAllergiesCannotExceed500Characters()
    {
        Assert.True(context.Result.IsFailure, "Expected failure but got success");
        var errors = context.Result.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? new List<string>();
        Assert.Contains("Allergies cannot exceed 500 characters.", allErrors);
    }

    [Then(@"I should be informed that additional information cannot exceed 500 characters")]
    public void ThenIShouldBeInformedThatAdditionalInformationCannotExceed500Characters()
    {
        Assert.True(context.Result.IsFailure, "Expected failure but got success");
        var errors = context.Result.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? new List<string>();
        Assert.Contains("Additional information cannot exceed 500 characters.", allErrors);
    }

    [Then(@"all medical info fields should have normalized whitespace")]
    public void ThenAllMedicalInfoFieldsShouldHaveNormalizedWhitespace()
    {
        Assert.Equal("Peanuts, Shellfish, Dairy", context.MedicalInfo.Allergies);
        Assert.Equal("Requires medication daily", context.MedicalInfo.AdditionalInfo);
    }
}
