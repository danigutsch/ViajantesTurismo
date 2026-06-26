namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers;

internal static class ImportCustomersDuplicateResolutionTestHelper
{
    public static void SeedExistingCustomer(FakeCustomersApiClient fakeCustomersApi, string email, string firstName, string lastName)
    {
        var customerId = Guid.NewGuid();

        fakeCustomersApi.AddCustomer(new GetCustomerDto
        {
            Id = customerId,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Mobile = "+551111111111",
            Nationality = "Brazilian",
            BikeType = BikeTypeDto.Regular,
        });

        fakeCustomersApi.AddCustomerDetails(new CustomerDetailsDto
        {
            Id = customerId,
            PersonalInfo = new PersonalInfoDto
            {
                FirstName = firstName,
                LastName = lastName,
                Gender = "Female",
                BirthDate = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
                Nationality = "Brazilian",
                Occupation = "Engineer",
            },
            IdentificationInfo = new IdentificationInfoDto
            {
                NationalId = "A123",
                IdNationality = "Brazilian",
            },
            ContactInfo = new ContactInfoDto
            {
                Email = email,
                Mobile = "+551111111111",
                Instagram = null,
                Facebook = null,
            },
            Address = new AddressDto
            {
                Street = "Rua A",
                Complement = null,
                Neighborhood = "Centro",
                PostalCode = "01000-000",
                City = "São Paulo",
                State = "SP",
                Country = "Brazil",
            },
            PhysicalInfo = new PhysicalInfoDto
            {
                WeightKg = 70,
                HeightCentimeters = 170,
                BikeType = BikeTypeDto.Regular,
            },
            AccommodationPreferences = new AccommodationPreferencesDto
            {
                RoomType = RoomTypeDto.DoubleOccupancy,
                BedType = BedTypeDto.SingleBed,
                CompanionId = null,
            },
            EmergencyContact = new EmergencyContactDto
            {
                Name = "Emergency Contact",
                Mobile = "+55222222222",
            },
            MedicalInfo = new MedicalInfoDto
            {
                Allergies = null,
                AdditionalInfo = null,
            },
        });
    }
}
