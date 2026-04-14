using ViajantesTurismo.Admin.Domain.Shared;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps.Validation;

[Binding]
[Scope(Feature = "Physical Info Validation")]
public sealed class PhysicalInfoValidationSteps(CustomerContext context)
{
    [When(@"I create physical info with weight (\d+) kg, height (\d+) cm, and bike type ""([^""]*)""")]
    public void WhenICreatePhysicalInfoWithWeightHeightAndBikeType(decimal weight, int height, string bikeTypeStr)
    {
        var bikeType = Enum.Parse<BikeType>(bikeTypeStr);
        context.PhysicalInfoResult = PhysicalInfo.Create(weight, height, bikeType);
    }

    [When(@"I create physical info with weight (-?\d+\.?\d*) kg")]
    public void WhenICreatePhysicalInfoWithWeightDecimal(decimal weight)
    {
        context.PhysicalInfoResult = PhysicalInfo.Create(weight, 180, BikeType.Regular);
    }

    [When(@"I create physical info with height (-?\d+) cm")]
    public void WhenICreatePhysicalInfoWithHeight(int height)
    {
        context.PhysicalInfoResult = PhysicalInfo.Create(75m, height, BikeType.Regular);
    }

    [When(@"I create physical info with weight (-?\d+\.?\d*) kg and height (-?\d+) cm")]
    public void WhenICreatePhysicalInfoWithWeightAndHeight(decimal weight, int height)
    {
        context.PhysicalInfoResult = PhysicalInfo.Create(weight, height, BikeType.Regular);
    }

    [When(@"I create physical info with bike type ""([^""]*)""")]
    public void WhenICreatePhysicalInfoWithBikeType(string bikeTypeStr)
    {
        var bikeType = Enum.Parse<BikeType>(bikeTypeStr);
        context.PhysicalInfoResult = PhysicalInfo.Create(75m, 180, bikeType);
    }

    [Then("the physical info should be created successfully")]
    public void ThenThePhysicalInfoShouldBeCreatedSuccessfully()
    {
        Assert.NotNull(context.PhysicalInfoResult);
        Assert.True(context.PhysicalInfoResult.Value.IsSuccess);
    }

    [Then("the physical info creation should fail")]
    public void ThenThePhysicalInfoCreationShouldFail()
    {
        Assert.NotNull(context.PhysicalInfoResult);
        Assert.False(context.PhysicalInfoResult.Value.IsSuccess);
    }

    [Then(@"the error should be ""(.*)""")]
    public void ThenThePhysicalInfoErrorShouldBe(string expectedError)
    {
        Assert.NotNull(context.PhysicalInfoResult);
        Assert.True(context.PhysicalInfoResult.Value.IsFailure, "Expected failure but got success");
        var errors = context.PhysicalInfoResult.Value.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? new List<string>();
        Assert.Contains(expectedError, allErrors);
    }
}
