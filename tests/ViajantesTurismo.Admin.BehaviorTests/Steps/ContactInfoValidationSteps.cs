using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Admin.Domain.Customers;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
public sealed class ContactInfoValidationSteps(ContactInfoContext context)
{
    [When(@"I create contact info with email ""([^""]*)"", mobile ""([^""]*)"", instagram ""([^""]*)"", facebook ""([^""]*)""")]
    public void WhenICreateContactInfoWithEmailMobileInstagramFacebookCommas(string email, string mobile, string instagram, string facebook)
    {
        context.Email = email;
        context.Mobile = mobile;
        context.Instagram = instagram;
        context.Facebook = facebook;
        context.ContactInfo = new ContactInfo(email, mobile, instagram, facebook);
    }

    [When(@"I create contact info with email ""([^""]*)"" and mobile ""([^""]*)"" and instagram ""([^""]*)"" and facebook ""([^""]*)""")]
    public void WhenICreateContactInfoWithEmailMobileInstagramFacebook(string email, string mobile, string instagram, string facebook)
    {
        context.Email = email;
        context.Mobile = mobile;
        context.Instagram = instagram;
        context.Facebook = facebook;
        context.ContactInfo = new ContactInfo(email, mobile, instagram, facebook);
    }

    [When(@"I create contact info with email ""([^""]*)""")]
    public void WhenICreateContactInfoWithEmail(string email)
    {
        context.Email = email;
        context.ContactInfo = new ContactInfo(email, "+1234567890", null, null);
    }

    [When(@"I create contact info with mobile ""(.*)""")]
    public void WhenICreateContactInfoWithMobile(string mobile)
    {
        context.Mobile = mobile;
        context.ContactInfo = new ContactInfo("test@example.com", mobile, null, null);
    }

    [When(@"I create contact info with instagram ""(.*)""")]
    public void WhenICreateContactInfoWithInstagram(string instagram)
    {
        context.Instagram = instagram;
        context.ContactInfo = new ContactInfo("test@example.com", "+1234567890", instagram, null);
    }

    [When(@"I create contact info with instagram null")]
    public void WhenICreateContactInfoWithInstagramNull()
    {
        context.Instagram = null!;
        context.ContactInfo = new ContactInfo("test@example.com", "+1234567890", null, null);
    }

    [When(@"I create contact info with facebook ""(.*)""")]
    public void WhenICreateContactInfoWithFacebook(string facebook)
    {
        context.Facebook = facebook;
        context.ContactInfo = new ContactInfo("test@example.com", "+1234567890", null, facebook);
    }

    [When(@"I create contact info with facebook null")]
    public void WhenICreateContactInfoWithFacebookNull()
    {
        context.Facebook = null!;
        context.ContactInfo = new ContactInfo("test@example.com", "+1234567890", null, null);
    }

    [Then(@"the contact info should be created successfully")]
    public void ThenTheContactInfoShouldBeCreatedSuccessfully()
    {
        Assert.NotNull(context.ContactInfo);
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
}
