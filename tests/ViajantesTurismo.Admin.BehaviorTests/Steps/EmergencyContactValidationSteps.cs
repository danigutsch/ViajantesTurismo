using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Admin.Domain.Customers;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
[Scope(Feature = "Emergency Contact Validation")]
public sealed class EmergencyContactValidationSteps(EmergencyContactContext context)
{
    [When(@"I create emergency contact with name ""([^""]*)"" and mobile ""([^""]*)""")]
    public void WhenICreateEmergencyContactWithName(string name, string mobile)
    {
        context.Name = name;
        context.Mobile = mobile;
        context.Result = EmergencyContact.Create(name, mobile);
        if (context.Result.IsSuccess)
        {
            context.EmergencyContact = context.Result.Value;
        }
    }

    [When(@"I create emergency contact with name ""([^""]*)""")]
    public void WhenICreateEmergencyContactWithName(string name)
    {
        context.Name = name;
        context.Result = EmergencyContact.Create(name, "+1234567890");
        if (context.Result.IsSuccess)
        {
            context.EmergencyContact = context.Result.Value;
        }
    }

    [When(@"I create emergency contact with mobile ""([^""]*)""")]
    public void WhenICreateEmergencyContactWithMobile(string mobile)
    {
        context.Mobile = mobile;
        context.Result = EmergencyContact.Create("Jane Doe", mobile);
        if (context.Result.IsSuccess)
        {
            context.EmergencyContact = context.Result.Value;
        }
    }

    [When(@"I create emergency contact with null name and mobile ""([^""]*)""")]
    public void WhenICreateEmergencyContactWithNullNameAndMobile(string mobile)
    {
        context.Mobile = mobile;
        context.Result = EmergencyContact.Create(null, mobile);
    }

    [When(@"I create emergency contact with name ""([^""]*)"" and null mobile")]
    public void WhenICreateEmergencyContactWithNameAndNullMobile(string name)
    {
        context.Name = name;
        context.Result = EmergencyContact.Create(name, null);
    }

    [When(@"I create emergency contact with name of (\d+) characters")]
    public void WhenICreateEmergencyContactWithNameOfDCharacters(int length)
    {
        var name = new string('A', length);
        context.Name = name;
        context.Result = EmergencyContact.Create(name, "+1234567890");
        if (context.Result.IsSuccess)
        {
            context.EmergencyContact = context.Result.Value;
        }
    }

    [When(@"I create emergency contact with mobile of (\d+) characters")]
    public void WhenICreateEmergencyContactWithMobileOfDCharacters(int length)
    {
        var mobile = new string('1', length);
        context.Mobile = mobile;
        context.Result = EmergencyContact.Create("Jane Doe", mobile);
        if (context.Result.IsSuccess)
        {
            context.EmergencyContact = context.Result.Value;
        }
    }

    [Then(@"the emergency contact should be created successfully")]
    public void ThenTheEmergencyContactShouldBeCreatedSuccessfully()
    {
        Assert.True(context.Result.IsSuccess);
        Assert.NotNull(context.EmergencyContact);
    }

    [Then(@"the name should be ""(.*)""")]
    public void ThenTheNameShouldBe(string expectedName)
    {
        Assert.Equal(expectedName, context.EmergencyContact.Name);
    }

    [Then(@"the mobile should be ""(.*)""")]
    public void ThenTheMobileShouldBe(string expectedMobile)
    {
        Assert.Equal(expectedMobile, context.EmergencyContact.Mobile);
    }

    [Then(@"the emergency contact creation should fail")]
    public void ThenTheEmergencyContactCreationShouldFail()
    {
        Assert.False(context.Result.IsSuccess);
    }

    [Then(@"the error should be ""(.*)""")]
    public void ThenTheErrorShouldBe(string expectedError)
    {
        Assert.True(context.Result.IsFailure, "Expected failure but got success");
        var errors = context.Result.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? new List<string>();
        Assert.Contains(expectedError, allErrors);
    }
}
