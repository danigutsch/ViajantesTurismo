using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Admin.Domain.Customers;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
public sealed class CommonPersonalInfoSteps(PersonalInfoContext context)
{
    [Given(@"I have valid personal information")]
    public void GivenIHaveValidPersonalInformation()
    {
        context.FirstName = "John";
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = new DateTime(1990, 5, 15);
        context.Nationality = "American";
        context.Profession = "Software Engineer";
    }

    [When(@"I create the personal info")]
    public void WhenICreateThePersonalInfo()
    {
        context.Result = PersonalInfo.Create(
            context.FirstName,
            context.LastName,
            context.Gender,
            context.BirthDate,
            context.Nationality,
            context.Profession,
            TimeProvider.System);
    }

    [Then(@"the creation should succeed")]
    public void ThenTheCreationShouldSucceed()
    {
        Assert.True(context.Result!.Value.IsSuccess, context.Result.Value.ErrorDetails?.Detail ?? "Result failed");
    }

    [Then(@"the personal info should contain the provided data")]
    public void ThenThePersonalInfoShouldContainTheProvidedData()
    {
        Assert.True(context.Result!.Value.IsSuccess);

        var info = context.Result.Value.Value;
        Assert.Equal(context.FirstName, info.FirstName, StringComparer.Ordinal);
        Assert.Equal(context.LastName, info.LastName, StringComparer.Ordinal);
        Assert.Equal(context.Gender, info.Gender, StringComparer.Ordinal);
        Assert.Equal(context.Nationality, info.Nationality, StringComparer.Ordinal);
        Assert.Equal(context.Profession, info.Profession, StringComparer.Ordinal);
    }

    [Then(@"the creation should fail")]
    public void ThenTheCreationShouldFail()
    {
        Assert.True(context.Result!.Value.IsFailure, "Expected failure but got success");
    }

    [Then(@"the error should be ""(.*)""")]
    public void ThenTheErrorShouldBe(string expectedError)
    {
        Assert.True(context.Result!.Value.IsFailure, "Expected failure but got success");

        var errors = context.Result.Value.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? new List<string>();

        Assert.Contains(expectedError, allErrors);
    }

    [Given(@"I have personal information with first name ""(.*)""")]
    public void GivenIHavePersonalInformationWithFirstName(string firstName)
    {
        context.FirstName = firstName;
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = new DateTime(1990, 5, 15);
        context.Nationality = "American";
        context.Profession = "Software Engineer";
    }

    [Given(@"I have personal information with null first name")]
    public void GivenIHavePersonalInformationWithNullFirstName()
    {
        context.FirstName = null!;
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = new DateTime(1990, 5, 15);
        context.Nationality = "American";
        context.Profession = "Software Engineer";
    }

    [Given(@"I have personal information with last name ""(.*)""")]
    public void GivenIHavePersonalInformationWithLastName(string lastName)
    {
        context.FirstName = "John";
        context.LastName = lastName;
        context.Gender = "Male";
        context.BirthDate = new DateTime(1990, 5, 15);
        context.Nationality = "American";
        context.Profession = "Software Engineer";
    }

    [Given(@"I have personal information with null last name")]
    public void GivenIHavePersonalInformationWithNullLastName()
    {
        context.FirstName = "John";
        context.LastName = null!;
        context.Gender = "Male";
        context.BirthDate = new DateTime(1990, 5, 15);
        context.Nationality = "American";
        context.Profession = "Software Engineer";
    }

    [Given(@"I have personal information with gender ""(.*)""")]
    public void GivenIHavePersonalInformationWithGender(string gender)
    {
        context.FirstName = "John";
        context.LastName = "Smith";
        context.Gender = gender;
        context.BirthDate = new DateTime(1990, 5, 15);
        context.Nationality = "American";
        context.Profession = "Software Engineer";
    }

    [Given(@"I have personal information with null gender")]
    public void GivenIHavePersonalInformationWithNullGender()
    {
        context.FirstName = "John";
        context.LastName = "Smith";
        context.Gender = null!;
        context.BirthDate = new DateTime(1990, 5, 15);
        context.Nationality = "American";
        context.Profession = "Software Engineer";
    }

    [Given(@"I have personal information with nationality ""(.*)""")]
    public void GivenIHavePersonalInformationWithNationality(string nationality)
    {
        context.FirstName = "John";
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = new DateTime(1990, 5, 15);
        context.Nationality = nationality;
        context.Profession = "Software Engineer";
    }

    [Given(@"I have personal information with null nationality")]
    public void GivenIHavePersonalInformationWithNullNationality()
    {
        context.FirstName = "John";
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = new DateTime(1990, 5, 15);
        context.Nationality = null!;
        context.Profession = "Software Engineer";
    }

    [Given(@"I have personal information with profession ""(.*)""")]
    public void GivenIHavePersonalInformationWithProfession(string profession)
    {
        context.FirstName = "John";
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = new DateTime(1990, 5, 15);
        context.Nationality = "American";
        context.Profession = profession;
    }

    [Given(@"I have personal information with null profession")]
    public void GivenIHavePersonalInformationWithNullProfession()
    {
        context.FirstName = "John";
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = new DateTime(1990, 5, 15);
        context.Nationality = "American";
        context.Profession = null!;
    }

    [Given(@"I have personal information with birth date in the future")]
    public void GivenIHavePersonalInformationWithBirthDateInTheFuture()
    {
        context.FirstName = "John";
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = DateTime.UtcNow.Date.AddDays(1);
        context.Nationality = "American";
        context.Profession = "Software Engineer";
    }

    [Given(@"I have personal information with birth date today")]
    public void GivenIHavePersonalInformationWithBirthDateToday()
    {
        context.FirstName = "John";
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = DateTime.UtcNow.Date;
        context.Nationality = "American";
        context.Profession = "Software Engineer";
    }

    [Given(@"I have personal information with birth date one day in the future")]
    public void GivenIHavePersonalInformationWithBirthDateOneDayInTheFuture()
    {
        context.FirstName = "John";
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = DateTime.UtcNow.Date.AddDays(1);
        context.Nationality = "American";
        context.Profession = "Software Engineer";
    }

    [Given(@"I have personal information with birth date one day in the past")]
    public void GivenIHavePersonalInformationWithBirthDateOneDayInThePast()
    {
        context.FirstName = "John";
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = DateTime.UtcNow.Date.AddDays(-1);
        context.Nationality = "American";
        context.Profession = "Software Engineer";
    }

    [Given(@"I have personal information with birth date (\d+) years ago")]
    public void GivenIHavePersonalInformationWithBirthDateYearsAgo(int years)
    {
        context.FirstName = "John";
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = DateTime.UtcNow.Date.AddYears(-years);
        context.Nationality = "American";
        context.Profession = "Software Engineer";
    }

    [Given(@"I have personal information with first name of (\d+) characters")]
    public void GivenIHavePersonalInformationWithFirstNameOfLength(int length)
    {
        context.FirstName = new string('A', length);
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = new DateTime(1990, 5, 15);
        context.Nationality = "American";
        context.Profession = "Software Engineer";
    }

    [Given(@"I have personal information with last name of (\d+) characters")]
    public void GivenIHavePersonalInformationWithLastNameOfLength(int length)
    {
        context.FirstName = "John";
        context.LastName = new string('A', length);
        context.Gender = "Male";
        context.BirthDate = new DateTime(1990, 5, 15);
        context.Nationality = "American";
        context.Profession = "Software Engineer";
    }

    [Given(@"I have personal information with gender of (\d+) characters")]
    public void GivenIHavePersonalInformationWithGenderOfLength(int length)
    {
        context.FirstName = "John";
        context.LastName = "Smith";
        context.Gender = new string('A', length);
        context.BirthDate = new DateTime(1990, 5, 15);
        context.Nationality = "American";
        context.Profession = "Software Engineer";
    }

    [Given(@"I have personal information with nationality of (\d+) characters")]
    public void GivenIHavePersonalInformationWithNationalityOfLength(int length)
    {
        context.FirstName = "John";
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = new DateTime(1990, 5, 15);
        context.Nationality = new string('A', length);
        context.Profession = "Software Engineer";
    }

    [Given(@"I have personal information with profession of (\d+) characters")]
    public void GivenIHavePersonalInformationWithProfessionOfLength(int length)
    {
        context.FirstName = "John";
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = new DateTime(1990, 5, 15);
        context.Nationality = "American";
        context.Profession = new string('A', length);
    }
}
