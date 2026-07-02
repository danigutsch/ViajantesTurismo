using ViajantesTurismo.Admin.Application.Customers.CreateCustomer;
using ViajantesTurismo.Admin.Contracts;

using ViajantesTurismo.Admin.Domain.Shared;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps.Customers;

[Binding]
public sealed class CustomerManagementSteps(CustomerContext context)
{
    private static readonly DateTime ValidBirthDate = new(1990, 5, 15, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime SanitizationBirthDate = new(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Given("I have valid identification information")]
    public void GivenIHaveValidIdentificationInformation()
    {
        context.IdentificationInfoResult = IdentificationInfo.Create("123456789", "American");
    }

    [Given("I have valid contact information")]
    public void GivenIHaveValidContactInformation()
    {
        context.ContactInfoResult =
            ContactInfo.Create("john.smith@example.com", "+1234567890", "@johnsmith", "john.smith");
    }

    [Given("I have valid address information")]
    public void GivenIHaveValidAddressInformation()
    {
        context.AddressResult = Address.Create(
            "123 Main Street",
            "Apt 4B",
            "Downtown",
            "12345",
            "New York",
            "NY",
            "USA");
    }

    [Given("I have valid physical information")]
    public void GivenIHaveValidPhysicalInformation()
    {
        context.PhysicalInfoResult = PhysicalInfo.Create(75.5m, 180, BikeType.Regular);
    }

    [Given("I have valid accommodation preferences")]
    public void GivenIHaveValidAccommodationPreferences()
    {
        context.AccommodationPreferencesResult =
            AccommodationPreferences.Create(RoomType.DoubleOccupancy, BedType.DoubleBed, Guid.CreateVersion7());
    }

    [Given("I have valid emergency contact")]
    public void GivenIHaveValidEmergencyContact()
    {
        context.EmergencyContactResult = EmergencyContact.Create("Jane Smith", "+1987654321");
    }

    [Given("I have valid medical information")]
    public void GivenIHaveValidMedicalInformation()
    {
        context.MedicalInfoResult = MedicalInfo.Create("Peanuts", "None");
    }

    [When("I create a customer")]
    public void WhenICreateACustomer()
    {
        TestAssert.NotNull(context.PhysicalInfoResult);
        TestAssert.NotNull(context.AccommodationPreferencesResult);
        TestAssert.NotNull(context.MedicalInfoResult);

        context.PersonalInfoResult = PersonalInfo.Create(
            "John",
            "Smith",
            "Male",
            ValidBirthDate,
            "American",
            "Software Engineer",
            TimeProvider.System);

        context.Customer = new Customer(
            context.PersonalInfoResult.Value,
            context.IdentificationInfoResult.Value,
            context.ContactInfoResult.Value,
            context.AddressResult.Value,
            context.PhysicalInfoResult.Value.Value,
            context.AccommodationPreferencesResult.Value.Value,
            new CustomerHealthInfo(context.EmergencyContactResult.Value, context.MedicalInfoResult.Value.Value));
    }

    [Then("the customer should be created successfully")]
    public void ThenTheCustomerShouldBeCreatedSuccessfully()
    {
        TestAssert.NotNull(context.Customer);
    }

    [Then("the customer should contain all the provided information")]
    public void ThenTheCustomerShouldContainAllTheProvidedInformation()
    {
        TestAssert.NotNull(context.Customer);
        TestAssert.NotNull(context.PhysicalInfoResult);
        TestAssert.NotNull(context.AccommodationPreferencesResult);
        TestAssert.NotNull(context.MedicalInfoResult);

        TestAssert.Equal(context.PersonalInfoResult.Value, context.Customer.PersonalInfo);
        TestAssert.Equal(context.IdentificationInfoResult.Value, context.Customer.IdentificationInfo);
        TestAssert.Equal(context.ContactInfoResult.Value, context.Customer.ContactInfo);
        TestAssert.Equal(context.AddressResult.Value, context.Customer.Address);
        TestAssert.Equal(context.PhysicalInfoResult.Value.Value, context.Customer.PhysicalInfo);
        TestAssert.Equal(context.AccommodationPreferencesResult.Value.Value, context.Customer.AccommodationPreferences);
        TestAssert.Equal(context.EmergencyContactResult.Value, context.Customer.EmergencyContact);
        TestAssert.Equal(context.MedicalInfoResult.Value.Value, context.Customer.MedicalInfo);
    }

    [Given(@"I have personal information for sanitization with first name ""([^""]*)"" and last name ""([^""]*)""")]
    public void GivenIHavePersonalInformationForSanitizationWithFirstName(string firstName, string lastName)
    {
        context.PersonalInfoResult = PersonalInfo.Create(
            firstName,
            lastName,
            "Male",
            SanitizationBirthDate,
            "American",
            "Engineer",
            TimeProvider.System);
    }

    [When("I create personal information from sanitization inputs")]
    public static void WhenICreatePersonalInformationFromSanitizationInputs()
    {
        // Result already stored in context by Given step
    }

    [Then("the personal information should be created successfully from sanitization")]
    public void ThenThePersonalInformationShouldBeCreatedSuccessfullyFromSanitization()
    {
        TestAssert.True(context.PersonalInfoResult.IsSuccess);
        TestAssert.NotNull(context.PersonalInfoResult.Value);
    }

    [Then(@"the sanitized first name should be ""(.*)""")]
    public void ThenTheSanitizedFirstNameShouldBe(string expectedFirstName)
    {
        TestAssert.Equal(expectedFirstName, context.PersonalInfoResult.Value.FirstName);
    }

    [Then(@"the sanitized last name should be ""(.*)""")]
    public void ThenTheSanitizedLastNameShouldBe(string expectedLastName)
    {
        TestAssert.Equal(expectedLastName, context.PersonalInfoResult.Value.LastName);
    }

    [Given(@"I have address for sanitization with city ""(.*)"" and country ""(.*)""")]
    public void GivenIHaveAddressForSanitizationWithCityAndCountry(string city, string country)
    {
        context.AddressResult = Address.Create(
            "123 Main St",
            null,
            "Downtown",
            "12345",
            city,
            "State",
            country);
    }

    [When("I create address information from sanitization inputs")]
    public static void WhenICreateAddressInformationFromSanitizationInputs()
    {
        // Result already stored in context by Given step.
    }

    [Then(@"the sanitized address city should be ""(.*)""")]
    public void ThenTheSanitizedAddressCityShouldBe(string expectedCity)
    {
        TestAssert.Equal(expectedCity, context.AddressResult.Value.City);
    }

    [Then(@"the sanitized address country should be ""(.*)""")]
    public void ThenTheSanitizedAddressCountryShouldBe(string expectedCountry)
    {
        TestAssert.Equal(expectedCountry, context.AddressResult.Value.Country);
    }

    [Given(@"I have contact info with email ""(.*)"" and mobile ""(.*)""")]
    public void GivenIHaveContactInfoWithEmailAndMobile(string email, string mobile)
    {
        context.ContactInfoResult = ContactInfo.Create(email, mobile, null, null);
    }

    [When("I create contact information")]
    public static void WhenICreateContactInformation()
    {
        // Result already stored in context by Given step.
    }

    [Then(@"the sanitized email should be ""(.*)""")]
    public void ThenTheSanitizedEmailShouldBe(string expectedEmail)
    {
        TestAssert.Equal(expectedEmail, context.ContactInfoResult.Value.Email);
    }

    [Then(@"the sanitized mobile should be ""(.*)""")]
    public void ThenTheSanitizedMobileShouldBe(string expectedMobile)
    {
        TestAssert.Equal(expectedMobile, context.ContactInfoResult.Value.Mobile);
    }

    [Given(@"I have contact info with Instagram ""(.*)"" and Facebook ""(.*)""")]
    public void GivenIHaveContactInfoWithInstagramAndFacebook(string instagram, string facebook)
    {
        context.ContactInfoResult = ContactInfo.Create("john@example.com", "+1234567890", instagram, facebook);
    }

    [When("I create contact information with social media")]
    public static void WhenICreateContactInformationWithSocialMedia()
    {
        // Result already stored in context by Given step.
    }

    [Then(@"the sanitized Instagram should be ""(.*)""")]
    public void ThenTheSanitizedInstagramShouldBe(string expectedInstagram)
    {
        TestAssert.Equal(expectedInstagram, context.ContactInfoResult.Value.Instagram);
    }

    [Then(@"the sanitized Facebook should be ""(.*)""")]
    public void ThenTheSanitizedFacebookShouldBe(string expectedFacebook)
    {
        TestAssert.Equal(expectedFacebook, context.ContactInfoResult.Value.Facebook);
    }

    [Given(@"I have identification info with national ID ""(.*)"" and nationality ""(.*)""")]
    public void GivenIHaveIdentificationInfoWithNationalIdAndNationality(string nationalId, string nationality)
    {
        context.IdentificationInfoResult = IdentificationInfo.Create(nationalId, nationality);
    }

    [When("I create identification information")]
    public static void WhenICreateIdentificationInformation()
    {
        // Result already stored in context by Given step.
    }

    [Then(@"the sanitized national ID should be ""(.*)""")]
    public void ThenTheSanitizedNationalIdShouldBe(string expectedNationalId)
    {
        TestAssert.Equal(expectedNationalId, context.IdentificationInfoResult.Value.NationalId);
    }

    [Then(@"the sanitized ID nationality should be ""(.*)""")]
    public void ThenTheSanitizedIdNationalityShouldBe(string expectedNationality)
    {
        TestAssert.Equal(expectedNationality, context.IdentificationInfoResult.Value.IdNationality);
    }

    [Given(@"I have emergency contact with name ""(.*)"" and mobile ""(.*)""")]
    public void GivenIHaveEmergencyContactWithNameAndMobile(string name, string mobile)
    {
        context.EmergencyContactResult = EmergencyContact.Create(name, mobile);
    }

    [When("I create emergency contact information")]
    public static void WhenICreateEmergencyContactInformation()
    {
        // Result already stored in context by Given step.
    }

    [Then(@"the sanitized emergency contact name should be ""(.*)""")]
    public void ThenTheSanitizedEmergencyContactNameShouldBe(string expectedName)
    {
        TestAssert.Equal(expectedName, context.EmergencyContactResult.Value.Name);
    }

    [Then(@"the sanitized emergency contact mobile should be ""(.*)""")]
    public void ThenTheSanitizedEmergencyContactMobileShouldBe(string expectedMobile)
    {
        TestAssert.Equal(expectedMobile, context.EmergencyContactResult.Value.Mobile);
    }

    [Given(@"I have medical info with allergies ""(.*)"" and additional info ""(.*)""")]
    public void GivenIHaveMedicalInfoWithAllergiesAndAdditionalInfo(string allergies, string additionalInfo)
    {
        context.MedicalInfoResult = MedicalInfo.Create(allergies, additionalInfo);
    }

    [When("I create medical information")]
    public static void WhenICreateMedicalInformation()
    {
        // Result already stored in context by Given step.
    }

    [Then(@"the sanitized allergies should be ""(.*)""")]
    public void ThenTheSanitizedAllergiesShouldBe(string expectedAllergies)
    {
        TestAssert.NotNull(context.MedicalInfoResult);
        TestAssert.Equal(expectedAllergies, context.MedicalInfoResult.Value.Value.Allergies);
    }

    [Then(@"the sanitized additional info should be ""(.*)""")]
    public void ThenTheSanitizedAdditionalInfoShouldBe(string expectedAdditionalInfo)
    {
        TestAssert.NotNull(context.MedicalInfoResult);
        TestAssert.Equal(expectedAdditionalInfo, context.MedicalInfoResult.Value.Value.AdditionalInfo);
    }

    [When(@"I attempt to create another customer with email ""(.*)""")]
    public async Task WhenIAttemptToCreateAnotherCustomerWithEmail(string email)
    {
        await CreateCustomerCommandForEmail(email);
    }

    [When(@"I create a customer with email ""(.*)""")]
    public async Task WhenICreateACustomerWithEmail(string email)
    {
        await CreateCustomerCommandForEmail(email);
    }

    private async Task CreateCustomerCommandForEmail(string email)
    {
        var command = new CreateCustomerCommand(
            PersonalInfo: new PersonalInfoDto
            {
                FirstName = "Jane",
                LastName = "Doe",
                Gender = "Female",
                BirthDate = DateTime.UtcNow.AddYears(-30),
                Nationality = "American",
                Occupation = "Designer"
            },
            IdentificationInfo: new IdentificationInfoDto
            {
                NationalId = "987654321",
                IdNationality = "American"
            },
            ContactInfo: new ContactInfoDto
            {
                Email = email,
                Mobile = "+1987654321",
                Instagram = null,
                Facebook = null
            },
            Address: new AddressDto
            {
                Street = "456 Oak St",
                Complement = null,
                Neighborhood = "Uptown",
                PostalCode = "54321",
                City = "City",
                State = "State",
                Country = "Country"
            },
            PhysicalInfo: new PhysicalInfoDto
            {
                WeightKg = 60m,
                HeightCentimeters = 165,
                BikeType = BikeTypeDto.Regular
            },
            AccommodationPreferences: new AccommodationPreferencesDto
            {
                RoomType = RoomTypeDto.DoubleOccupancy,
                BedType = BedTypeDto.SingleBed,
                CompanionId = null
            },
            EmergencyContact: new EmergencyContactDto
            {
                Name = "John Doe",
                Mobile = "+1234567890"
            },
            MedicalInfo: new MedicalInfoDto
            {
                Allergies = null,
                AdditionalInfo = null
            });

        context.CommandResult = await context.CommandHandler.Handle(command, CancellationToken.None);
    }

    [Then("the customer creation should fail")]
    public void ThenTheCustomerCreationShouldFail()
    {
        TestAssert.NotNull(context.CommandResult);
        TestAssert.True(context.CommandResult.Value.IsFailure);
    }
}
