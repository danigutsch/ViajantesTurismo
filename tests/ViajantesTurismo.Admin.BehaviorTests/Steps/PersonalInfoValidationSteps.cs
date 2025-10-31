using Reqnroll;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
public sealed class PersonalInfoValidationSteps
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
        if (_result is null || _result.Value.IsFailure)
        {
            var error = _result.HasValue ? _result.Value.ErrorDetails?.Detail : "Result was null";
            throw new InvalidOperationException($"Expected success but got: {error}");
        }
    }

    [Then(@"the personal info should contain the provided data")]
    public void ThenThePersonalInfoShouldContainTheProvidedData()
    {
        if (_result is null || _result.Value.IsFailure)
        {
            throw new InvalidOperationException("Result was null or failed");
        }

        var info = _result.Value.Value;
        if (info.FirstName != _firstName ||
            info.LastName != _lastName ||
            info.Gender != _gender ||
            info.Nationality != _nationality ||
            info.Profession != _profession)
        {
            throw new InvalidOperationException("Personal info does not match expected values");
        }
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

    [Then(@"the creation should fail")]
    public void ThenTheCreationShouldFail()
    {
        if (_result is null || _result.Value.IsSuccess)
        {
            throw new InvalidOperationException("Expected failure but got success");
        }
    }

    [Then(@"the error should be ""(.*)""")]
    public void ThenTheErrorShouldBe(string expectedError)
    {
        if (_result is null || _result.Value.IsSuccess)
        {
            throw new InvalidOperationException("Expected failure but got success");
        }

        var errors = _result.Value.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? new List<string>();

        if (allErrors.All(e => e != expectedError))
        {
            throw new InvalidOperationException($"Expected error '{expectedError}' but got: {string.Join(", ", allErrors)}");
        }
    }
}
