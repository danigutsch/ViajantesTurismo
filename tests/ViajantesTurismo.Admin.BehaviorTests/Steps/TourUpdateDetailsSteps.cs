using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
public sealed class TourUpdateDetailsSteps(TourContext tourContext)
{
    [Given(@"a tour exists with identifier ""(.*)"" and name ""(.*)""")]
    public void GivenATourExistsWithIdentifierAndName(string identifier, string name)
    {
        tourContext.Tour = TestHelpers.CreateTestTourWithIdentifierAndName(identifier, name);
    }

    [When(@"I update the tour details to identifier ""(.*)"" and name ""(.*)""")]
    public void WhenIUpdateTheTourDetailsToIdentifierAndName(string identifier, string name)
    {
        tourContext.Result = tourContext.Tour!.UpdateDetails(identifier, name);
    }

    [When(@"I update the tour details to identifier with (.*) characters and name ""(.*)""")]
    public void WhenIUpdateTheTourDetailsToIdentifierWithCharactersAndName(int length, string name)
    {
        var longIdentifier = new string('X', length);
        tourContext.Result = tourContext.Tour!.UpdateDetails(longIdentifier, name);
    }

    [When(@"I update the tour details to identifier ""(.*)"" and name with (.*) characters")]
    public void WhenIUpdateTheTourDetailsToIdentifierAndNameWithCharacters(string identifier, int length)
    {
        var longName = new string('X', length);
        tourContext.Result = tourContext.Tour!.UpdateDetails(identifier, longName);
    }

    [Then(@"the tour details update should succeed")]
    public void ThenTheTourDetailsUpdateShouldSucceed()
    {
        var result = (Result)tourContext.Result;
        Assert.True(result.IsSuccess, $"Expected success but got error: {result.ErrorDetails}");
    }

    [Then(@"the tour details update should fail")]
    public void ThenTheTourDetailsUpdateShouldFail()
    {
        var result = (Result)tourContext.Result;
        Assert.False(result.IsSuccess);
    }

    [Then(@"the tour should have identifier ""(.*)""")]
    public void ThenTheTourShouldHaveIdentifier(string expectedIdentifier)
    {
        Assert.Equal(expectedIdentifier, tourContext.Tour!.Identifier);
    }

    [Then(@"the tour should have name ""(.*)""")]
    public void ThenTheTourShouldHaveName(string expectedName)
    {
        Assert.Equal(expectedName, tourContext.Tour!.Name);
    }

    [Then(@"the error should contain ""(.*)""")]
    public void ThenTheErrorShouldContain(string expectedText)
    {
        var result = (Result)tourContext.Result;
        var errorMessage = result.ErrorDetails?.Detail ?? string.Empty;
        Assert.Contains(expectedText, errorMessage, StringComparison.OrdinalIgnoreCase);
    }
}