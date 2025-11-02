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
        context.Allergies = allergies;
        context.AdditionalInfo = additionalInfo;
        context.Result = MedicalInfo.Create(allergies, additionalInfo);
        if (context.Result.IsSuccess)
        {
            context.MedicalInfo = context.Result.Value;
        }
    }

    [When(@"I create medical info with allergies null and additional info ""([^""]*)""")]
    public void WhenICreateMedicalInfoWithAllergiesNullAndAdditionalInfo(string additionalInfo)
    {
        context.Allergies = null;
        context.AdditionalInfo = additionalInfo;
        context.Result = MedicalInfo.Create(null, additionalInfo);
        if (context.Result.IsSuccess)
        {
            context.MedicalInfo = context.Result.Value;
        }
    }

    [When(@"I create medical info with allergies ""([^""]*)"" and additional info null")]
    public void WhenICreateMedicalInfoWithAllergiesAndAdditionalInfoNull(string allergies)
    {
        context.Allergies = allergies;
        context.AdditionalInfo = null;
        context.Result = MedicalInfo.Create(allergies, null);
        if (context.Result.IsSuccess)
        {
            context.MedicalInfo = context.Result.Value;
        }
    }

    [When(@"I create medical info with allergies null and additional info null")]
    public void WhenICreateMedicalInfoWithAllergiesNullAndAdditionalInfoNull()
    {
        context.Allergies = null;
        context.AdditionalInfo = null;
        context.Result = MedicalInfo.Create(null, null);
        if (context.Result.IsSuccess)
        {
            context.MedicalInfo = context.Result.Value;
        }
    }

    [When(@"I create medical info with allergies ""([^""]*)""")]
    public void WhenICreateMedicalInfoWithAllergies(string allergies)
    {
        context.Allergies = allergies;
        context.Result = MedicalInfo.Create(allergies, null);
        if (context.Result.IsSuccess)
        {
            context.MedicalInfo = context.Result.Value;
        }
    }

    [When(@"I create medical info with additional info ""([^""]*)""")]
    public void WhenICreateMedicalInfoWithAdditionalInfo(string additionalInfo)
    {
        context.AdditionalInfo = additionalInfo;
        context.Result = MedicalInfo.Create(null, additionalInfo);
        if (context.Result.IsSuccess)
        {
            context.MedicalInfo = context.Result.Value;
        }
    }

    [When(@"I create medical info with allergies of (\d+) characters")]
    public void WhenICreateMedicalInfoWithAllergiesOfCharacters(int length)
    {
        var allergies = new string('A', length);
        context.Allergies = allergies;
        context.Result = MedicalInfo.Create(allergies, null);
        if (context.Result.IsSuccess)
        {
            context.MedicalInfo = context.Result.Value;
        }
    }

    [When(@"I create medical info with additional info of (\d+) characters")]
    public void WhenICreateMedicalInfoWithAdditionalInfoOfCharacters(int length)
    {
        var additionalInfo = new string('B', length);
        context.AdditionalInfo = additionalInfo;
        context.Result = MedicalInfo.Create(null, additionalInfo);
        if (context.Result.IsSuccess)
        {
            context.MedicalInfo = context.Result.Value;
        }
    }

    [When(@"I create medical info with allergies of (\d+) characters and additional info of (\d+) characters")]
    public void WhenICreateMedicalInfoWithAllergiesAndAdditionalInfoOfCharacters(int allergiesLength, int additionalInfoLength)
    {
        var allergies = new string('A', allergiesLength);
        var additionalInfo = new string('B', additionalInfoLength);
        context.Allergies = allergies;
        context.AdditionalInfo = additionalInfo;
        context.Result = MedicalInfo.Create(allergies, additionalInfo);
    }

    [Then(@"the medical info should be created successfully")]
    public void ThenTheMedicalInfoShouldBeCreatedSuccessfully()
    {
        Assert.True(context.Result.IsSuccess);
        Assert.NotNull(context.MedicalInfo);
    }

    [Then(@"the allergies should be null")]
    public void ThenTheAllergiesShouldBeNull()
    {
        Assert.Null(context.MedicalInfo.Allergies);
    }

    [Then(@"the allergies should be ""(.*)""")]
    public void ThenTheAllergiesShouldBe(string expectedAllergies)
    {
        Assert.Equal(expectedAllergies, context.MedicalInfo.Allergies);
    }

    [Then(@"the additional info should be null")]
    public void ThenTheAdditionalInfoShouldBeNull()
    {
        Assert.Null(context.MedicalInfo.AdditionalInfo);
    }

    [Then(@"the additional info should be ""(.*)""")]
    public void ThenTheAdditionalInfoShouldBe(string expectedAdditionalInfo)
    {
        Assert.Equal(expectedAdditionalInfo, context.MedicalInfo.AdditionalInfo);
    }

    [Then(@"the medical info creation should fail")]
    public void ThenTheMedicalInfoCreationShouldFail()
    {
        Assert.False(context.Result.IsSuccess);
    }

    [Then(@"the error should be ""(.*)""")]
    public void ThenTheMedicalInfoErrorShouldBe(string expectedError)
    {
        Assert.True(context.Result.IsFailure, "Expected failure but got success");
        var errors = context.Result.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? new List<string>();
        Assert.Contains(expectedError, allErrors);
    }
}