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
        (context.PhysicalInfoResult).ShouldNotBeNull();
        (context.AccommodationPreferencesResult).ShouldNotBeNull();
        (context.MedicalInfoResult).ShouldNotBeNull();

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
        (context.Customer).ShouldNotBeNull();
    }

    [Then("the customer should contain all the provided information")]
    public void ThenTheCustomerShouldContainAllTheProvidedInformation()
    {
        (context.Customer).ShouldNotBeNull();
        (context.PhysicalInfoResult).ShouldNotBeNull();
        (context.AccommodationPreferencesResult).ShouldNotBeNull();
        (context.MedicalInfoResult).ShouldNotBeNull();

        (context.Customer.PersonalInfo).ShouldBe(context.PersonalInfoResult.Value);
        (context.Customer.IdentificationInfo).ShouldBe(context.IdentificationInfoResult.Value);
        (context.Customer.ContactInfo).ShouldBe(context.ContactInfoResult.Value);
        (context.Customer.Address).ShouldBe(context.AddressResult.Value);
        (context.Customer.PhysicalInfo).ShouldBe(context.PhysicalInfoResult.Value.Value);
        (context.Customer.AccommodationPreferences).ShouldBe(context.AccommodationPreferencesResult.Value.Value);
        (context.Customer.EmergencyContact).ShouldBe(context.EmergencyContactResult.Value);
        (context.Customer.MedicalInfo).ShouldBe(context.MedicalInfoResult.Value.Value);
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
    public void WhenICreatePersonalInformationFromSanitizationInputs()
    {
        (context.PersonalInfoResult.IsSuccess).ShouldBeTrue();
    }

    [Then("the personal information should be created successfully from sanitization")]
    public void ThenThePersonalInformationShouldBeCreatedSuccessfullyFromSanitization()
    {
        (context.PersonalInfoResult.IsSuccess).ShouldBeTrue();
        (context.PersonalInfoResult.Value).ShouldNotBeNull();
    }

    [Then(@"the sanitized first name should be ""(.*)""")]
    public void ThenTheSanitizedFirstNameShouldBe(string expectedFirstName)
    {
        (context.PersonalInfoResult.Value.FirstName).ShouldBe(expectedFirstName);
    }

    [Then(@"the sanitized last name should be ""(.*)""")]
    public void ThenTheSanitizedLastNameShouldBe(string expectedLastName)
    {
        (context.PersonalInfoResult.Value.LastName).ShouldBe(expectedLastName);
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
    public void WhenICreateAddressInformationFromSanitizationInputs()
    {
        (context.AddressResult.IsSuccess).ShouldBeTrue();
    }

    [Then(@"the sanitized address city should be ""(.*)""")]
    public void ThenTheSanitizedAddressCityShouldBe(string expectedCity)
    {
        (context.AddressResult.Value.City).ShouldBe(expectedCity);
    }

    [Then(@"the sanitized address country should be ""(.*)""")]
    public void ThenTheSanitizedAddressCountryShouldBe(string expectedCountry)
    {
        (context.AddressResult.Value.Country).ShouldBe(expectedCountry);
    }

    [Given(@"I have contact info with email ""(.*)"" and mobile ""(.*)""")]
    public void GivenIHaveContactInfoWithEmailAndMobile(string email, string mobile)
    {
        context.ContactInfoResult = ContactInfo.Create(email, mobile, null, null);
    }

    [When("I create contact information")]
    public void WhenICreateContactInformation()
    {
        (context.ContactInfoResult.IsSuccess).ShouldBeTrue();
    }

    [Then(@"the sanitized email should be ""(.*)""")]
    public void ThenTheSanitizedEmailShouldBe(string expectedEmail)
    {
        (context.ContactInfoResult.Value.Email).ShouldBe(expectedEmail);
    }

    [Then(@"the sanitized mobile should be ""(.*)""")]
    public void ThenTheSanitizedMobileShouldBe(string expectedMobile)
    {
        (context.ContactInfoResult.Value.Mobile).ShouldBe(expectedMobile);
    }

    [Given(@"I have contact info with Instagram ""(.*)"" and Facebook ""(.*)""")]
    public void GivenIHaveContactInfoWithInstagramAndFacebook(string instagram, string facebook)
    {
        context.ContactInfoResult = ContactInfo.Create("john@example.com", "+1234567890", instagram, facebook);
    }

    [When("I create contact information with social media")]
    public void WhenICreateContactInformationWithSocialMedia()
    {
        (context.ContactInfoResult.IsSuccess).ShouldBeTrue();
    }

    [Then(@"the sanitized Instagram should be ""(.*)""")]
    public void ThenTheSanitizedInstagramShouldBe(string expectedInstagram)
    {
        (context.ContactInfoResult.Value.Instagram).ShouldBe(expectedInstagram);
    }

    [Then(@"the sanitized Facebook should be ""(.*)""")]
    public void ThenTheSanitizedFacebookShouldBe(string expectedFacebook)
    {
        (context.ContactInfoResult.Value.Facebook).ShouldBe(expectedFacebook);
    }

    [Given(@"I have identification info with national ID ""(.*)"" and nationality ""(.*)""")]
    public void GivenIHaveIdentificationInfoWithNationalIdAndNationality(string nationalId, string nationality)
    {
        context.IdentificationInfoResult = IdentificationInfo.Create(nationalId, nationality);
    }

    [When("I create identification information")]
    public void WhenICreateIdentificationInformation()
    {
        (context.IdentificationInfoResult.IsSuccess).ShouldBeTrue();
    }

    [Then(@"the sanitized national ID should be ""(.*)""")]
    public void ThenTheSanitizedNationalIdShouldBe(string expectedNationalId)
    {
        (context.IdentificationInfoResult.Value.NationalId).ShouldBe(expectedNationalId);
    }

    [Then(@"the sanitized ID nationality should be ""(.*)""")]
    public void ThenTheSanitizedIdNationalityShouldBe(string expectedNationality)
    {
        (context.IdentificationInfoResult.Value.IdNationality).ShouldBe(expectedNationality);
    }

    [Given(@"I have emergency contact with name ""(.*)"" and mobile ""(.*)""")]
    public void GivenIHaveEmergencyContactWithNameAndMobile(string name, string mobile)
    {
        context.EmergencyContactResult = EmergencyContact.Create(name, mobile);
    }

    [When("I create emergency contact information")]
    public void WhenICreateEmergencyContactInformation()
    {
        (context.EmergencyContactResult.IsSuccess).ShouldBeTrue();
    }

    [Then(@"the sanitized emergency contact name should be ""(.*)""")]
    public void ThenTheSanitizedEmergencyContactNameShouldBe(string expectedName)
    {
        (context.EmergencyContactResult.Value.Name).ShouldBe(expectedName);
    }

    [Then(@"the sanitized emergency contact mobile should be ""(.*)""")]
    public void ThenTheSanitizedEmergencyContactMobileShouldBe(string expectedMobile)
    {
        (context.EmergencyContactResult.Value.Mobile).ShouldBe(expectedMobile);
    }

    [Given(@"I have medical info with allergies ""(.*)"" and additional info ""(.*)""")]
    public void GivenIHaveMedicalInfoWithAllergiesAndAdditionalInfo(string allergies, string additionalInfo)
    {
        context.MedicalInfoResult = MedicalInfo.Create(allergies, additionalInfo);
    }

    [When("I create medical information")]
    public void WhenICreateMedicalInformation()
    {
        (context.MedicalInfoResult).ShouldNotBeNull();
        (context.MedicalInfoResult.Value.IsSuccess).ShouldBeTrue();
    }

    [Then(@"the sanitized allergies should be ""(.*)""")]
    public void ThenTheSanitizedAllergiesShouldBe(string expectedAllergies)
    {
        (context.MedicalInfoResult).ShouldNotBeNull();
        (context.MedicalInfoResult.Value.Value.Allergies).ShouldBe(expectedAllergies);
    }

    [Then(@"the sanitized additional info should be ""(.*)""")]
    public void ThenTheSanitizedAdditionalInfoShouldBe(string expectedAdditionalInfo)
    {
        (context.MedicalInfoResult).ShouldNotBeNull();
        (context.MedicalInfoResult.Value.Value.AdditionalInfo).ShouldBe(expectedAdditionalInfo);
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
        (context.CommandResult).ShouldNotBeNull();
        (context.CommandResult.Value.IsFailure).ShouldBeTrue();
    }
}
