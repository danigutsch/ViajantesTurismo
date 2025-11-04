using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Admin.Domain.Customers;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
public sealed class CustomerUpdateMethodsSteps(CustomerContext customerContext)
{
    [Given(@"a customer exists with personal info ""(.*)"" ""(.*)""")]
    public void GivenACustomerExistsWithPersonalInfo(string firstName, string lastName)
    {
        customerContext.Customer = TestHelpers.CreateTestCustomerWithNames(firstName, lastName);
    }

    [Given(@"a customer exists with passport ""(.*)""")]
    public void GivenACustomerExistsWithPassport(string passport)
    {
        customerContext.Customer = TestHelpers.CreateTestCustomerWithPassport(passport);
    }

    [Given(@"a customer exists with email ""(.*)""")]
    public void GivenACustomerExistsWithEmail(string email)
    {
        customerContext.Customer = TestHelpers.CreateTestCustomerWithEmail(email);
    }

    [Given(@"a customer exists with city ""(.*)""")]
    public void GivenACustomerExistsWithCity(string city)
    {
        customerContext.Customer = TestHelpers.CreateTestCustomerWithCity(city);
    }

    [Given(@"a customer exists with height (.*)")]
    public void GivenACustomerExistsWithHeight(int height)
    {
        customerContext.Customer = TestHelpers.CreateTestCustomerWithHeight(height);
    }

    [Given(@"a customer exists with bed type ""(.*)""")]
    public void GivenACustomerExistsWithBedType(string bedType)
    {
        var bedTypeEnum = Enum.Parse<BedType>(bedType + "Bed");
        customerContext.Customer = TestHelpers.CreateTestCustomerWithBedType(bedTypeEnum);
    }

    [Given(@"a customer exists with emergency contact ""(.*)""")]
    public void GivenACustomerExistsWithEmergencyContact(string name)
    {
        customerContext.Customer = TestHelpers.CreateTestCustomerWithEmergencyContact(name);
    }

    [Given(@"a customer exists with allergies ""(.*)""")]
    public void GivenACustomerExistsWithAllergies(string allergies)
    {
        customerContext.Customer = TestHelpers.CreateTestCustomerWithAllergies(allergies);
    }

    [When(@"I update the personal info to ""(.*)"" ""(.*)""")]
    public void WhenIUpdateThePersonalInfoTo(string firstName, string lastName)
    {
        var personalInfo = PersonalInfo.Create(
            firstName,
            lastName,
            "Male",
            DateTime.UtcNow.AddYears(-30),
            "USA",
            "Engineer",
            TimeProvider.System).Value;
        customerContext.Customer.UpdatePersonalInfo(personalInfo);
    }

    [When(@"I update the identification info to passport ""(.*)""")]
    public void WhenIUpdateTheIdentificationInfoToPassport(string passport)
    {
        var identificationInfo = IdentificationInfo.Create(passport, "USA").Value;
        customerContext.Customer.UpdateIdentificationInfo(identificationInfo);
    }

    [When(@"I update the contact info to email ""(.*)""")]
    public void WhenIUpdateTheContactInfoToEmail(string email)
    {
        var contactInfo = ContactInfo.Create(email, "+1234567890", null, null).Value;
        customerContext.Customer.UpdateContactInfo(contactInfo);
    }

    [When(@"I update the address to city ""(.*)""")]
    public void WhenIUpdateTheAddressToCity(string city)
    {
        var address = Address.Create("123 Main St", null, "Downtown", "12345", city, "CA", "USA").Value;
        customerContext.Customer.UpdateAddress(address);
    }

    [When(@"I update the physical info to height (.*)")]
    public void WhenIUpdateThePhysicalInfoToHeight(int height)
    {
        var physicalInfo = PhysicalInfo.Create(70, height, BikeType.Regular).Value;
        customerContext.Customer.UpdatePhysicalInfo(physicalInfo);
    }

    [When(@"I update the accommodation preferences to bed type ""(.*)""")]
    public void WhenIUpdateTheAccommodationPreferencesToBedType(string bedType)
    {
        var bedTypeEnum = Enum.Parse<BedType>(bedType + "Bed");
        var accommodationPreferences = AccommodationPreferences.Create(RoomType.SingleRoom, bedTypeEnum, null).Value;
        customerContext.Customer.UpdateAccommodationPreferences(accommodationPreferences);
    }

    [When(@"I update the emergency contact to ""(.*)""")]
    public void WhenIUpdateTheEmergencyContactTo(string name)
    {
        var emergencyContact = EmergencyContact.Create(name, "+9876543210").Value;
        customerContext.Customer.UpdateEmergencyContact(emergencyContact);
    }

    [When(@"I update the medical info to allergies ""(.*)""")]
    public void WhenIUpdateTheMedicalInfoToAllergies(string allergies)
    {
        var medicalInfo = MedicalInfo.Create(allergies, "None").Value;
        customerContext.Customer.UpdateMedicalInfo(medicalInfo);
    }

    [Then(@"the customer personal info update should succeed")]
    public void ThenTheCustomerPersonalInfoUpdateShouldSucceed()
    {
        Assert.NotNull(customerContext.Customer);
    }

    [Then(@"the customer identification info update should succeed")]
    public void ThenTheCustomerIdentificationInfoUpdateShouldSucceed()
    {
        Assert.NotNull(customerContext.Customer);
    }

    [Then(@"the customer contact info update should succeed")]
    public void ThenTheCustomerContactInfoUpdateShouldSucceed()
    {
        Assert.NotNull(customerContext.Customer);
    }

    [Then(@"the customer address update should succeed")]
    public void ThenTheCustomerAddressUpdateShouldSucceed()
    {
        Assert.NotNull(customerContext.Customer);
    }

    [Then(@"the customer physical info update should succeed")]
    public void ThenTheCustomerPhysicalInfoUpdateShouldSucceed()
    {
        Assert.NotNull(customerContext.Customer);
    }

    [Then(@"the customer accommodation preferences update should succeed")]
    public void ThenTheCustomerAccommodationPreferencesUpdateShouldSucceed()
    {
        Assert.NotNull(customerContext.Customer);
    }

    [Then(@"the customer emergency contact update should succeed")]
    public void ThenTheCustomerEmergencyContactUpdateShouldSucceed()
    {
        Assert.NotNull(customerContext.Customer);
    }

    [Then(@"the customer medical info update should succeed")]
    public void ThenTheCustomerMedicalInfoUpdateShouldSucceed()
    {
        Assert.NotNull(customerContext.Customer);
    }

    [Then(@"all customer updates should succeed")]
    public void ThenAllCustomerUpdatesShouldSucceed()
    {
        Assert.NotNull(customerContext.Customer);
    }

    [Then(@"the customer should have first name ""(.*)""")]
    public void ThenTheCustomerShouldHaveFirstName(string expectedFirstName)
    {
        Assert.Equal(expectedFirstName, customerContext.Customer.PersonalInfo.FirstName);
    }

    [Then(@"the customer should have last name ""(.*)""")]
    public void ThenTheCustomerShouldHaveLastName(string expectedLastName)
    {
        Assert.Equal(expectedLastName, customerContext.Customer.PersonalInfo.LastName);
    }

    [Then(@"the customer should have passport ""(.*)""")]
    public void ThenTheCustomerShouldHavePassport(string expectedPassport)
    {
        Assert.Equal(expectedPassport, customerContext.Customer.IdentificationInfo.NationalId);
    }

    [Then(@"the customer should have email ""(.*)""")]
    public void ThenTheCustomerShouldHaveEmail(string expectedEmail)
    {
        Assert.Equal(expectedEmail, customerContext.Customer.ContactInfo.Email);
    }

    [Then(@"the customer should have city ""(.*)""")]
    public void ThenTheCustomerShouldHaveCity(string expectedCity)
    {
        Assert.Equal(expectedCity, customerContext.Customer.Address.City);
    }

    [Then(@"the customer should have height (.*)")]
    public void ThenTheCustomerShouldHaveHeight(int expectedHeight)
    {
        Assert.Equal(expectedHeight, customerContext.Customer.PhysicalInfo.HeightCentimeters);
    }

    [Then(@"the customer should have bed type ""(.*)""")]
    public void ThenTheCustomerShouldHaveBedType(string expectedBedType)
    {
        var bedTypeEnum = Enum.Parse<BedType>(expectedBedType + "Bed");
        Assert.Equal(bedTypeEnum, customerContext.Customer.AccommodationPreferences.BedType);
    }

    [Then(@"the customer should have emergency contact ""(.*)""")]
    public void ThenTheCustomerShouldHaveEmergencyContact(string expectedName)
    {
        Assert.Equal(expectedName, customerContext.Customer.EmergencyContact.Name);
    }

    [Then(@"the customer should have allergies ""(.*)""")]
    public void ThenTheCustomerShouldHaveAllergies(string expectedAllergies)
    {
        Assert.Equal(expectedAllergies, customerContext.Customer.MedicalInfo.Allergies);
    }
}