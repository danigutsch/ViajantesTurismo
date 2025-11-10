using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Admin.Domain.Customers;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
[Scope(Feature = "Contact Info Validation")]
public sealed class ContactInfoValidationSteps(ContactInfoContext context)
{
    [When(
        @"I create contact info with email ""([^""]*)"", mobile ""([^""]*)"", instagram ""([^""]*)"", facebook ""([^""]*)""")]
    public void WhenICreateContactInfoWithEmailInstagram(string email, string mobile, string instagram, string facebook)
    {
        context.Email = email;
        context.Mobile = mobile;
        context.Instagram = instagram;
        context.Facebook = facebook;
        context.Result = ContactInfo.Create(email, mobile, instagram, facebook);
        if (context.Result.IsSuccess)
        {
            context.ContactInfo = context.Result.Value;
        }
    }

    [When(@"I create contact info with email ""([^""]*)""")]
    public void WhenICreateContactInfoWithEmail(string email)
    {
        context.Email = email;
        context.Result = ContactInfo.Create(email, "+1234567890", null, null);
        if (context.Result.IsSuccess)
        {
            context.ContactInfo = context.Result.Value;
        }
    }

    [When("I create contact info with null email")]
    public void WhenICreateContactInfoWithNullEmail()
    {
        context.Email = null!;
        context.Result = ContactInfo.Create(null!, "+1234567890", null, null);
    }

    [When(@"I create contact info with email of (\d+) characters")]
    public void WhenICreateContactInfoWithEmailOfDCharacters(int length)
    {
        var email = new string('a', length - 12) + "@example.com";
        context.Email = email;
        context.Result = ContactInfo.Create(email, "+1234567890", null, null);
        if (context.Result.IsSuccess)
        {
            context.ContactInfo = context.Result.Value;
        }
    }

    [When(@"I create contact info with mobile ""(.*)""")]
    public void WhenICreateContactInfoWithMobile(string mobile)
    {
        context.Mobile = mobile;
        context.Result = ContactInfo.Create("test@example.com", mobile, null, null);
        if (context.Result.IsSuccess)
        {
            context.ContactInfo = context.Result.Value;
        }
    }

    [When("I create contact info with null mobile")]
    public void WhenICreateContactInfoWithNullMobile()
    {
        context.Mobile = null!;
        context.Result = ContactInfo.Create("test@example.com", null!, null, null);
    }

    [When(@"I create contact info with mobile of (\d+) characters")]
    public void WhenICreateContactInfoWithMobileOfDCharacters(int length)
    {
        var mobile = new string('1', length);
        context.Mobile = mobile;
        context.Result = ContactInfo.Create("test@example.com", mobile, null, null);
        if (context.Result.IsSuccess)
        {
            context.ContactInfo = context.Result.Value;
        }
    }

    [When(@"I create contact info with instagram ""(.*)""")]
    public void WhenICreateContactInfoWithInstagram(string instagram)
    {
        context.Instagram = instagram;
        context.Result = ContactInfo.Create("test@example.com", "+1234567890", instagram, null);
        if (context.Result.IsSuccess)
        {
            context.ContactInfo = context.Result.Value;
        }
    }

    [When("I create contact info with instagram null")]
    public void WhenICreateContactInfoWithInstagramNull()
    {
        context.Instagram = null!;
        context.Result = ContactInfo.Create("test@example.com", "+1234567890", null, null);
        if (context.Result.IsSuccess)
        {
            context.ContactInfo = context.Result.Value;
        }
    }

    [When(@"I create contact info with Instagram of (\d+) characters")]
    public void WhenICreateContactInfoWithInstagramOfDCharacters(int length)
    {
        var instagram = new string('a', length);
        context.Instagram = instagram;
        context.Result = ContactInfo.Create("test@example.com", "+1234567890", instagram, null);
        if (context.Result.IsSuccess)
        {
            context.ContactInfo = context.Result.Value;
        }
    }

    [When(@"I create contact info with facebook ""(.*)""")]
    public void WhenICreateContactInfoWithFacebook(string facebook)
    {
        context.Facebook = facebook;
        context.Result = ContactInfo.Create("test@example.com", "+1234567890", null, facebook);
        if (context.Result.IsSuccess)
        {
            context.ContactInfo = context.Result.Value;
        }
    }

    [When("I create contact info with facebook null")]
    public void WhenICreateContactInfoWithFacebookNull()
    {
        context.Facebook = null!;
        context.Result = ContactInfo.Create("test@example.com", "+1234567890", null, null);
        if (context.Result.IsSuccess)
        {
            context.ContactInfo = context.Result.Value;
        }
    }

    [When(@"I create contact info with Facebook of (\d+) characters")]
    public void WhenICreateContactInfoWithFacebookOfDCharacters(int length)
    {
        var facebook = new string('a', length);
        context.Facebook = facebook;
        context.Result = ContactInfo.Create("test@example.com", "+1234567890", null, facebook);
        if (context.Result.IsSuccess)
        {
            context.ContactInfo = context.Result.Value;
        }
    }

    [When(@"I create contact info with email ""([^""]*)"" and mobile ""([^""]*)""")]
    public void WhenICreateContactInfoWithEmail(string email, string mobile)
    {
        context.Email = email;
        context.Mobile = mobile;
        context.Result = ContactInfo.Create(email, mobile, null, null);
    }

    [Then("the contact info should be created successfully")]
    public void ThenTheContactInfoShouldBeCreatedSuccessfully()
    {
        Assert.True(context.Result.IsSuccess, context.Result.ErrorDetails?.Detail ?? "Result failed");
        Assert.NotNull(context.ContactInfo);
    }

    [Then("the contact info creation should fail")]
    public void ThenTheContactInfoCreationShouldFail()
    {
        Assert.True(context.Result.IsFailure, "Expected failure but got success");
    }

    [Then(@"the email should be ""(.*)""")]
    public void ThenTheEmailShouldBe(string expectedEmail)
    {
        Assert.Equal(expectedEmail, context.ContactInfo.Email);
    }

    [Then(@"the mobile should be ""(.*)""")]
    public void ThenTheMobileShouldBe(string expectedMobile)
    {
        Assert.Equal(expectedMobile, context.ContactInfo.Mobile);
    }

    [Then(@"the instagram should be ""(.*)""")]
    public void ThenTheInstagramShouldBe(string expectedInstagram)
    {
        Assert.Equal(expectedInstagram, context.ContactInfo.Instagram);
    }

    [Then("the instagram should be null")]
    public void ThenTheInstagramShouldBeNull()
    {
        Assert.Null(context.ContactInfo.Instagram);
    }

    [Then("the facebook should be null")]
    public void ThenTheFacebookShouldBeNull()
    {
        Assert.Null(context.ContactInfo.Facebook);
    }

    [Then(@"the error should be ""(.*)""")]
    public void ThenTheErrorShouldBe(string expectedError)
    {
        Assert.True(context.Result.IsFailure, "Expected failure but got success");

        var errors = context.Result.ErrorDetails?.ValidationErrors;
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
        context.Email = "test@example.com";
        context.Mobile = null!;
        context.Result = ContactInfo.Create("test@example.com", null!, null, null);
    }

    [When(@"I attempt to create contact info with email ""([^""]*)""$")]
    public void WhenIAttemptToCreateContactInfoWithEmail(string email)
    {
        WhenICreateContactInfoWithEmail(email);
    }

    [When(@"I attempt to create contact info with mobile ""(.*)""")]
    public void WhenIAttemptToCreateContactInfoWithMobile(string mobile)
    {
        context.Email = "test@example.com";
        context.Mobile = mobile;
        context.Result = ContactInfo.Create("test@example.com", mobile, null, null);
    }

    [When(@"I attempt to create contact info with email of (\d+) characters")]
    public void WhenIAttemptToCreateContactInfoWithEmailOfCharacters(int length)
    {
        WhenICreateContactInfoWithEmailOfDCharacters(length);
    }

    [When(@"I attempt to create contact info with mobile of (\d+) characters")]
    public void WhenIAttemptToCreateContactInfoWithMobileOfCharacters(int length)
    {
        context.Email = "test@example.com";
        var mobile = new string('1', length);
        context.Mobile = mobile;
        context.Result = ContactInfo.Create("test@example.com", mobile, null, null);
    }

    [When(@"I attempt to create contact info with Instagram of (\d+) characters")]
    public void WhenIAttemptToCreateContactInfoWithInstagramOfCharacters(int length)
    {
        context.Email = "test@example.com";
        context.Mobile = "+1234567890";
        var instagram = new string('a', length);
        context.Instagram = instagram;
        context.Result = ContactInfo.Create("test@example.com", "+1234567890", instagram, null);
    }

    [When(@"I attempt to create contact info with Facebook of (\d+) characters")]
    public void WhenIAttemptToCreateContactInfoWithFacebookOfCharacters(int length)
    {
        context.Email = "test@example.com";
        context.Mobile = "+1234567890";
        var facebook = new string('a', length);
        context.Facebook = facebook;
        context.Result = ContactInfo.Create("test@example.com", "+1234567890", null, facebook);
    }

    [When(@"I attempt to create contact info with email ""(.*)"" and mobile ""(.*)""")]
    public void WhenIAttemptToCreateContactInfoWithEmailAndMobile(string email, string mobile)
    {
        context.Email = email;
        context.Mobile = mobile;
        context.Result = ContactInfo.Create(email, mobile, null, null);
    }

    [Then("I should not be able to create the contact info")]
    public void ThenIShouldNotBeAbleToCreateTheContactInfo()
    {
        Assert.True(context.Result.IsFailure, "Expected contact info creation to fail, but it succeeded.");
    }

    [Then("I should be informed that (.+) is required")]
    public void ThenIShouldBeInformedThatFieldIsRequired(string fieldName)
    {
        Assert.True(context.Result.IsFailure);
        var normalizedFieldName = fieldName.Replace(" ", "", StringComparison.Ordinal);
        Assert.True(context.Result.ErrorDetails?.ValidationErrors?.Any(kvp =>
                kvp.Key.Equals(normalizedFieldName, StringComparison.OrdinalIgnoreCase)) ?? false,
            $"Expected validation error for {normalizedFieldName}");
    }

    [Then(@"I should be informed that (.+) cannot exceed (\d+) characters")]
    public void ThenIShouldBeInformedThatFieldCannotExceedCharacters(string fieldName, int maxLength)
    {
        Assert.True(context.Result.IsFailure);
        var normalizedFieldName = fieldName.Replace(" ", "", StringComparison.Ordinal);
        Assert.True(context.Result.ErrorDetails?.ValidationErrors?.Any(kvp =>
                kvp.Key.Equals(normalizedFieldName, StringComparison.OrdinalIgnoreCase)) ?? false,
            $"Expected validation error for {normalizedFieldName}");
    }
}
