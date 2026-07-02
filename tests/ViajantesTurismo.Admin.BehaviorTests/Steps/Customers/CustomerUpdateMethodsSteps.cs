using ViajantesTurismo.Admin.Domain.Shared;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps.Customers;

[Binding]
public sealed class CustomerUpdateMethodsSteps(CustomerContext customerContext)
{
    [Given(@"a customer exists with personal info ""(.*)"" ""(.*)""")]
    public void GivenACustomerExistsWithPersonalInfo(string firstName, string lastName)
    {
        customerContext.Customer = EntityBuilders.BuildCustomer(new CustomerOptions(
            FirstName: firstName,
            LastName: lastName));
    }

    [Given(@"a customer exists with passport ""(.*)""")]
    public void GivenACustomerExistsWithPassport(string passport)
    {
        customerContext.Customer = EntityBuilders.BuildCustomer(new CustomerOptions(PassportNumber: passport));
    }

    [Given(@"a customer exists with email ""(.*)""")]
    public void GivenACustomerExistsWithEmail(string email)
    {
        customerContext.Customer = EntityBuilders.BuildCustomer(new CustomerOptions(Email: email));
        customerContext.Customers.Add(customerContext.Customer);
        customerContext.CustomerStore.AddExistingCustomer(customerContext.Customer);
    }

    [Given(@"a customer exists with city ""(.*)""")]
    public void GivenACustomerExistsWithCity(string city)
    {
        customerContext.Customer = EntityBuilders.BuildCustomer(new CustomerOptions(City: city));
    }

    [Given("a customer exists with height (.*)")]
    public void GivenACustomerExistsWithHeight(int height)
    {
        customerContext.Customer = EntityBuilders.BuildCustomer(new CustomerOptions(HeightCentimeters: height));
    }

    [Given(@"a customer exists with bed type ""(.*)""")]
    public void GivenACustomerExistsWithBedType(string bedType)
    {
        var bedTypeEnum = Enum.Parse<BedType>(bedType + "Bed");
        customerContext.Customer = EntityBuilders.BuildCustomer(new CustomerOptions(PreferredBed: bedTypeEnum));
    }

    [Given(@"a customer exists with emergency contact ""(.*)""")]
    public void GivenACustomerExistsWithEmergencyContact(string name)
    {
        customerContext.Customer = EntityBuilders.BuildCustomer(new CustomerOptions(EmergencyContactName: name));
    }

    [Given(@"a customer exists with allergies ""(.*)""")]
    public void GivenACustomerExistsWithAllergies(string allergies)
    {
        customerContext.Customer = EntityBuilders.BuildCustomer(new CustomerOptions(Allergies: allergies));
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

    [When("I update the physical info to height (.*)")]
    public void WhenIUpdateThePhysicalInfoToHeight(int height)
    {
        var physicalInfo = PhysicalInfo.Create(70, height, BikeType.Regular).Value;
        customerContext.Customer.UpdatePhysicalInfo(physicalInfo);
    }

    [When(@"I update the accommodation preferences to bed type ""(.*)""")]
    public void WhenIUpdateTheAccommodationPreferencesToBedType(string bedType)
    {
        var bedTypeEnum = Enum.Parse<BedType>(bedType + "Bed");
        var accommodationPreferences = AccommodationPreferences.Create(RoomType.DoubleOccupancy, bedTypeEnum, null).Value;
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

    [Then("the customer personal info update should succeed")]
    public void ThenTheCustomerPersonalInfoUpdateShouldSucceed()
    {
        (customerContext.Customer).ShouldNotBeNull();
    }

    [Then("the customer identification info update should succeed")]
    public void ThenTheCustomerIdentificationInfoUpdateShouldSucceed()
    {
        (customerContext.Customer).ShouldNotBeNull();
    }

    [Then("the customer contact info update should succeed")]
    public void ThenTheCustomerContactInfoUpdateShouldSucceed()
    {
        (customerContext.Customer).ShouldNotBeNull();
    }

    [Then("the customer address update should succeed")]
    public void ThenTheCustomerAddressUpdateShouldSucceed()
    {
        (customerContext.Customer).ShouldNotBeNull();
    }

    [Then("the customer physical info update should succeed")]
    public void ThenTheCustomerPhysicalInfoUpdateShouldSucceed()
    {
        (customerContext.Customer).ShouldNotBeNull();
    }

    [Then("the customer accommodation preferences update should succeed")]
    public void ThenTheCustomerAccommodationPreferencesUpdateShouldSucceed()
    {
        (customerContext.Customer).ShouldNotBeNull();
    }

    [Then("the customer emergency contact update should succeed")]
    public void ThenTheCustomerEmergencyContactUpdateShouldSucceed()
    {
        (customerContext.Customer).ShouldNotBeNull();
    }

    [Then("the customer medical info update should succeed")]
    public void ThenTheCustomerMedicalInfoUpdateShouldSucceed()
    {
        (customerContext.Customer).ShouldNotBeNull();
    }

    [Then("all customer updates should succeed")]
    public void ThenAllCustomerUpdatesShouldSucceed()
    {
        (customerContext.Customer).ShouldNotBeNull();
    }

    [Then(@"the customer should have first name ""(.*)""")]
    public void ThenTheCustomerShouldHaveFirstName(string expectedFirstName)
    {
        (customerContext.Customer.PersonalInfo.FirstName).ShouldBe(expectedFirstName);
    }

    [Then(@"the customer should have last name ""(.*)""")]
    public void ThenTheCustomerShouldHaveLastName(string expectedLastName)
    {
        (customerContext.Customer.PersonalInfo.LastName).ShouldBe(expectedLastName);
    }

    [Then(@"the customer should have passport ""(.*)""")]
    public void ThenTheCustomerShouldHavePassport(string expectedPassport)
    {
        (customerContext.Customer.IdentificationInfo.NationalId).ShouldBe(expectedPassport);
    }

    [Then(@"the customer should have email ""(.*)""")]
    public void ThenTheCustomerShouldHaveEmail(string expectedEmail)
    {
        (customerContext.Customer.ContactInfo.Email).ShouldBe(expectedEmail);
    }

    [Then(@"the customer should have city ""(.*)""")]
    public void ThenTheCustomerShouldHaveCity(string expectedCity)
    {
        (customerContext.Customer.Address.City).ShouldBe(expectedCity);
    }

    [Then("the customer should have height (.*)")]
    public void ThenTheCustomerShouldHaveHeight(int expectedHeight)
    {
        (customerContext.Customer.PhysicalInfo.HeightCentimeters).ShouldBe(expectedHeight);
    }

    [Then(@"the customer should have bed type ""(.*)""")]
    public void ThenTheCustomerShouldHaveBedType(string expectedBedType)
    {
        var bedTypeEnum = Enum.Parse<BedType>(expectedBedType + "Bed");
        (customerContext.Customer.AccommodationPreferences.BedType).ShouldBe(bedTypeEnum);
    }

    [Then(@"the customer should have emergency contact ""(.*)""")]
    public void ThenTheCustomerShouldHaveEmergencyContact(string expectedName)
    {
        (customerContext.Customer.EmergencyContact.Name).ShouldBe(expectedName);
    }

    [Then(@"the customer should have allergies ""(.*)""")]
    public void ThenTheCustomerShouldHaveAllergies(string expectedAllergies)
    {
        (customerContext.Customer.MedicalInfo.Allergies).ShouldBe(expectedAllergies);
    }
}
