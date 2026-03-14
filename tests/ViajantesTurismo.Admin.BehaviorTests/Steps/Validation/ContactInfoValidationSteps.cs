using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Admin.Domain.Customers;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps.Validation;

[Binding]
[Scope(Feature = "Contact Info Validation")]
public sealed class ContactInfoValidationSteps(CustomerContext context)
{
    [When(
        @"I create contact info with email ""([^""]*)"", mobile ""([^""]*)"", instagram ""([^""]*)"", facebook ""([^""]*)""")]
    public void WhenICreateContactInfoWithEmailInstagram(string email, string mobile, string instagram, string facebook)
    {
        context.ContactInfoResult = ContactInfo.Create(email, mobile, instagram, facebook);
    }

    [When(@"I create contact info with email ""([^""]*)""")]
    public void WhenICreateContactInfoWithEmail(string email)
    {
        context.ContactInfoResult = ContactInfo.Create(email, "+1234567890", null, null);
    }

    [When("I create contact info with null email")]
    public void WhenICreateContactInfoWithNullEmail()
    {
        context.ContactInfoResult = ContactInfo.Create(null!, "+1234567890", null, null);
    }

    [When(@"I create contact info with email of (\d+) characters")]
    public void WhenICreateContactInfoWithEmailOfDCharacters(int length)
    {
        var email = new string('a', length - 12) + "@example.com";
        context.ContactInfoResult = ContactInfo.Create(email, "+1234567890", null, null);
    }

    [When(@"I create contact info with mobile ""(.*)""")]
    public void WhenICreateContactInfoWithMobile(string mobile)
    {
        context.ContactInfoResult = ContactInfo.Create("test@example.com", mobile, null, null);
    }

    [When("I create contact info with null mobile")]
    public void WhenICreateContactInfoWithNullMobile()
    {
        context.ContactInfoResult = ContactInfo.Create("test@example.com", null!, null, null);
    }

    [When(@"I create contact info with mobile of (\d+) characters")]
    public void WhenICreateContactInfoWithMobileOfDCharacters(int length)
    {
        var mobile = new string('1', length);
        context.ContactInfoResult = ContactInfo.Create("test@example.com", mobile, null, null);
    }

    [When(@"I create contact info with instagram ""(.*)""")]
    public void WhenICreateContactInfoWithInstagram(string instagram)
    {
        context.ContactInfoResult = ContactInfo.Create("test@example.com", "+1234567890", instagram, null);
    }

    [When("I create contact info with instagram null")]
    public void WhenICreateContactInfoWithInstagramNull()
    {
        context.ContactInfoResult = ContactInfo.Create("test@example.com", "+1234567890", null, null);
    }

    [When(@"I create contact info with Instagram of (\d+) characters")]
    public void WhenICreateContactInfoWithInstagramOfDCharacters(int length)
    {
        var instagram = new string('a', length);
        context.ContactInfoResult = ContactInfo.Create("test@example.com", "+1234567890", instagram, null);
    }

    [When(@"I create contact info with facebook ""(.*)""")]
    public void WhenICreateContactInfoWithFacebook(string facebook)
    {
        context.ContactInfoResult = ContactInfo.Create("test@example.com", "+1234567890", null, facebook);
    }

    [When("I create contact info with facebook null")]
    public void WhenICreateContactInfoWithFacebookNull()
    {
        context.ContactInfoResult = ContactInfo.Create("test@example.com", "+1234567890", null, null);
    }

    [When(@"I create contact info with Facebook of (\d+) characters")]
    public void WhenICreateContactInfoWithFacebookOfDCharacters(int length)
    {
        var facebook = new string('a', length);
        context.ContactInfoResult = ContactInfo.Create("test@example.com", "+1234567890", null, facebook);
    }

    [When(@"I create contact info with email ""([^""]*)"" and mobile ""([^""]*)""")]
    public void WhenICreateContactInfoWithEmail(string email, string mobile)
    {
        context.ContactInfoResult = ContactInfo.Create(email, mobile, null, null);
    }

    [Then("the contact info should be created successfully")]
    public void ThenTheContactInfoShouldBeCreatedSuccessfully()
    {
        Assert.True(context.ContactInfoResult.IsSuccess, context.ContactInfoResult.ErrorDetails?.Detail ?? "Result failed");
        Assert.NotNull(context.ContactInfoResult.Value);
    }

    [Then("the contact info creation should fail")]
    public void ThenTheContactInfoCreationShouldFail()
    {
        Assert.True(context.ContactInfoResult.IsFailure, "Expected failure but got success");
    }

    [Then(@"the email should be ""(.*)""")]
    public void ThenTheEmailShouldBe(string expectedEmail)
    {
        Assert.Equal(expectedEmail, context.ContactInfoResult.Value.Email);
    }

    [Then(@"the mobile should be ""(.*)""")]
    public void ThenTheMobileShouldBe(string expectedMobile)
    {
        Assert.Equal(expectedMobile, context.ContactInfoResult.Value.Mobile);
    }

    [Then(@"the instagram should be ""(.*)""")]
    public void ThenTheInstagramShouldBe(string expectedInstagram)
    {
        Assert.Equal(expectedInstagram, context.ContactInfoResult.Value.Instagram);
    }

    [Then("the instagram should be null")]
    public void ThenTheInstagramShouldBeNull()
    {
        Assert.Null(context.ContactInfoResult.Value.Instagram);
    }

    [Then("the facebook should be null")]
    public void ThenTheFacebookShouldBeNull()
    {
        Assert.Null(context.ContactInfoResult.Value.Facebook);
    }

    [Then(@"the error should be ""(.*)""")]
    public void ThenTheErrorShouldBe(string expectedError)
    {
        Assert.True(context.ContactInfoResult.IsFailure, "Expected failure but got success");

        var errors = context.ContactInfoResult.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? new List<string>();

        Assert.Contains(expectedError, allErrors);
    }

    [When("I attempt to create contact info with null email")]
    public void WhenIAttemptToCreateContactInfoWithNullEmail()
    {
        WhenICreateContactInfoWithNullEmail();
    }

    [When("I attempt to create contact info with null mobile")]
    public void WhenIAttemptToCreateContactInfoWithNullMobile()
    {
        context.ContactInfoResult = ContactInfo.Create("test@example.com", null!, null, null);
    }

    [When(@"I attempt to create contact info with email ""([^""]*)""$")]
    public void WhenIAttemptToCreateContactInfoWithEmail(string email)
    {
        WhenICreateContactInfoWithEmail(email);
    }

    [When(@"I attempt to create contact info with mobile ""(.*)""")]
    public void WhenIAttemptToCreateContactInfoWithMobile(string mobile)
    {
        context.ContactInfoResult = ContactInfo.Create("test@example.com", mobile, null, null);
    }

    [When(@"I attempt to create contact info with email of (\d+) characters")]
    public void WhenIAttemptToCreateContactInfoWithEmailOfCharacters(int length)
    {
        WhenICreateContactInfoWithEmailOfDCharacters(length);
    }

    [When(@"I attempt to create contact info with mobile of (\d+) characters")]
    public void WhenIAttemptToCreateContactInfoWithMobileOfCharacters(int length)
    {
        var mobile = new string('1', length);
        context.ContactInfoResult = ContactInfo.Create("test@example.com", mobile, null, null);
    }

    [When(@"I attempt to create contact info with Instagram of (\d+) characters")]
    public void WhenIAttemptToCreateContactInfoWithInstagramOfCharacters(int length)
    {
        var instagram = new string('a', length);
        context.ContactInfoResult = ContactInfo.Create("test@example.com", "+1234567890", instagram, null);
    }

    [When(@"I attempt to create contact info with Facebook of (\d+) characters")]
    public void WhenIAttemptToCreateContactInfoWithFacebookOfCharacters(int length)
    {
        var facebook = new string('a', length);
        context.ContactInfoResult = ContactInfo.Create("test@example.com", "+1234567890", null, facebook);
    }

    [When(@"I attempt to create contact info with email ""(.*)"" and mobile ""(.*)""")]
    public void WhenIAttemptToCreateContactInfoWithEmailAndMobile(string email, string mobile)
    {
        context.ContactInfoResult = ContactInfo.Create(email, mobile, null, null);
    }

    [Then("I should not be able to create the contact info")]
    public void ThenIShouldNotBeAbleToCreateTheContactInfo()
    {
        Assert.True(context.ContactInfoResult.IsFailure, "Expected contact info creation to fail, but it succeeded.");
    }

    [Then("I should be informed that (.+) is required")]
    public void ThenIShouldBeInformedThatFieldIsRequired(string fieldName)
    {
        Assert.True(context.ContactInfoResult.IsFailure);
        var normalizedFieldName = fieldName.Replace(" ", "", StringComparison.Ordinal);
        Assert.True(context.ContactInfoResult.ErrorDetails?.ValidationErrors?.Any(kvp =>
                kvp.Key.Equals(normalizedFieldName, StringComparison.OrdinalIgnoreCase)) ?? false,
            $"Expected validation error for {normalizedFieldName}");
    }

    [Then(@"I should be informed that (.+) cannot exceed (\d+) characters")]
    public void ThenIShouldBeInformedThatFieldCannotExceedCharacters(string fieldName, int maxLength)
    {
        Assert.True(context.ContactInfoResult.IsFailure);
        var normalizedFieldName = fieldName.Replace(" ", "", StringComparison.Ordinal);
        Assert.True(context.ContactInfoResult.ErrorDetails?.ValidationErrors?.Any(kvp =>
                kvp.Key.Equals(normalizedFieldName, StringComparison.OrdinalIgnoreCase)) ?? false,
            $"Expected validation error for {normalizedFieldName}");
    }

    [Then("I should be informed that email must be in a valid format")]
    public void ThenIShouldBeInformedThatEmailMustBeInAValidFormat()
    {
        Assert.True(context.ContactInfoResult.IsFailure);
        Assert.True(context.ContactInfoResult.ErrorDetails?.ValidationErrors?.Any(kvp =>
                kvp.Key.Equals("Email", StringComparison.OrdinalIgnoreCase)) ?? false,
            "Expected validation error for Email");
    }
}
