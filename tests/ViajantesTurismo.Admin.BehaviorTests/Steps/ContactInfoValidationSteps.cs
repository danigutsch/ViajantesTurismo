using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Admin.Domain.Customers;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
[Scope(Feature = "Contact Info Validation")]
public sealed class ContactInfoValidationSteps(ContactInfoContext context)
{
    [When(@"I create contact info with email ""([^""]*)"", mobile ""([^""]*)"", instagram ""([^""]*)"", facebook ""([^""]*)""")]
    public void WhenICreateContactInfoWithEmailMobileInstagramFacebookCommas(string email, string mobile, string instagram, string facebook)
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

    [When(@"I create contact info with email ""([^""]*)"" and mobile ""([^""]*)"" and instagram ""([^""]*)"" and facebook ""([^""]*)""")]
    public void WhenICreateContactInfoWithEmailMobileInstagramFacebook(string email, string mobile, string instagram, string facebook)
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

    [When(@"I create contact info with null email")]
    public void WhenICreateContactInfoWithNullEmail()
    {
        context.Email = null!;
        context.Result = ContactInfo.Create(null!, "+1234567890", null, null);
    }

    [When(@"I create contact info with email of (\d+) characters")]
    public void WhenICreateContactInfoWithEmailOfLength(int length)
    {
        var email = new string('a', length - 12) + "@example.com"; // Account for @example.com
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

    [When(@"I create contact info with null mobile")]
    public void WhenICreateContactInfoWithNullMobile()
    {
        context.Mobile = null!;
        context.Result = ContactInfo.Create("test@example.com", null!, null, null);
    }

    [When(@"I create contact info with mobile of (\d+) characters")]
    public void WhenICreateContactInfoWithMobileOfLength(int length)
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

    [When(@"I create contact info with instagram null")]
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
    public void WhenICreateContactInfoWithInstagramOfLength(int length)
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

    [When(@"I create contact info with facebook null")]
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
    public void WhenICreateContactInfoWithFacebookOfLength(int length)
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
    public void WhenICreateContactInfoWithEmailAndMobile(string email, string mobile)
    {
        context.Email = email;
        context.Mobile = mobile;
        context.Result = ContactInfo.Create(email, mobile, null, null);
    }

    [Then(@"the contact info should be created successfully")]
    public void ThenTheContactInfoShouldBeCreatedSuccessfully()
    {
        Assert.True(context.Result.IsSuccess, context.Result.ErrorDetails?.Detail ?? "Result failed");
        Assert.NotNull(context.ContactInfo);
    }

    [Then(@"the contact info creation should fail")]
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

    [Then(@"the instagram should be null")]
    public void ThenTheInstagramShouldBeNull()
    {
        Assert.Null(context.ContactInfo.Instagram);
    }

    [Then(@"the facebook should be ""(.*)""")]
    public void ThenTheFacebookShouldBe(string expectedFacebook)
    {
        Assert.Equal(expectedFacebook, context.ContactInfo.Facebook);
    }

    [Then(@"the facebook should be null")]
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
}

