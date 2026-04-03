namespace ViajantesTurismo.Admin.BehaviorTests.Steps.Shared;

[Binding]
[Scope(Feature = "Personal Information Validation")]
[Scope(Feature = "First Name Validation")]
[Scope(Feature = "Last Name Validation")]
[Scope(Feature = "Gender Validation")]
[Scope(Feature = "Nationality Validation")]
[Scope(Feature = "Occupation Validation")]
[Scope(Feature = "Birth Date Validation")]
[Scope(Feature = "Customer Management")]
[Scope(Feature = "Customer Creation")]
[Scope(Feature = "Customer Sanitization")]
public sealed class CommonPersonalInfoSteps(CustomerContext context)
{
    private static readonly DateTime ValidBirthDate = new(1990, 5, 15, 0, 0, 0, DateTimeKind.Utc);

    [Given("I have valid personal information")]
    public void GivenIHaveValidPersonalInformation()
    {
        context.FirstName = "John";
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = ValidBirthDate;
        context.Nationality = "American";
        context.Occupation = "Software Engineer";
    }

    [When("I create the personal info")]
    public void WhenICreateThePersonalInfo()
    {
        context.PersonalInfoResult = PersonalInfo.Create(
            context.FirstName,
            context.LastName,
            context.Gender,
            context.BirthDate,
            context.Nationality,
            context.Occupation,
            TimeProvider.System);
    }

    [Then("the creation should succeed")]
    public void ThenTheCreationShouldSucceed()
    {
        Assert.True(context.PersonalInfoResult.IsSuccess, context.PersonalInfoResult.ErrorDetails?.Detail ?? "Result failed");
    }

    [Then("the personal info should be successfully created")]
    public void ThenThePersonalInfoShouldBeSuccessfullyCreated()
    {
        Assert.True(context.PersonalInfoResult.IsSuccess, context.PersonalInfoResult.ErrorDetails?.Detail ?? "Result failed");
    }

    [When(@"I attempt to create personal info with first name ""(.*)""")]
    public void WhenIAttemptToCreatePersonalInfoWithFirstName(string firstName)
    {
        context.FirstName = firstName;
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = ValidBirthDate;
        context.Nationality = "American";
        context.Occupation = "Software Engineer";
        WhenICreateThePersonalInfo();
    }

    [When("I attempt to create personal info with null first name")]
    public void WhenIAttemptToCreatePersonalInfoWithNullFirstName()
    {
        context.FirstName = null!;
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = ValidBirthDate;
        context.Nationality = "American";
        context.Occupation = "Software Engineer";
        WhenICreateThePersonalInfo();
    }

    [When(@"I attempt to create personal info with first name of (\d+) characters")]
    public void WhenIAttemptToCreatePersonalInfoWithFirstNameOfCharacters(int characterCount)
    {
        context.FirstName = new string('A', characterCount);
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = ValidBirthDate;
        context.Nationality = "American";
        context.Occupation = "Software Engineer";
        WhenICreateThePersonalInfo();
    }

    [When(@"I create personal info with first name ""(.*)""")]
    public void WhenICreatePersonalInfoWithFirstName(string firstName)
    {
        context.FirstName = firstName;
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = ValidBirthDate;
        context.Nationality = "American";
        context.Occupation = "Software Engineer";
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
        Assert.True(context.PersonalInfoResult.IsFailure, "Expected personal info creation to fail, but it succeeded.");
    }

    [Then("I should be informed that first name is required")]
    public void ThenIShouldBeInformedThatFirstNameIsRequired()
    {
        Assert.True(context.PersonalInfoResult.IsFailure);
        Assert.True(context.PersonalInfoResult.ErrorDetails?.ValidationErrors?.ContainsKey("FirstName") ?? false,
            "Expected validation error for FirstName");
    }

    [Then(@"I should be informed that first name cannot exceed (\d+) characters")]
    public void ThenIShouldBeInformedThatFirstNameCannotExceedCharacters(int maxLength)
    {
        Assert.True(context.PersonalInfoResult.IsFailure);
        Assert.True(context.PersonalInfoResult.ErrorDetails?.ValidationErrors?.ContainsKey("FirstName") ?? false,
            "Expected validation error for FirstName");
    }

    [Then("the personal info should contain the provided data")]
    public void ThenThePersonalInfoShouldContainTheProvidedData()
    {
        Assert.True(context.PersonalInfoResult.IsSuccess);

        var info = context.PersonalInfoResult.Value;
        Assert.Equal(context.FirstName, info.FirstName, StringComparer.Ordinal);
        Assert.Equal(context.LastName, info.LastName, StringComparer.Ordinal);
        Assert.Equal(context.Gender, info.Gender, StringComparer.Ordinal);
        Assert.Equal(context.Nationality, info.Nationality, StringComparer.Ordinal);
        Assert.Equal(context.Occupation, info.Occupation, StringComparer.Ordinal);
    }

    [Then("the creation should fail")]
    public void ThenTheCreationShouldFail()
    {
        Assert.True(context.PersonalInfoResult.IsFailure, "Expected failure but got success");
    }

    [Then(@"the error should be ""(.*)""")]
    public void ThenTheErrorShouldBe(string expectedError)
    {
        Assert.True(context.PersonalInfoResult.IsFailure, "Expected failure but got success");

        var errors = context.PersonalInfoResult.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? [];

        Assert.Contains(expectedError, allErrors);
    }

    [Given(@"I have personal information with first name ""(.*)""")]
    public void GivenIHavePersonalInformationWithFirstName(string firstName)
    {
        context.FirstName = firstName;
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = ValidBirthDate;
        context.Nationality = "American";
        context.Occupation = "Software Engineer";
    }

    [Given("I have personal information with null first name")]
    public void GivenIHavePersonalInformationWithNullFirstName()
    {
        context.FirstName = null!;
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = ValidBirthDate;
        context.Nationality = "American";
        context.Occupation = "Software Engineer";
    }

    [Given(@"I have personal information with last name ""(.*)""")]
    public void GivenIHavePersonalInformationWithLastName(string lastName)
    {
        context.FirstName = "John";
        context.LastName = lastName;
        context.Gender = "Male";
        context.BirthDate = ValidBirthDate;
        context.Nationality = "American";
        context.Occupation = "Software Engineer";
    }

    [Given("I have personal information with null last name")]
    public void GivenIHavePersonalInformationWithNullLastName()
    {
        context.FirstName = "John";
        context.LastName = null!;
        context.Gender = "Male";
        context.BirthDate = ValidBirthDate;
        context.Nationality = "American";
        context.Occupation = "Software Engineer";
    }

    [When("I attempt to create personal info with null last name")]
    public void WhenIAttemptToCreatePersonalInfoWithNullLastName()
    {
        context.FirstName = "John";
        context.LastName = null!;
        context.Gender = "Male";
        context.BirthDate = ValidBirthDate;
        context.Nationality = "American";
        context.Occupation = "Software Engineer";
        WhenICreateThePersonalInfo();
    }

    [When(@"I attempt to create personal info with last name ""(.*)""")]
    public void WhenIAttemptToCreatePersonalInfoWithLastName(string lastName)
    {
        context.FirstName = "John";
        context.LastName = lastName;
        context.Gender = "Male";
        context.BirthDate = ValidBirthDate;
        context.Nationality = "American";
        context.Occupation = "Software Engineer";
        WhenICreateThePersonalInfo();
    }

    [When(@"I attempt to create personal info with last name of (\d+) characters")]
    public void WhenIAttemptToCreatePersonalInfoWithLastNameOfCharacters(int characterCount)
    {
        context.FirstName = "John";
        context.LastName = new string('B', characterCount);
        context.Gender = "Male";
        context.BirthDate = ValidBirthDate;
        context.Nationality = "American";
        context.Occupation = "Software Engineer";
        WhenICreateThePersonalInfo();
    }

    [When(@"I create personal info with last name ""(.*)""")]
    public void WhenICreatePersonalInfoWithLastName(string lastName)
    {
        context.FirstName = "John";
        context.LastName = lastName;
        context.Gender = "Male";
        context.BirthDate = ValidBirthDate;
        context.Nationality = "American";
        context.Occupation = "Software Engineer";
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
        Assert.True(context.PersonalInfoResult.IsFailure);
        Assert.True(context.PersonalInfoResult.ErrorDetails?.ValidationErrors?.ContainsKey("LastName") ?? false,
            "Expected validation error for LastName");
    }

    [Then(@"I should be informed that last name cannot exceed (\d+) characters")]
    public void ThenIShouldBeInformedThatLastNameCannotExceedCharacters(int maxLength)
    {
        Assert.True(context.PersonalInfoResult.IsFailure);
        Assert.True(context.PersonalInfoResult.ErrorDetails?.ValidationErrors?.ContainsKey("LastName") ?? false,
            "Expected validation error for LastName");
    }

    [Given(@"I have personal information with gender ""(.*)""")]
    public void GivenIHavePersonalInformationWithGender(string gender)
    {
        context.FirstName = "John";
        context.LastName = "Smith";
        context.Gender = gender;
        context.BirthDate = ValidBirthDate;
        context.Nationality = "American";
        context.Occupation = "Software Engineer";
    }

    [Given("I have personal information with null gender")]
    public void GivenIHavePersonalInformationWithNullGender()
    {
        context.FirstName = "John";
        context.LastName = "Smith";
        context.Gender = null!;
        context.BirthDate = ValidBirthDate;
        context.Nationality = "American";
        context.Occupation = "Software Engineer";
    }

    [Given(@"I have personal information with nationality ""(.*)""")]
    public void GivenIHavePersonalInformationWithNationality(string nationality)
    {
        context.FirstName = "John";
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = ValidBirthDate;
        context.Nationality = nationality;
        context.Occupation = "Software Engineer";
    }

    [Given("I have personal information with null nationality")]
    public void GivenIHavePersonalInformationWithNullNationality()
    {
        context.FirstName = "John";
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = ValidBirthDate;
        context.Nationality = null!;
        context.Occupation = "Software Engineer";
    }

    [Given(@"I have personal information with occupation ""(.*)""")]
    public void GivenIHavePersonalInformationWithOccupation(string occupation)
    {
        context.FirstName = "John";
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = ValidBirthDate;
        context.Nationality = "American";
        context.Occupation = occupation;
    }

    [Given("I have personal information with null occupation")]
    public void GivenIHavePersonalInformationWithNullOccupation()
    {
        context.FirstName = "John";
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = ValidBirthDate;
        context.Nationality = "American";
        context.Occupation = null!;
    }

    [Given("I have personal information with birth date in the future")]
    public void GivenIHavePersonalInformationWithBirthDateInTheFuture()
    {
        context.FirstName = "John";
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = DateTime.UtcNow.Date.AddDays(1);
        context.Nationality = "American";
        context.Occupation = "Software Engineer";
    }

    [Given("I have personal information with birth date today")]
    public void GivenIHavePersonalInformationWithBirthDateToday()
    {
        context.FirstName = "John";
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = DateTime.UtcNow.Date;
        context.Nationality = "American";
        context.Occupation = "Software Engineer";
    }

    [Given("I have personal information with birth date one day in the future")]
    public void GivenIHavePersonalInformationWithBirthDateOneDayInTheFuture()
    {
        context.FirstName = "John";
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = DateTime.UtcNow.Date.AddDays(1);
        context.Nationality = "American";
        context.Occupation = "Software Engineer";
    }

    [Given("I have personal information with birth date one day in the past")]
    public void GivenIHavePersonalInformationWithBirthDateOneDayInThePast()
    {
        context.FirstName = "John";
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = DateTime.UtcNow.Date.AddDays(-1);
        context.Nationality = "American";
        context.Occupation = "Software Engineer";
    }

    [Given(@"I have personal information with birth date (\d+) years ago")]
    public void GivenIHavePersonalInformationWithBirthDateYearsAgo(int years)
    {
        context.FirstName = "John";
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = DateTime.UtcNow.Date.AddYears(-years);
        context.Nationality = "American";
        context.Occupation = "Software Engineer";
    }

    [When("I attempt to create personal info with birth date in the future")]
    public void WhenIAttemptToCreatePersonalInfoWithBirthDateInTheFuture()
    {
        context.FirstName = "John";
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = DateTime.UtcNow.Date.AddDays(1);
        context.Nationality = "American";
        context.Occupation = "Software Engineer";
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
        context.Occupation = "Software Engineer";
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
        context.Occupation = "Software Engineer";
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
        context.Occupation = "Software Engineer";
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
        context.Occupation = "Software Engineer";
        WhenICreateThePersonalInfo();
    }

    [Then("I should be informed that birth date cannot be in the future")]
    public void ThenIShouldBeInformedThatBirthDateCannotBeInTheFuture()
    {
        Assert.True(context.PersonalInfoResult.IsFailure);
        Assert.True(context.PersonalInfoResult.ErrorDetails?.ValidationErrors?.ContainsKey("BirthDate") ?? false,
            "Expected validation error for BirthDate");
    }

    [Given(@"I have personal information with first name of (\d+) characters")]
    public void GivenIHavePersonalInformationWithFirstNameOfLength(int length)
    {
        context.FirstName = new string('A', length);
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = ValidBirthDate;
        context.Nationality = "American";
        context.Occupation = "Software Engineer";
    }

    [Given(@"I have personal information with last name of (\d+) characters")]
    public void GivenIHavePersonalInformationWithLastNameOfLength(int length)
    {
        context.FirstName = "John";
        context.LastName = new string('A', length);
        context.Gender = "Male";
        context.BirthDate = ValidBirthDate;
        context.Nationality = "American";
        context.Occupation = "Software Engineer";
    }

    [Given(@"I have personal information with gender of (\d+) characters")]
    public void GivenIHavePersonalInformationWithGenderOfLength(int length)
    {
        context.FirstName = "John";
        context.LastName = "Smith";
        context.Gender = new string('A', length);
        context.BirthDate = ValidBirthDate;
        context.Nationality = "American";
        context.Occupation = "Software Engineer";
    }

    [Given(@"I have personal information with nationality of (\d+) characters")]
    public void GivenIHavePersonalInformationWithNationalityOfLength(int length)
    {
        context.FirstName = "John";
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = ValidBirthDate;
        context.Nationality = new string('A', length);
        context.Occupation = "Software Engineer";
    }

    [Given(@"I have personal information with occupation of (\d+) characters")]
    public void GivenIHavePersonalInformationWithOccupationOfLength(int length)
    {
        context.FirstName = "John";
        context.LastName = "Smith";
        context.Gender = "Male";
        context.BirthDate = ValidBirthDate;
        context.Nationality = "American";
        context.Occupation = new string('A', length);
    }

    [When("I attempt to create personal info without gender")]
    public void WhenIAttemptToCreatePersonalInfoWithoutGender()
    {
        context.PersonalInfoResult = PersonalInfo.Create(
            "John",
            "Smith",
            null!,
            ValidBirthDate,
            "American",
            "Software Engineer",
            TimeProvider.System
        );
    }

    [When("I attempt to create personal info with whitespace-only gender")]
    public void WhenIAttemptToCreatePersonalInfoWithWhitespaceOnlyGender()
    {
        context.PersonalInfoResult = PersonalInfo.Create(
            "John",
            "Smith",
            "   ",
            ValidBirthDate,
            "American",
            "Software Engineer",
            TimeProvider.System
        );
    }

    [When(@"I create personal info with gender of (\d+) characters")]
    public void WhenICreatePersonalInfoWithGenderOfCharacters(int length)
    {
        context.PersonalInfoResult = PersonalInfo.Create(
            "John",
            "Smith",
            new string('A', length),
            ValidBirthDate,
            "American",
            "Software Engineer",
            TimeProvider.System
        );
    }

    [When(@"I attempt to create personal info with gender of (\d+) characters")]
    public void WhenIAttemptToCreatePersonalInfoWithGenderOfCharacters(int length)
    {
        context.PersonalInfoResult = PersonalInfo.Create(
            "John",
            "Smith",
            new string('A', length),
            ValidBirthDate,
            "American",
            "Software Engineer",
            TimeProvider.System
        );
    }

    [When(@"I create personal info with gender ""([^""]*)""")]
    public void WhenICreatePersonalInfoWithGender(string gender)
    {
        context.PersonalInfoResult = PersonalInfo.Create(
            "John",
            "Smith",
            gender,
            ValidBirthDate,
            "American",
            "Software Engineer",
            TimeProvider.System
        );
    }

    [Then("I should be informed that gender is required")]
    public void ThenIShouldBeInformedThatGenderIsRequired()
    {
        Assert.True(context.PersonalInfoResult.IsFailure, "Expected failure but got success");
        var errors = context.PersonalInfoResult.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? [];
        Assert.Contains("Gender is required.", allErrors);
    }

    [Then("I should be informed that gender cannot exceed 64 characters")]
    public void ThenIShouldBeInformedThatGenderCannotExceed64Characters()
    {
        Assert.True(context.PersonalInfoResult.IsFailure, "Expected failure but got success");
        var errors = context.PersonalInfoResult.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? [];
        Assert.Contains("Gender cannot exceed 64 characters.", allErrors);
    }

    [Then(@"the gender should be ""([^""]*)""")]
    public void ThenTheGenderShouldBe(string expectedGender)
    {
        Assert.Equal(expectedGender, context.PersonalInfoResult.Value.Gender);
    }

    [When("I attempt to create personal info without nationality")]
    public void WhenIAttemptToCreatePersonalInfoWithoutNationality()
    {
        context.PersonalInfoResult = PersonalInfo.Create(
            "John",
            "Smith",
            "Male",
            ValidBirthDate,
            null!,
            "Software Engineer",
            TimeProvider.System
        );
    }

    [When("I attempt to create personal info with whitespace-only nationality")]
    public void WhenIAttemptToCreatePersonalInfoWithWhitespaceOnlyNationality()
    {
        context.PersonalInfoResult = PersonalInfo.Create(
            "John",
            "Smith",
            "Male",
            ValidBirthDate,
            "   ",
            "Software Engineer",
            TimeProvider.System
        );
    }

    [When(@"I create personal info with nationality of (\d+) characters")]
    public void WhenICreatePersonalInfoWithNationalityOfCharacters(int length)
    {
        context.PersonalInfoResult = PersonalInfo.Create(
            "John",
            "Smith",
            "Male",
            ValidBirthDate,
            new string('A', length),
            "Software Engineer",
            TimeProvider.System
        );
    }

    [When(@"I attempt to create personal info with nationality of (\d+) characters")]
    public void WhenIAttemptToCreatePersonalInfoWithNationalityOfCharacters(int length)
    {
        context.PersonalInfoResult = PersonalInfo.Create(
            "John",
            "Smith",
            "Male",
            ValidBirthDate,
            new string('A', length),
            "Software Engineer",
            TimeProvider.System
        );
    }

    [When(@"I create personal info with nationality ""([^""]*)""")]
    public void WhenICreatePersonalInfoWithNationality(string nationality)
    {
        context.PersonalInfoResult = PersonalInfo.Create(
            "John",
            "Smith",
            "Male",
            ValidBirthDate,
            nationality,
            "Software Engineer",
            TimeProvider.System
        );
    }

    [Then("I should be informed that nationality is required")]
    public void ThenIShouldBeInformedThatNationalityIsRequired()
    {
        Assert.True(context.PersonalInfoResult.IsFailure, "Expected failure but got success");
        var errors = context.PersonalInfoResult.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? [];
        Assert.Contains("Nationality is required.", allErrors);
    }

    [Then("I should be informed that nationality cannot exceed 128 characters")]
    public void ThenIShouldBeInformedThatNationalityCannotExceed128Characters()
    {
        Assert.True(context.PersonalInfoResult.IsFailure, "Expected failure but got success");
        var errors = context.PersonalInfoResult.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? [];
        Assert.Contains("Nationality cannot exceed 128 characters.", allErrors);
    }

    [Then(@"the nationality should be ""([^""]*)""")]
    public void ThenTheNationalityShouldBe(string expectedNationality)
    {
        Assert.Equal(expectedNationality, context.PersonalInfoResult.Value.Nationality);
    }

    [When("I attempt to create personal info without occupation")]
    public void WhenIAttemptToCreatePersonalInfoWithoutOccupation()
    {
        context.PersonalInfoResult = PersonalInfo.Create(
            "John",
            "Smith",
            "Male",
            ValidBirthDate,
            "American",
            null!,
            TimeProvider.System
        );
    }

    [When("I attempt to create personal info with whitespace-only occupation")]
    public void WhenIAttemptToCreatePersonalInfoWithWhitespaceOnlyOccupation()
    {
        context.PersonalInfoResult = PersonalInfo.Create(
            "John",
            "Smith",
            "Male",
            ValidBirthDate,
            "American",
            "   ",
            TimeProvider.System
        );
    }

    [When(@"I create personal info with occupation of (\d+) characters")]
    public void WhenICreatePersonalInfoWithOccupationOfCharacters(int length)
    {
        context.PersonalInfoResult = PersonalInfo.Create(
            "John",
            "Smith",
            "Male",
            ValidBirthDate,
            "American",
            new string('A', length),
            TimeProvider.System
        );
    }

    [When(@"I attempt to create personal info with occupation of (\d+) characters")]
    public void WhenIAttemptToCreatePersonalInfoWithOccupationOfCharacters(int length)
    {
        context.PersonalInfoResult = PersonalInfo.Create(
            "John",
            "Smith",
            "Male",
            ValidBirthDate,
            "American",
            new string('A', length),
            TimeProvider.System
        );
    }

    [When(@"I create personal info with occupation ""([^""]*)""")]
    public void WhenICreatePersonalInfoWithOccupation(string occupation)
    {
        context.PersonalInfoResult = PersonalInfo.Create(
            "John",
            "Smith",
            "Male",
            ValidBirthDate,
            "American",
            occupation,
            TimeProvider.System
        );
    }

    [Then("I should be informed that occupation is required")]
    public void ThenIShouldBeInformedThatOccupationIsRequired()
    {
        Assert.True(context.PersonalInfoResult.IsFailure, "Expected failure but got success");
        var errors = context.PersonalInfoResult.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? [];
        Assert.Contains("Occupation is required.", allErrors);
    }

    [Then("I should be informed that occupation cannot exceed 128 characters")]
    public void ThenIShouldBeInformedThatOccupationCannotExceed128Characters()
    {
        Assert.True(context.PersonalInfoResult.IsFailure, "Expected failure but got success");
        var errors = context.PersonalInfoResult.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? [];
        Assert.Contains("Occupation cannot exceed 128 characters.", allErrors);
    }

    [Then(@"the occupation should be ""([^""]*)""")]
    public void ThenTheOccupationShouldBe(string expectedOccupation)
    {
        Assert.Equal(expectedOccupation, context.PersonalInfoResult.Value.Occupation);
    }
}
