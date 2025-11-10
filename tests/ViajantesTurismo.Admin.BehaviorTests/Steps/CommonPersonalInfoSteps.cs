using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Admin.Domain.Customers;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
[Scope(Feature = "Personal Information Validation")]
[Scope(Feature = "First Name Validation")]
[Scope(Feature = "Last Name Validation")]
[Scope(Feature = "Gender Validation")]
[Scope(Feature = "Nationality Validation")]
[Scope(Feature = "Profession Validation")]
[Scope(Feature = "Birth Date Validation")]
[Scope(Feature = "Customer Management")]
[Scope(Feature = "Customer Creation")]
[Scope(Feature = "Customer Sanitization")]
public sealed class CommonPersonalInfoSteps(PersonalInfoContext context)
{
    [Given("I have valid personal information")]
    public void GivenIHaveValidPersonalInformation()
    {
        context.FirstName = "John";
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = new DateTime(1990, 5, 15);
        context.Nationality = "American";
        context.Profession = "Software Engineer";
    }

    [When("I create the personal info")]
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

    [Then("the creation should succeed")]
    public void ThenTheCreationShouldSucceed()
    {
        Assert.True(context.Result.IsSuccess, context.Result.ErrorDetails?.Detail ?? "Result failed");
    }

    [Then("the personal info should be successfully created")]
    public void ThenThePersonalInfoShouldBeSuccessfullyCreated()
    {
        Assert.True(context.Result.IsSuccess, context.Result.ErrorDetails?.Detail ?? "Result failed");
    }

    [When(@"I attempt to create personal info with first name ""(.*)""")]
    public void WhenIAttemptToCreatePersonalInfoWithFirstName(string firstName)
    {
        context.FirstName = firstName;
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = new DateTime(1990, 5, 15);
        context.Nationality = "American";
        context.Profession = "Software Engineer";
        WhenICreateThePersonalInfo();
    }

    [When("I attempt to create personal info with null first name")]
    public void WhenIAttemptToCreatePersonalInfoWithNullFirstName()
    {
        context.FirstName = null!;
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = new DateTime(1990, 5, 15);
        context.Nationality = "American";
        context.Profession = "Software Engineer";
        WhenICreateThePersonalInfo();
    }

    [When(@"I attempt to create personal info with first name of (\d+) characters")]
    public void WhenIAttemptToCreatePersonalInfoWithFirstNameOfCharacters(int characterCount)
    {
        context.FirstName = new string('A', characterCount);
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = new DateTime(1990, 5, 15);
        context.Nationality = "American";
        context.Profession = "Software Engineer";
        WhenICreateThePersonalInfo();
    }

    [When(@"I create personal info with first name ""(.*)""")]
    public void WhenICreatePersonalInfoWithFirstName(string firstName)
    {
        context.FirstName = firstName;
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = new DateTime(1990, 5, 15);
        context.Nationality = "American";
        context.Profession = "Software Engineer";
        WhenICreateThePersonalInfo();
    }

    [When(@"I create personal info with first name of (\d+) characters")]
    public void WhenICreatePersonalInfoWithFirstNameOfCharacters(int characterCount)
    {
        WhenICreatePersonalInfoWithFirstName(new string('A', characterCount));
    }

    [Then("I should not be able to create the personal info")]
    public void ThenIShouldNotBeAbleToCreateThePersonalInfo()
    {
        Assert.True(context.Result.IsFailure, "Expected personal info creation to fail, but it succeeded.");
    }

    [Then("I should be informed that first name is required")]
    public void ThenIShouldBeInformedThatFirstNameIsRequired()
    {
        Assert.True(context.Result.IsFailure);
        Assert.True(context.Result.ErrorDetails?.ValidationErrors?.ContainsKey("FirstName") ?? false,
            "Expected validation error for FirstName");
    }

    [Then(@"I should be informed that first name cannot exceed (\d+) characters")]
    public void ThenIShouldBeInformedThatFirstNameCannotExceedCharacters(int maxLength)
    {
        Assert.True(context.Result.IsFailure);
        Assert.True(context.Result.ErrorDetails?.ValidationErrors?.ContainsKey("FirstName") ?? false,
            "Expected validation error for FirstName");
    }

    [Then("the personal info should contain the provided data")]
    public void ThenThePersonalInfoShouldContainTheProvidedData()
    {
        Assert.True(context.Result.IsSuccess);

        var info = context.Result.Value;
        Assert.Equal(context.FirstName, info.FirstName, StringComparer.Ordinal);
        Assert.Equal(context.LastName, info.LastName, StringComparer.Ordinal);
        Assert.Equal(context.Gender, info.Gender, StringComparer.Ordinal);
        Assert.Equal(context.Nationality, info.Nationality, StringComparer.Ordinal);
        Assert.Equal(context.Profession, info.Profession, StringComparer.Ordinal);
    }

    [Then("the creation should fail")]
    public void ThenTheCreationShouldFail()
    {
        Assert.True(context.Result.IsFailure, "Expected failure but got success");
    }

    [Then(@"the error should be ""(.*)""")]
    public void ThenTheErrorShouldBe(string expectedError)
    {
        Assert.True(context.Result.IsFailure, "Expected failure but got success");

        var errors = context.Result.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? [];

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

    [Given("I have personal information with null first name")]
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

    [Given("I have personal information with null last name")]
    public void GivenIHavePersonalInformationWithNullLastName()
    {
        context.FirstName = "John";
        context.LastName = null!;
        context.Gender = "Male";
        context.BirthDate = new DateTime(1990, 5, 15);
        context.Nationality = "American";
        context.Profession = "Software Engineer";
    }

    [When("I attempt to create personal info with null last name")]
    public void WhenIAttemptToCreatePersonalInfoWithNullLastName()
    {
        context.FirstName = "John";
        context.LastName = null!;
        context.Gender = "Male";
        context.BirthDate = new DateTime(1990, 5, 15);
        context.Nationality = "American";
        context.Profession = "Software Engineer";
        WhenICreateThePersonalInfo();
    }

    [When(@"I attempt to create personal info with last name ""(.*)""")]
    public void WhenIAttemptToCreatePersonalInfoWithLastName(string lastName)
    {
        context.FirstName = "John";
        context.LastName = lastName;
        context.Gender = "Male";
        context.BirthDate = new DateTime(1990, 5, 15);
        context.Nationality = "American";
        context.Profession = "Software Engineer";
        WhenICreateThePersonalInfo();
    }

    [When(@"I attempt to create personal info with last name of (\d+) characters")]
    public void WhenIAttemptToCreatePersonalInfoWithLastNameOfCharacters(int characterCount)
    {
        context.FirstName = "John";
        context.LastName = new string('B', characterCount);
        context.Gender = "Male";
        context.BirthDate = new DateTime(1990, 5, 15);
        context.Nationality = "American";
        context.Profession = "Software Engineer";
        WhenICreateThePersonalInfo();
    }

    [When(@"I create personal info with last name ""(.*)""")]
    public void WhenICreatePersonalInfoWithLastName(string lastName)
    {
        context.FirstName = "John";
        context.LastName = lastName;
        context.Gender = "Male";
        context.BirthDate = new DateTime(1990, 5, 15);
        context.Nationality = "American";
        context.Profession = "Software Engineer";
        WhenICreateThePersonalInfo();
    }

    [When(@"I create personal info with last name of (\d+) characters")]
    public void WhenICreatePersonalInfoWithLastNameOfCharacters(int characterCount)
    {
        WhenICreatePersonalInfoWithLastName(new string('B', characterCount));
    }

    [Then("I should be informed that last name is required")]
    public void ThenIShouldBeInformedThatLastNameIsRequired()
    {
        Assert.True(context.Result.IsFailure);
        Assert.True(context.Result.ErrorDetails?.ValidationErrors?.ContainsKey("LastName") ?? false,
            "Expected validation error for LastName");
    }

    [Then(@"I should be informed that last name cannot exceed (\d+) characters")]
    public void ThenIShouldBeInformedThatLastNameCannotExceedCharacters(int maxLength)
    {
        Assert.True(context.Result.IsFailure);
        Assert.True(context.Result.ErrorDetails?.ValidationErrors?.ContainsKey("LastName") ?? false,
            "Expected validation error for LastName");
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

    [Given("I have personal information with null gender")]
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

    [Given("I have personal information with null nationality")]
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

    [Given("I have personal information with null profession")]
    public void GivenIHavePersonalInformationWithNullProfession()
    {
        context.FirstName = "John";
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = new DateTime(1990, 5, 15);
        context.Nationality = "American";
        context.Profession = null!;
    }

    [Given("I have personal information with birth date in the future")]
    public void GivenIHavePersonalInformationWithBirthDateInTheFuture()
    {
        context.FirstName = "John";
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = DateTime.UtcNow.Date.AddDays(1);
        context.Nationality = "American";
        context.Profession = "Software Engineer";
    }

    [Given("I have personal information with birth date today")]
    public void GivenIHavePersonalInformationWithBirthDateToday()
    {
        context.FirstName = "John";
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = DateTime.UtcNow.Date;
        context.Nationality = "American";
        context.Profession = "Software Engineer";
    }

    [Given("I have personal information with birth date one day in the future")]
    public void GivenIHavePersonalInformationWithBirthDateOneDayInTheFuture()
    {
        context.FirstName = "John";
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = DateTime.UtcNow.Date.AddDays(1);
        context.Nationality = "American";
        context.Profession = "Software Engineer";
    }

    [Given("I have personal information with birth date one day in the past")]
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

    [When("I attempt to create personal info with birth date in the future")]
    public void WhenIAttemptToCreatePersonalInfoWithBirthDateInTheFuture()
    {
        context.FirstName = "John";
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = DateTime.UtcNow.Date.AddDays(1);
        context.Nationality = "American";
        context.Profession = "Software Engineer";
        WhenICreateThePersonalInfo();
    }

    [When("I attempt to create personal info with birth date one day in the future")]
    public void WhenIAttemptToCreatePersonalInfoWithBirthDateOneDayInTheFuture()
    {
        context.FirstName = "John";
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = DateTime.UtcNow.Date.AddDays(1);
        context.Nationality = "American";
        context.Profession = "Software Engineer";
        WhenICreateThePersonalInfo();
    }

    [When("I create personal info with birth date today")]
    public void WhenICreatePersonalInfoWithBirthDateToday()
    {
        context.FirstName = "John";
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = DateTime.UtcNow.Date;
        context.Nationality = "American";
        context.Profession = "Software Engineer";
        WhenICreateThePersonalInfo();
    }

    [When("I create personal info with birth date one day in the past")]
    public void WhenICreatePersonalInfoWithBirthDateOneDayInThePast()
    {
        context.FirstName = "John";
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = DateTime.UtcNow.Date.AddDays(-1);
        context.Nationality = "American";
        context.Profession = "Software Engineer";
        WhenICreateThePersonalInfo();
    }

    [When(@"I create personal info with birth date (\d+) years ago")]
    public void WhenICreatePersonalInfoWithBirthDateYearsAgo(int years)
    {
        context.FirstName = "John";
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = DateTime.UtcNow.Date.AddYears(-years);
        context.Nationality = "American";
        context.Profession = "Software Engineer";
        WhenICreateThePersonalInfo();
    }

    [Then("I should be informed that birth date cannot be in the future")]
    public void ThenIShouldBeInformedThatBirthDateCannotBeInTheFuture()
    {
        Assert.True(context.Result.IsFailure);
        Assert.True(context.Result.ErrorDetails?.ValidationErrors?.ContainsKey("BirthDate") ?? false,
            "Expected validation error for BirthDate");
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

    [When("I attempt to create personal info without gender")]
    public void WhenIAttemptToCreatePersonalInfoWithoutGender()
    {
        context.Result = PersonalInfo.Create(
            "John",
            "Smith",
            null!,
            new DateTime(1990, 5, 15),
            "American",
            "Software Engineer",
            TimeProvider.System
        );
    }

    [When("I attempt to create personal info with whitespace-only gender")]
    public void WhenIAttemptToCreatePersonalInfoWithWhitespaceOnlyGender()
    {
        context.Result = PersonalInfo.Create(
            "John",
            "Smith",
            "   ",
            new DateTime(1990, 5, 15),
            "American",
            "Software Engineer",
            TimeProvider.System
        );
    }

    [When(@"I create personal info with gender of (\d+) characters")]
    public void WhenICreatePersonalInfoWithGenderOfCharacters(int length)
    {
        context.Result = PersonalInfo.Create(
            "John",
            "Smith",
            new string('A', length),
            new DateTime(1990, 5, 15),
            "American",
            "Software Engineer",
            TimeProvider.System
        );
        if (context.Result.IsSuccess)
        {
            context.PersonalInfo = context.Result.Value;
        }
    }

    [When(@"I attempt to create personal info with gender of (\d+) characters")]
    public void WhenIAttemptToCreatePersonalInfoWithGenderOfCharacters(int length)
    {
        context.Result = PersonalInfo.Create(
            "John",
            "Smith",
            new string('A', length),
            new DateTime(1990, 5, 15),
            "American",
            "Software Engineer",
            TimeProvider.System
        );
    }

    [When(@"I create personal info with gender ""([^""]*)""")]
    public void WhenICreatePersonalInfoWithGender(string gender)
    {
        context.Result = PersonalInfo.Create(
            "John",
            "Smith",
            gender,
            new DateTime(1990, 5, 15),
            "American",
            "Software Engineer",
            TimeProvider.System
        );
        if (context.Result.IsSuccess)
        {
            context.PersonalInfo = context.Result.Value;
        }
    }

    [Then("I should be informed that gender is required")]
    public void ThenIShouldBeInformedThatGenderIsRequired()
    {
        Assert.True(context.Result.IsFailure, "Expected failure but got success");
        var errors = context.Result.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? [];
        Assert.Contains("Gender is required.", allErrors);
    }

    [Then("I should be informed that gender cannot exceed 64 characters")]
    public void ThenIShouldBeInformedThatGenderCannotExceed64Characters()
    {
        Assert.True(context.Result.IsFailure, "Expected failure but got success");
        var errors = context.Result.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? [];
        Assert.Contains("Gender cannot exceed 64 characters.", allErrors);
    }

    [Then(@"the gender should be ""([^""]*)""")]
    public void ThenTheGenderShouldBe(string expectedGender)
    {
        Assert.Equal(expectedGender, context.PersonalInfo.Gender);
    }

    [When("I attempt to create personal info without nationality")]
    public void WhenIAttemptToCreatePersonalInfoWithoutNationality()
    {
        context.Result = PersonalInfo.Create(
            "John",
            "Smith",
            "Male",
            new DateTime(1990, 5, 15),
            null!,
            "Software Engineer",
            TimeProvider.System
        );
    }

    [When("I attempt to create personal info with whitespace-only nationality")]
    public void WhenIAttemptToCreatePersonalInfoWithWhitespaceOnlyNationality()
    {
        context.Result = PersonalInfo.Create(
            "John",
            "Smith",
            "Male",
            new DateTime(1990, 5, 15),
            "   ",
            "Software Engineer",
            TimeProvider.System
        );
    }

    [When(@"I create personal info with nationality of (\d+) characters")]
    public void WhenICreatePersonalInfoWithNationalityOfCharacters(int length)
    {
        context.Result = PersonalInfo.Create(
            "John",
            "Smith",
            "Male",
            new DateTime(1990, 5, 15),
            new string('A', length),
            "Software Engineer",
            TimeProvider.System
        );
        if (context.Result.IsSuccess)
        {
            context.PersonalInfo = context.Result.Value;
        }
    }

    [When(@"I attempt to create personal info with nationality of (\d+) characters")]
    public void WhenIAttemptToCreatePersonalInfoWithNationalityOfCharacters(int length)
    {
        context.Result = PersonalInfo.Create(
            "John",
            "Smith",
            "Male",
            new DateTime(1990, 5, 15),
            new string('A', length),
            "Software Engineer",
            TimeProvider.System
        );
    }

    [When(@"I create personal info with nationality ""([^""]*)""")]
    public void WhenICreatePersonalInfoWithNationality(string nationality)
    {
        context.Result = PersonalInfo.Create(
            "John",
            "Smith",
            "Male",
            new DateTime(1990, 5, 15),
            nationality,
            "Software Engineer",
            TimeProvider.System
        );
        if (context.Result.IsSuccess)
        {
            context.PersonalInfo = context.Result.Value;
        }
    }

    [Then("I should be informed that nationality is required")]
    public void ThenIShouldBeInformedThatNationalityIsRequired()
    {
        Assert.True(context.Result.IsFailure, "Expected failure but got success");
        var errors = context.Result.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? [];
        Assert.Contains("Nationality is required.", allErrors);
    }

    [Then("I should be informed that nationality cannot exceed 128 characters")]
    public void ThenIShouldBeInformedThatNationalityCannotExceed128Characters()
    {
        Assert.True(context.Result.IsFailure, "Expected failure but got success");
        var errors = context.Result.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? [];
        Assert.Contains("Nationality cannot exceed 128 characters.", allErrors);
    }

    [Then(@"the nationality should be ""([^""]*)""")]
    public void ThenTheNationalityShouldBe(string expectedNationality)
    {
        Assert.Equal(expectedNationality, context.PersonalInfo.Nationality);
    }

    [When("I attempt to create personal info without profession")]
    public void WhenIAttemptToCreatePersonalInfoWithoutProfession()
    {
        context.Result = PersonalInfo.Create(
            "John",
            "Smith",
            "Male",
            new DateTime(1990, 5, 15),
            "American",
            null!,
            TimeProvider.System
        );
    }

    [When("I attempt to create personal info with whitespace-only profession")]
    public void WhenIAttemptToCreatePersonalInfoWithWhitespaceOnlyProfession()
    {
        context.Result = PersonalInfo.Create(
            "John",
            "Smith",
            "Male",
            new DateTime(1990, 5, 15),
            "American",
            "   ",
            TimeProvider.System
        );
    }

    [When(@"I create personal info with profession of (\d+) characters")]
    public void WhenICreatePersonalInfoWithProfessionOfCharacters(int length)
    {
        context.Result = PersonalInfo.Create(
            "John",
            "Smith",
            "Male",
            new DateTime(1990, 5, 15),
            "American",
            new string('A', length),
            TimeProvider.System
        );
        if (context.Result.IsSuccess)
        {
            context.PersonalInfo = context.Result.Value;
        }
    }

    [When(@"I attempt to create personal info with profession of (\d+) characters")]
    public void WhenIAttemptToCreatePersonalInfoWithProfessionOfCharacters(int length)
    {
        context.Result = PersonalInfo.Create(
            "John",
            "Smith",
            "Male",
            new DateTime(1990, 5, 15),
            "American",
            new string('A', length),
            TimeProvider.System
        );
    }

    [When(@"I create personal info with profession ""([^""]*)""")]
    public void WhenICreatePersonalInfoWithProfession(string profession)
    {
        context.Result = PersonalInfo.Create(
            "John",
            "Smith",
            "Male",
            new DateTime(1990, 5, 15),
            "American",
            profession,
            TimeProvider.System
        );
        if (context.Result.IsSuccess)
        {
            context.PersonalInfo = context.Result.Value;
        }
    }

    [Then("I should be informed that profession is required")]
    public void ThenIShouldBeInformedThatProfessionIsRequired()
    {
        Assert.True(context.Result.IsFailure, "Expected failure but got success");
        var errors = context.Result.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? [];
        Assert.Contains("Profession is required.", allErrors);
    }

    [Then("I should be informed that profession cannot exceed 128 characters")]
    public void ThenIShouldBeInformedThatProfessionCannotExceed128Characters()
    {
        Assert.True(context.Result.IsFailure, "Expected failure but got success");
        var errors = context.Result.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? [];
        Assert.Contains("Profession cannot exceed 128 characters.", allErrors);
    }

    [Then(@"the profession should be ""([^""]*)""")]
    public void ThenTheProfessionShouldBe(string expectedProfession)
    {
        Assert.Equal(expectedProfession, context.PersonalInfo.Profession);
    }
}
