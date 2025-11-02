using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Admin.Domain.Customers;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
public sealed class CustomerManagementSteps(CustomerContext context)
{
    [Given(@"I have valid identification information")]
    public void GivenIHaveValidIdentificationInformation()
    {
        context.IdentificationInfo = IdentificationInfo.Create("123456789", "American").Value;
    }

    [Given(@"I have valid contact information")]
    public void GivenIHaveValidContactInformation()
    {
        context.ContactInfo = ContactInfo.Create("john.smith@example.com", "+1234567890", "@johnsmith", "john.smith").Value;
    }

    [Given(@"I have valid address information")]
    public void GivenIHaveValidAddressInformation()
    {
        context.Address = new Address(
            "123 Main Street",
            "Apt 4B",
            "Downtown",
            "12345",
            "New York",
            "NY",
            "USA");
    }

    [Given(@"I have valid physical information")]
    public void GivenIHaveValidPhysicalInformation()
    {
        context.PhysicalInfo = new PhysicalInfo(75.5m, 180, BikeType.Regular);
    }

    [Given(@"I have valid accommodation preferences")]
    public void GivenIHaveValidAccommodationPreferences()
    {
        context.AccommodationPreferences = new AccommodationPreferences(RoomType.DoubleRoom, BedType.DoubleBed, null);
    }

    [Given(@"I have valid emergency contact")]
    public void GivenIHaveValidEmergencyContact()
    {
        context.EmergencyContact = new EmergencyContact("Jane Smith", "+1987654321");
    }

    [Given(@"I have valid medical information")]
    public void GivenIHaveValidMedicalInformation()
    {
        context.MedicalInfo = new MedicalInfo("Peanuts", "None");
    }

    [Given(@"I have an existing customer")]
    public void GivenIHaveAnExistingCustomer()
    {
        // Reuse the helper to create personal info
        context.PersonalInfo = PersonalInfo.Create(
            "John",
            "Smith",
            "Male",
            new DateTime(1990, 5, 15),
            "American",
            "Software Engineer",
            TimeProvider.System).Value;

        context.IdentificationInfo = IdentificationInfo.Create("123456789", "American").Value;
        context.ContactInfo = ContactInfo.Create("john.smith@example.com", "+1234567890", null, null).Value;
        context.Address = new Address("123 Main St", null, "Downtown", "12345", "New York", "NY", "USA");
        context.PhysicalInfo = new PhysicalInfo(75m, 180, BikeType.Regular);
        context.AccommodationPreferences = new AccommodationPreferences(RoomType.SingleRoom, BedType.SingleBed, null);
        context.EmergencyContact = new EmergencyContact("Emergency Contact", "+1111111111");
        context.MedicalInfo = new MedicalInfo(null, null);

        context.Customer = new Customer(
            context.PersonalInfo,
            context.IdentificationInfo,
            context.ContactInfo,
            context.Address,
            context.PhysicalInfo,
            context.AccommodationPreferences,
            context.EmergencyContact,
            context.MedicalInfo);
    }

    [When(@"I create a customer")]
    public void WhenICreateACustomer()
    {
        // Personal info must be created through its factory method with validation
        context.PersonalInfo = PersonalInfo.Create(
            "John",
            "Smith",
            "Male",
            new DateTime(1990, 5, 15),
            "American",
            "Software Engineer",
            TimeProvider.System).Value;

        context.Customer = new Customer(
            context.PersonalInfo,
            context.IdentificationInfo,
            context.ContactInfo,
            context.Address,
            context.PhysicalInfo,
            context.AccommodationPreferences,
            context.EmergencyContact,
            context.MedicalInfo);
    }

    [When(@"I update the customer with new personal information")]
    public void WhenIUpdateTheCustomerWithNewPersonalInformation()
    {
        var newPersonalInfo = PersonalInfo.Create(
            "Jane",
            "Doe",
            "Female",
            new DateTime(1985, 3, 20),
            "Canadian",
            "Designer",
            TimeProvider.System).Value;

        context.Customer.UpdatePersonalInfo(newPersonalInfo);
        context.PersonalInfo = newPersonalInfo;
    }

    [When(@"I update the customer with new identification information")]
    public void WhenIUpdateTheCustomerWithNewIdentificationInformation()
    {
        var newIdentificationInfo = IdentificationInfo.Create("987654321", "Canadian").Value;
        context.Customer.UpdateIdentificationInfo(newIdentificationInfo);
        context.IdentificationInfo = newIdentificationInfo;
    }

    [When(@"I update the customer with new contact information")]
    public void WhenIUpdateTheCustomerWithNewContactInformation()
    {
        var newContactInfo = ContactInfo.Create("jane.doe@example.com", "+9876543210", "@janedoe", null).Value;
        context.Customer.UpdateContactInfo(newContactInfo);
        context.ContactInfo = newContactInfo;
    }

    [When(@"I update the customer with new address")]
    public void WhenIUpdateTheCustomerWithNewAddress()
    {
        var newAddress = new Address("456 Oak Avenue", "Suite 100", "Uptown", "54321", "Toronto", "ON", "Canada");
        context.Customer.UpdateAddress(newAddress);
        context.Address = newAddress;
    }

    [When(@"I update the customer with new physical information")]
    public void WhenIUpdateTheCustomerWithNewPhysicalInformation()
    {
        var newPhysicalInfo = new PhysicalInfo(68m, 165, BikeType.EBike);
        context.Customer.UpdatePhysicalInfo(newPhysicalInfo);
        context.PhysicalInfo = newPhysicalInfo;
    }

    [When(@"I update the customer with new accommodation preferences")]
    public void WhenIUpdateTheCustomerWithNewAccommodationPreferences()
    {
        var newAccommodationPreferences = new AccommodationPreferences(RoomType.DoubleRoom, BedType.DoubleBed, 5);
        context.Customer.UpdateAccommodationPreferences(newAccommodationPreferences);
        context.AccommodationPreferences = newAccommodationPreferences;
    }

    [When(@"I update the customer with new emergency contact")]
    public void WhenIUpdateTheCustomerWithNewEmergencyContact()
    {
        var newEmergencyContact = new EmergencyContact("Bob Doe", "+5555555555");
        context.Customer.UpdateEmergencyContact(newEmergencyContact);
        context.EmergencyContact = newEmergencyContact;
    }

    [When(@"I update the customer with new medical information")]
    public void WhenIUpdateTheCustomerWithNewMedicalInformation()
    {
        var newMedicalInfo = new MedicalInfo("Lactose", "Requires medication");
        context.Customer.UpdateMedicalInfo(newMedicalInfo);
        context.MedicalInfo = newMedicalInfo;
    }

    [Then(@"the customer should be created successfully")]
    public void ThenTheCustomerShouldBeCreatedSuccessfully()
    {
        Assert.NotNull(context.Customer);
    }

    [Then(@"the customer should contain all the provided information")]
    public void ThenTheCustomerShouldContainAllTheProvidedInformation()
    {
        Assert.NotNull(context.Customer);
        Assert.Equal(context.PersonalInfo, context.Customer.PersonalInfo);
        Assert.Equal(context.IdentificationInfo, context.Customer.IdentificationInfo);
        Assert.Equal(context.ContactInfo, context.Customer.ContactInfo);
        Assert.Equal(context.Address, context.Customer.Address);
        Assert.Equal(context.PhysicalInfo, context.Customer.PhysicalInfo);
        Assert.Equal(context.AccommodationPreferences, context.Customer.AccommodationPreferences);
        Assert.Equal(context.EmergencyContact, context.Customer.EmergencyContact);
        Assert.Equal(context.MedicalInfo, context.Customer.MedicalInfo);
    }

    [Then(@"the customer personal information should be updated")]
    public void ThenTheCustomerPersonalInformationShouldBeUpdated()
    {
        Assert.NotNull(context.Customer);
        Assert.Equal(context.PersonalInfo, context.Customer.PersonalInfo);
    }

    [Then(@"the customer identification information should be updated")]
    public void ThenTheCustomerIdentificationInformationShouldBeUpdated()
    {
        Assert.NotNull(context.Customer);
        Assert.Equal(context.IdentificationInfo, context.Customer.IdentificationInfo);
    }

    [Then(@"the customer contact information should be updated")]
    public void ThenTheCustomerContactInformationShouldBeUpdated()
    {
        Assert.NotNull(context.Customer);
        Assert.Equal(context.ContactInfo, context.Customer.ContactInfo);
    }

    [Then(@"the customer address should be updated")]
    public void ThenTheCustomerAddressShouldBeUpdated()
    {
        Assert.NotNull(context.Customer);
        Assert.Equal(context.Address, context.Customer.Address);
    }

    [Then(@"the customer physical information should be updated")]
    public void ThenTheCustomerPhysicalInformationShouldBeUpdated()
    {
        Assert.NotNull(context.Customer);
        Assert.Equal(context.PhysicalInfo, context.Customer.PhysicalInfo);
    }

    [Then(@"the customer accommodation preferences should be updated")]
    public void ThenTheCustomerAccommodationPreferencesShouldBeUpdated()
    {
        Assert.NotNull(context.Customer);
        Assert.Equal(context.AccommodationPreferences, context.Customer.AccommodationPreferences);
    }

    [Then(@"the customer emergency contact should be updated")]
    public void ThenTheCustomerEmergencyContactShouldBeUpdated()
    {
        Assert.NotNull(context.Customer);
        Assert.Equal(context.EmergencyContact, context.Customer.EmergencyContact);
    }

    [Then(@"the customer medical information should be updated")]
    public void ThenTheCustomerMedicalInformationShouldBeUpdated()
    {
        Assert.NotNull(context.Customer);
        Assert.Equal(context.MedicalInfo, context.Customer.MedicalInfo);
    }

    [Given(@"I have personal information for sanitization with first name ""([^""]*)"" and last name ""([^""]*)""")]
    public void GivenIHavePersonalInformationForSanitizationWithFirstNameAndLastName(string firstName, string lastName)
    {
        context.PersonalInfoResult = PersonalInfo.Create(
            firstName,
            lastName,
            "Male",
            new DateTime(1990, 1, 1),
            "American",
            "Engineer",
            TimeProvider.System);
    }

    [When(@"I create personal information from sanitization inputs")]
    public void WhenICreatePersonalInformationFromSanitizationInputs()
    {
        if (context.PersonalInfoResult.IsSuccess)
        {
            context.PersonalInfo = context.PersonalInfoResult.Value;
        }
    }

    [Then(@"the personal information should be created successfully from sanitization")]
    public void ThenThePersonalInformationShouldBeCreatedSuccessfullyFromSanitization()
    {
        Assert.True(context.PersonalInfoResult.IsSuccess);
        Assert.NotNull(context.PersonalInfo);
    }

    [Then(@"the sanitized first name should be ""(.*)""")]
    public void ThenTheSanitizedFirstNameShouldBe(string expectedFirstName)
    {
        Assert.Equal(expectedFirstName, context.PersonalInfo.FirstName);
    }

    [Then(@"the sanitized last name should be ""(.*)""")]
    public void ThenTheSanitizedLastNameShouldBe(string expectedLastName)
    {
        Assert.Equal(expectedLastName, context.PersonalInfo.LastName);
    }

    [Given(@"I have address for sanitization with city ""(.*)"" and country ""(.*)""")]
    public void GivenIHaveAddressForSanitizationWithCityAndCountry(string city, string country)
    {
        context.Address = new Address(
            "123 Main St",
            null,
            "Downtown",
            "12345",
            city,
            "State",
            country);
    }

    [When(@"I create address information from sanitization inputs")]
#pragma warning disable CA1822
    public void WhenICreateAddressInformationFromSanitizationInputs()
#pragma warning restore CA1822
    {
        // Address is already created in the Given step
    }

    [Then(@"the sanitized address city should be ""(.*)""")]
    public void ThenTheSanitizedAddressCityShouldBe(string expectedCity)
    {
        Assert.Equal(expectedCity, context.Address.City);
    }

    [Then(@"the sanitized address country should be ""(.*)""")]
    public void ThenTheSanitizedAddressCountryShouldBe(string expectedCountry)
    {
        Assert.Equal(expectedCountry, context.Address.Country);
    }

    [Given(@"I have contact info with email ""(.*)"" and mobile ""(.*)""")]
    public void GivenIHaveContactInfoWithEmailAndMobile(string email, string mobile)
    {
        context.ContactInfo = ContactInfo.Create(email, mobile, null, null).Value;
    }

    [When(@"I create contact information")]
#pragma warning disable CA1822
    public void WhenICreateContactInformation()
#pragma warning restore CA1822
    {
        // ContactInfo is already created in the Given step
    }

    [Then(@"the sanitized email should be ""(.*)""")]
    public void ThenTheSanitizedEmailShouldBe(string expectedEmail)
    {
        Assert.Equal(expectedEmail, context.ContactInfo.Email);
    }

    [Then(@"the sanitized mobile should be ""(.*)""")]
    public void ThenTheSanitizedMobileShouldBe(string expectedMobile)
    {
        Assert.Equal(expectedMobile, context.ContactInfo.Mobile);
    }

    [Given(@"I have contact info with Instagram ""(.*)"" and Facebook ""(.*)""")]
    public void GivenIHaveContactInfoWithInstagramAndFacebook(string instagram, string facebook)
    {
        context.ContactInfo = ContactInfo.Create("john@example.com", "+1234567890", instagram, facebook).Value;
    }

    [When(@"I create contact information with social media")]
#pragma warning disable CA1822
    public void WhenICreateContactInformationWithSocialMedia()
#pragma warning restore CA1822
    {
        // ContactInfo is already created in the Given step
    }

    [Then(@"the sanitized Instagram should be ""(.*)""")]
    public void ThenTheSanitizedInstagramShouldBe(string expectedInstagram)
    {
        Assert.Equal(expectedInstagram, context.ContactInfo.Instagram);
    }

    [Then(@"the sanitized Facebook should be ""(.*)""")]
    public void ThenTheSanitizedFacebookShouldBe(string expectedFacebook)
    {
        Assert.Equal(expectedFacebook, context.ContactInfo.Facebook);
    }

    [Given(@"I have identification info with national ID ""(.*)"" and nationality ""(.*)""")]
    public void GivenIHaveIdentificationInfoWithNationalIDAndNationality(string nationalId, string nationality)
    {
        context.IdentificationInfo = IdentificationInfo.Create(nationalId, nationality).Value;
    }

    [When(@"I create identification information")]
#pragma warning disable CA1822
    public void WhenICreateIdentificationInformation()
#pragma warning restore CA1822
    {
        // IdentificationInfo is already created in the Given step
    }

    [Then(@"the sanitized national ID should be ""(.*)""")]
    public void ThenTheSanitizedNationalIDShouldBe(string expectedNationalId)
    {
        Assert.Equal(expectedNationalId, context.IdentificationInfo.NationalId);
    }

    [Then(@"the sanitized ID nationality should be ""(.*)""")]
    public void ThenTheSanitizedIDNationalityShouldBe(string expectedNationality)
    {
        Assert.Equal(expectedNationality, context.IdentificationInfo.IdNationality);
    }

    [Given(@"I have emergency contact with name ""(.*)"" and mobile ""(.*)""")]
    public void GivenIHaveEmergencyContactWithNameAndMobile(string name, string mobile)
    {
        context.EmergencyContact = new EmergencyContact(name, mobile);
    }

    [When(@"I create emergency contact information")]
#pragma warning disable CA1822
    public void WhenICreateEmergencyContactInformation()
#pragma warning restore CA1822
    {
        // EmergencyContact is already created in the Given step
    }

    [Then(@"the sanitized emergency contact name should be ""(.*)""")]
    public void ThenTheSanitizedEmergencyContactNameShouldBe(string expectedName)
    {
        Assert.Equal(expectedName, context.EmergencyContact.Name);
    }

    [Then(@"the sanitized emergency contact mobile should be ""(.*)""")]
    public void ThenTheSanitizedEmergencyContactMobileShouldBe(string expectedMobile)
    {
        Assert.Equal(expectedMobile, context.EmergencyContact.Mobile);
    }

    [Given(@"I have medical info with allergies ""(.*)"" and additional info ""(.*)""")]
    public void GivenIHaveMedicalInfoWithAllergiesAndAdditionalInfo(string allergies, string additionalInfo)
    {
        context.MedicalInfo = new MedicalInfo(allergies, additionalInfo);
    }

    [When(@"I create medical information")]
#pragma warning disable CA1822
    public void WhenICreateMedicalInformation()
#pragma warning restore CA1822
    {
        // MedicalInfo is already created in the Given step
    }

    [Then(@"the sanitized allergies should be ""(.*)""")]
    public void ThenTheSanitizedAllergiesShouldBe(string expectedAllergies)
    {
        Assert.Equal(expectedAllergies, context.MedicalInfo.Allergies);
    }

    [Then(@"the sanitized additional info should be ""(.*)""")]
    public void ThenTheSanitizedAdditionalInfoShouldBe(string expectedAdditionalInfo)
    {
        Assert.Equal(expectedAdditionalInfo, context.MedicalInfo.AdditionalInfo);
    }
}
