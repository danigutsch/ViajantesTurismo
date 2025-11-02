using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Admin.Domain.Customers;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
[Scope(Feature = "Identification Info Validation")]
public sealed class IdentificationInfoValidationSteps(IdentificationInfoContext context)
{
    [When(@"I create identification info with national ID ""([^""]*)"" and ID nationality ""([^""]*)""")]
    public void WhenICreateIdentificationInfoWithNationalIdAndIdNationality(string nationalId, string idNationality)
    {
        context.NationalId = nationalId;
        context.IdNationality = idNationality;
        context.Result = IdentificationInfo.Create(nationalId, idNationality);
    }

    [When(@"I create identification info with null national ID and ID nationality ""([^""]*)""")]
    public void WhenICreateIdentificationInfoWithNullNationalIdAndIdNationality(string idNationality)
    {
        context.NationalId = null;
        context.IdNationality = idNationality;
        context.Result = IdentificationInfo.Create(null, idNationality);
    }

    [When(@"I create identification info with national ID ""([^""]*)"" and null ID nationality")]
    public void WhenICreateIdentificationInfoWithNationalIdAndNullIdNationality(string nationalId)
    {
        context.NationalId = nationalId;
        context.IdNationality = null;
        context.Result = IdentificationInfo.Create(nationalId, null);
    }

    [When(@"I create identification info with national ID of (\d+) characters")]
    public void WhenICreateIdentificationInfoWithNationalIdOfCharacters(int length)
    {
        var nationalId = new string('A', length);
        context.NationalId = nationalId;
        context.Result = IdentificationInfo.Create(nationalId, "Brazilian");
    }

    [When(@"I create identification info with ID nationality of (\d+) characters")]
    public void WhenICreateIdentificationInfoWithIdNationalityOfCharacters(int length)
    {
        var idNationality = new string('B', length);
        context.IdNationality = idNationality;
        context.Result = IdentificationInfo.Create("12345678", idNationality);
    }

    [When(@"I create identification info with national ID of (\d+) characters and ID nationality of (\d+) characters")]
    public void WhenICreateIdentificationInfoWithNationalIdAndIdNationalityOfCharacters(int nationalIdLength, int idNationalityLength)
    {
        var nationalId = new string('A', nationalIdLength);
        var idNationality = new string('B', idNationalityLength);
        context.NationalId = nationalId;
        context.IdNationality = idNationality;
        context.Result = IdentificationInfo.Create(nationalId, idNationality);
    }

    [Then(@"the identification info should be created successfully")]
    public void ThenTheIdentificationInfoShouldBeCreatedSuccessfully()
    {
        Assert.True(context.Result.IsSuccess);
        Assert.NotNull(context.IdentificationInfo);
    }

    [Then(@"the national ID should be ""(.*)""")]
    public void ThenTheNationalIdShouldBe(string expectedNationalId)
    {
        Assert.Equal(expectedNationalId, context.IdentificationInfo.NationalId, StringComparer.Ordinal);
    }

    [Then(@"the ID nationality should be ""(.*)""")]
    public void ThenTheIdNationalityShouldBe(string expectedIdNationality)
    {
        Assert.Equal(expectedIdNationality, context.IdentificationInfo.IdNationality, StringComparer.Ordinal);
    }

    [Then(@"the identification info creation should fail")]
    public void ThenTheIdentificationInfoCreationShouldFail()
    {
        Assert.False(context.Result.IsSuccess);
    }

    [Then(@"the error should be ""(.*)""")]
    public void ThenTheIdentificationInfoErrorShouldBe(string expectedError)
    {
        Assert.True(context.Result.IsFailure, "Expected failure but got success");
        var errors = context.Result.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? new List<string>();
        Assert.Contains(expectedError, allErrors, StringComparer.Ordinal);
    }
}