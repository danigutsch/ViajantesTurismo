using Reqnroll;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
public sealed class CustomerManagementSteps
{
    private PersonalInfo _personalInfo = null!;
    private IdentificationInfo _identificationInfo = null!;
    private ContactInfo _contactInfo = null!;
    private Address _address = null!;
    private PhysicalInfo _physicalInfo = null!;
    private AccommodationPreferences _accommodationPreferences = null!;
    private EmergencyContact _emergencyContact = null!;
    private MedicalInfo _medicalInfo = null!;
    private Customer? _customer;
    private Result<PersonalInfo>? _personalInfoResult;

    [Given(@"I have valid identification information")]
    public void GivenIHaveValidIdentificationInformation()
    {
        _identificationInfo = new IdentificationInfo("123456789", "American");
    }

    [Given(@"I have valid contact information")]
    public void GivenIHaveValidContactInformation()
    {
        _contactInfo = new ContactInfo("john.smith@example.com", "+1234567890", "@johnsmith", "john.smith");
    }

    [Given(@"I have valid address information")]
    public void GivenIHaveValidAddressInformation()
    {
        _address = new Address(
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
        _physicalInfo = new PhysicalInfo(75.5m, 180, BikeType.Regular);
    }

    [Given(@"I have valid accommodation preferences")]
    public void GivenIHaveValidAccommodationPreferences()
    {
        _accommodationPreferences = new AccommodationPreferences(RoomType.DoubleRoom, BedType.DoubleBed, null);
    }

    [Given(@"I have valid emergency contact")]
    public void GivenIHaveValidEmergencyContact()
    {
        _emergencyContact = new EmergencyContact("Jane Smith", "+1987654321");
    }

    [Given(@"I have valid medical information")]
    public void GivenIHaveValidMedicalInformation()
    {
        _medicalInfo = new MedicalInfo("Peanuts", "None");
    }

    [Given(@"I have an existing customer")]
    public void GivenIHaveAnExistingCustomer()
    {
        // Reuse the helper to create personal info
        _personalInfo = PersonalInfo.Create(
            "John",
            "Smith",
            "Male",
            new DateTime(1990, 5, 15),
            "American",
            "Software Engineer",
            TimeProvider.System).Value;

        _identificationInfo = new IdentificationInfo("123456789", "American");
        _contactInfo = new ContactInfo("john.smith@example.com", "+1234567890", null, null);
        _address = new Address("123 Main St", null, "Downtown", "12345", "New York", "NY", "USA");
        _physicalInfo = new PhysicalInfo(75m, 180, BikeType.Regular);
        _accommodationPreferences = new AccommodationPreferences(RoomType.SingleRoom, BedType.SingleBed, null);
        _emergencyContact = new EmergencyContact("Emergency Contact", "+1111111111");
        _medicalInfo = new MedicalInfo(null, null);

        _customer = new Customer(
            _personalInfo,
            _identificationInfo,
            _contactInfo,
            _address,
            _physicalInfo,
            _accommodationPreferences,
            _emergencyContact,
            _medicalInfo);
    }

    [When(@"I create a customer")]
    public void WhenICreateACustomer()
    {
        // Personal info must be created through its factory method with validation
        _personalInfo = PersonalInfo.Create(
            "John",
            "Smith",
            "Male",
            new DateTime(1990, 5, 15),
            "American",
            "Software Engineer",
            TimeProvider.System).Value;

        _customer = new Customer(
            _personalInfo,
            _identificationInfo,
            _contactInfo,
            _address,
            _physicalInfo,
            _accommodationPreferences,
            _emergencyContact,
            _medicalInfo);
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

        _customer!.UpdatePersonalInfo(newPersonalInfo);
        _personalInfo = newPersonalInfo;
    }

    [When(@"I update the customer with new identification information")]
    public void WhenIUpdateTheCustomerWithNewIdentificationInformation()
    {
        var newIdentificationInfo = new IdentificationInfo("987654321", "Canadian");
        _customer!.UpdateIdentificationInfo(newIdentificationInfo);
        _identificationInfo = newIdentificationInfo;
    }

    [When(@"I update the customer with new contact information")]
    public void WhenIUpdateTheCustomerWithNewContactInformation()
    {
        var newContactInfo = new ContactInfo("jane.doe@example.com", "+9876543210", "@janedoe", null);
        _customer!.UpdateContactInfo(newContactInfo);
        _contactInfo = newContactInfo;
    }

    [When(@"I update the customer with new address")]
    public void WhenIUpdateTheCustomerWithNewAddress()
    {
        var newAddress = new Address("456 Oak Avenue", "Suite 100", "Uptown", "54321", "Toronto", "ON", "Canada");
        _customer!.UpdateAddress(newAddress);
        _address = newAddress;
    }

    [When(@"I update the customer with new physical information")]
    public void WhenIUpdateTheCustomerWithNewPhysicalInformation()
    {
        var newPhysicalInfo = new PhysicalInfo(68m, 165, BikeType.EBike);
        _customer!.UpdatePhysicalInfo(newPhysicalInfo);
        _physicalInfo = newPhysicalInfo;
    }

    [When(@"I update the customer with new accommodation preferences")]
    public void WhenIUpdateTheCustomerWithNewAccommodationPreferences()
    {
        var newAccommodationPreferences = new AccommodationPreferences(RoomType.DoubleRoom, BedType.DoubleBed, 5);
        _customer!.UpdateAccommodationPreferences(newAccommodationPreferences);
        _accommodationPreferences = newAccommodationPreferences;
    }

    [When(@"I update the customer with new emergency contact")]
    public void WhenIUpdateTheCustomerWithNewEmergencyContact()
    {
        var newEmergencyContact = new EmergencyContact("Bob Doe", "+5555555555");
        _customer!.UpdateEmergencyContact(newEmergencyContact);
        _emergencyContact = newEmergencyContact;
    }

    [When(@"I update the customer with new medical information")]
    public void WhenIUpdateTheCustomerWithNewMedicalInformation()
    {
        var newMedicalInfo = new MedicalInfo("Lactose", "Requires medication");
        _customer!.UpdateMedicalInfo(newMedicalInfo);
        _medicalInfo = newMedicalInfo;
    }

    [Then(@"the customer should be created successfully")]
    public void ThenTheCustomerShouldBeCreatedSuccessfully()
    {
        Assert.NotNull(_customer);
    }

    [Then(@"the customer should contain all the provided information")]
    public void ThenTheCustomerShouldContainAllTheProvidedInformation()
    {
        Assert.NotNull(_customer);
        Assert.Equal(_personalInfo, _customer.PersonalInfo);
        Assert.Equal(_identificationInfo, _customer.IdentificationInfo);
        Assert.Equal(_contactInfo, _customer.ContactInfo);
        Assert.Equal(_address, _customer.Address);
        Assert.Equal(_physicalInfo, _customer.PhysicalInfo);
        Assert.Equal(_accommodationPreferences, _customer.AccommodationPreferences);
        Assert.Equal(_emergencyContact, _customer.EmergencyContact);
        Assert.Equal(_medicalInfo, _customer.MedicalInfo);
    }

    [Then(@"the customer personal information should be updated")]
    public void ThenTheCustomerPersonalInformationShouldBeUpdated()
    {
        Assert.NotNull(_customer);
        Assert.Equal(_personalInfo, _customer.PersonalInfo);
    }

    [Then(@"the customer identification information should be updated")]
    public void ThenTheCustomerIdentificationInformationShouldBeUpdated()
    {
        Assert.NotNull(_customer);
        Assert.Equal(_identificationInfo, _customer.IdentificationInfo);
    }

    [Then(@"the customer contact information should be updated")]
    public void ThenTheCustomerContactInformationShouldBeUpdated()
    {
        Assert.NotNull(_customer);
        Assert.Equal(_contactInfo, _customer.ContactInfo);
    }

    [Then(@"the customer address should be updated")]
    public void ThenTheCustomerAddressShouldBeUpdated()
    {
        Assert.NotNull(_customer);
        Assert.Equal(_address, _customer.Address);
    }

    [Then(@"the customer physical information should be updated")]
    public void ThenTheCustomerPhysicalInformationShouldBeUpdated()
    {
        Assert.NotNull(_customer);
        Assert.Equal(_physicalInfo, _customer.PhysicalInfo);
    }

    [Then(@"the customer accommodation preferences should be updated")]
    public void ThenTheCustomerAccommodationPreferencesShouldBeUpdated()
    {
        Assert.NotNull(_customer);
        Assert.Equal(_accommodationPreferences, _customer.AccommodationPreferences);
    }

    [Then(@"the customer emergency contact should be updated")]
    public void ThenTheCustomerEmergencyContactShouldBeUpdated()
    {
        Assert.NotNull(_customer);
        Assert.Equal(_emergencyContact, _customer.EmergencyContact);
    }

    [Then(@"the customer medical information should be updated")]
    public void ThenTheCustomerMedicalInformationShouldBeUpdated()
    {
        Assert.NotNull(_customer);
        Assert.Equal(_medicalInfo, _customer.MedicalInfo);
    }

    [Given(@"I have personal information for sanitization with first name ""([^""]*)"" and last name ""([^""]*)""")] 
    public void GivenIHavePersonalInformationForSanitizationWithFirstNameAndLastName(string firstName, string lastName)
    {
        _personalInfoResult = PersonalInfo.Create(
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
        if (_personalInfoResult?.IsSuccess == true)
        {
            _personalInfo = _personalInfoResult.Value.Value;
        }
    }

    [Then(@"the personal information should be created successfully from sanitization")]
    public void ThenThePersonalInformationShouldBeCreatedSuccessfullyFromSanitization()
    {
        Assert.True(_personalInfoResult?.IsSuccess);
        Assert.NotNull(_personalInfo);
    }

    [Then(@"the sanitized first name should be ""(.*)""")] 
    public void ThenTheSanitizedFirstNameShouldBe(string expectedFirstName)
    {
        Assert.Equal(expectedFirstName, _personalInfo.FirstName);
    }

    [Then(@"the sanitized last name should be ""(.*)""")] 
    public void ThenTheSanitizedLastNameShouldBe(string expectedLastName)
    {
        Assert.Equal(expectedLastName, _personalInfo.LastName);
    }

    [Given(@"I have address for sanitization with city ""(.*)"" and country ""(.*)""")] 
    public void GivenIHaveAddressForSanitizationWithCityAndCountry(string city, string country)
    {
        _address = new Address(
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
        Assert.Equal(expectedCity, _address.City);
    }

    [Then(@"the sanitized address country should be ""(.*)""")] 
    public void ThenTheSanitizedAddressCountryShouldBe(string expectedCountry)
    {
        Assert.Equal(expectedCountry, _address.Country);
    }
}
