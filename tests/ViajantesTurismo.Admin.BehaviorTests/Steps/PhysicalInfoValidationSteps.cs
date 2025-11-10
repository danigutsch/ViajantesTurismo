using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Admin.Domain.Customers;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
[Scope(Feature = "Physical Info Validation")]
public sealed class PhysicalInfoValidationSteps(PhysicalInfoContext context)
{
    [When(@"I create physical info with weight (\d+) kg, height (\d+) cm, and bike type ""([^""]*)""")]
    public void WhenICreatePhysicalInfoWithWeightHeightAndBikeType(decimal weight, int height, string bikeTypeStr)
    {
        var bikeType = Enum.Parse<BikeType>(bikeTypeStr);
        context.WeightKg = weight;
        context.HeightCentimeters = height;
        context.BikeType = bikeType;
        context.Result = PhysicalInfo.Create(weight, height, bikeType);
        if (context.Result.IsSuccess)
        {
            context.PhysicalInfo = context.Result.Value;
        }
    }

    [When(@"I create physical info with weight (-?\d+\.?\d*) kg")]
    public void WhenICreatePhysicalInfoWithWeightDecimal(decimal weight)
    {
        context.WeightKg = weight;
        context.Result = PhysicalInfo.Create(weight, 180, BikeType.Regular);
        if (context.Result.IsSuccess)
        {
            context.PhysicalInfo = context.Result.Value;
        }
    }

    [When(@"I create physical info with height (-?\d+) cm")]
    public void WhenICreatePhysicalInfoWithHeight(int height)
    {
        context.HeightCentimeters = height;
        context.Result = PhysicalInfo.Create(75m, height, BikeType.Regular);
        if (context.Result.IsSuccess)
        {
            context.PhysicalInfo = context.Result.Value;
        }
    }

    [When(@"I create physical info with weight (-?\d+\.?\d*) kg and height (-?\d+) cm")]
    public void WhenICreatePhysicalInfoWithWeightAndHeight(decimal weight, int height)
    {
        context.WeightKg = weight;
        context.HeightCentimeters = height;
        context.Result = PhysicalInfo.Create(weight, height, BikeType.Regular);
    }

    [When(@"I create physical info with bike type ""([^""]*)""")]
    public void WhenICreatePhysicalInfoWithBikeType(string bikeTypeStr)
    {
        var bikeType = Enum.Parse<BikeType>(bikeTypeStr);
        context.BikeType = bikeType;
        context.Result = PhysicalInfo.Create(75m, 180, bikeType);
        if (context.Result.IsSuccess)
        {
            context.PhysicalInfo = context.Result.Value;
        }
    }

    [Then("the physical info should be created successfully")]
    public void ThenThePhysicalInfoShouldBeCreatedSuccessfully()
    {
        Assert.True(context.Result.IsSuccess);
        Assert.NotNull(context.PhysicalInfo);
    }

    [Then("the physical info creation should fail")]
    public void ThenThePhysicalInfoCreationShouldFail()
    {
        Assert.False(context.Result.IsSuccess);
    }

    [Then(@"the error should be ""(.*)""")]
    public void ThenThePhysicalInfoErrorShouldBe(string expectedError)
    {
        Assert.True(context.Result.IsFailure, "Expected failure but got success");
        var errors = context.Result.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? new List<string>();
        Assert.Contains(expectedError, allErrors);
    }
}
