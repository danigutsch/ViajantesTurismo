using Reqnroll;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Common.Results;
using Xunit;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
public sealed class CommonPersonalInfoSteps
{
    private DateTime _birthDate;
    private string _firstName = null!;
    private string _gender = null!;
    private string _lastName = null!;
    private string _nationality = null!;
    private string _profession = null!;
    private Result<PersonalInfo>? _result;

    [Given(@"I have valid personal information")]
    public void GivenIHaveValidPersonalInformation()
    {
        _firstName = "John";
        _lastName = "Smith";
        _gender = "Male";
        _birthDate = new DateTime(1990, 5, 15);
        _nationality = "American";
        _profession = "Software Engineer";
    }

    [When(@"I create the personal info")]
    public void WhenICreateThePersonalInfo()
    {
        _result = PersonalInfo.Create(_firstName, _lastName, _gender, _birthDate, _nationality, _profession);
    }

    [Then(@"the creation should succeed")]
    public void ThenTheCreationShouldSucceed()
    {
        Assert.NotNull(_result);
        Assert.True(_result.Value.IsSuccess, _result.Value.ErrorDetails?.Detail ?? "Result failed");
    }

    [Then(@"the personal info should contain the provided data")]
    public void ThenThePersonalInfoShouldContainTheProvidedData()
    {
        Assert.NotNull(_result);
        Assert.True(_result.Value.IsSuccess);

        var info = _result.Value.Value;
        Assert.Equal(_firstName, info.FirstName);
        Assert.Equal(_lastName, info.LastName);
        Assert.Equal(_gender, info.Gender);
        Assert.Equal(_nationality, info.Nationality);
        Assert.Equal(_profession, info.Profession);
    }

    [Then(@"the creation should fail")]
    public void ThenTheCreationShouldFail()
    {
        Assert.NotNull(_result);
        Assert.True(_result.Value.IsFailure, "Expected failure but got success");
    }

    [Then(@"the error should be ""(.*)""")]
    public void ThenTheErrorShouldBe(string expectedError)
    {
        Assert.NotNull(_result);
        Assert.True(_result.Value.IsFailure, "Expected failure but got success");

        var errors = _result.Value.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? new List<string>();

        Assert.Contains(expectedError, allErrors);
    }

    [Given(@"I have personal information with first name ""(.*)""")]
    public void GivenIHavePersonalInformationWithFirstName(string firstName)
    {
        _firstName = firstName;
        _lastName = "Smith";
        _gender = "Male";
        _birthDate = new DateTime(1990, 5, 15);
        _nationality = "American";
        _profession = "Software Engineer";
    }

    [Given(@"I have personal information with last name ""(.*)""")]
    public void GivenIHavePersonalInformationWithLastName(string lastName)
    {
        _firstName = "John";
        _lastName = lastName;
        _gender = "Male";
        _birthDate = new DateTime(1990, 5, 15);
        _nationality = "American";
        _profession = "Software Engineer";
    }

    [Given(@"I have personal information with gender ""(.*)""")]
    public void GivenIHavePersonalInformationWithGender(string gender)
    {
        _firstName = "John";
        _lastName = "Smith";
        _gender = gender;
        _birthDate = new DateTime(1990, 5, 15);
        _nationality = "American";
        _profession = "Software Engineer";
    }

    [Given(@"I have personal information with nationality ""(.*)""")]
    public void GivenIHavePersonalInformationWithNationality(string nationality)
    {
        _firstName = "John";
        _lastName = "Smith";
        _gender = "Male";
        _birthDate = new DateTime(1990, 5, 15);
        _nationality = nationality;
        _profession = "Software Engineer";
    }

    [Given(@"I have personal information with null nationality")]
    public void GivenIHavePersonalInformationWithNullNationality()
    {
        _firstName = "John";
        _lastName = "Smith";
        _gender = "Male";
        _birthDate = new DateTime(1990, 5, 15);
        _nationality = null!;
        _profession = "Software Engineer";
    }

    [Given(@"I have personal information with profession ""(.*)""")]
    public void GivenIHavePersonalInformationWithProfession(string profession)
    {
        _firstName = "John";
        _lastName = "Smith";
        _gender = "Male";
        _birthDate = new DateTime(1990, 5, 15);
        _nationality = "American";
        _profession = profession;
    }

    [Given(@"I have personal information with null profession")]
    public void GivenIHavePersonalInformationWithNullProfession()
    {
        _firstName = "John";
        _lastName = "Smith";
        _gender = "Male";
        _birthDate = new DateTime(1990, 5, 15);
        _nationality = "American";
        _profession = null!;
    }

    [Given(@"I have personal information with birth date in the future")]
    public void GivenIHavePersonalInformationWithBirthDateInTheFuture()
    {
        _firstName = "John";
        _lastName = "Smith";
        _gender = "Male";
        _birthDate = DateTime.Now.AddDays(1);
        _nationality = "American";
        _profession = "Software Engineer";
    }
}
