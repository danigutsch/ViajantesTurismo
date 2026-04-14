using System.Text;
using ViajantesTurismo.Admin.Web.Components.Pages.Customers;

namespace ViajantesTurismo.Admin.WebTests.Components.Pages.Customers;

public sealed class ImportCustomersConflictLoaderTests
{
    private static readonly string AllCanonicalHeaders =
        string.Join(",", CustomerImportHeaderMatcher.Fields.Select(field => field.Name));

    [Fact]
    public async Task LoadConflictStates_When_Existing_Customer_Is_Found_Populates_Existing_And_Incoming_Values()
    {
        // Arrange
        const string conflictEmail = "existing@example.com";
        const string incomingFirstName = "IncomingFirst";
        const string existingLastName = "ExistingLast";

        var customersApi = new FakeCustomersApiClient();
        var customerId = Guid.NewGuid();
        customersApi.AddCustomer(new GetCustomerDto
        {
            Id = customerId,
            FirstName = "ExistingFirst",
            LastName = existingLastName,
            Email = conflictEmail,
            Mobile = "+551111111111",
            Nationality = "Brazilian",
            BikeType = BikeTypeDto.Regular,
        });
        customersApi.AddCustomerDetails(BuildCustomerDetails(customerId, conflictEmail, existingLastName));

        var mappedFileBytes = Encoding.UTF8.GetBytes(AllCanonicalHeaders + "\n" + BuildCsvRow(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [CustomerImportFieldNames.FirstName] = incomingFirstName,
            [CustomerImportFieldNames.LastName] = "IncomingLast",
            [CustomerImportFieldNames.Email] = conflictEmail,
        }));

        // Act
        var conflictStates = await ImportCustomersConflictLoader.LoadConflictStates(
            customersApi,
            [new ImportConflictDto(conflictEmail)],
            mappedFileBytes);

        // Assert
        var conflictState = Assert.Single(conflictStates);
        Assert.Equal(conflictEmail, conflictState.Email);
        Assert.Equal(incomingFirstName, conflictState.GetIncomingValue(CustomerImportFieldNames.FirstName));
        Assert.Equal(existingLastName, conflictState.GetExistingValue(CustomerImportFieldNames.LastName));
        Assert.Equal(conflictEmail, conflictState.GetExistingValue(CustomerImportFieldNames.Email));
    }

    [Fact]
    public async Task LoadConflictStates_When_Existing_Customer_Details_Are_Missing_Leaves_Existing_Values_Empty()
    {
        // Arrange
        const string conflictEmail = "missing-details@example.com";

        var customersApi = new FakeCustomersApiClient();
        customersApi.AddCustomer(new GetCustomerDto
        {
            Id = Guid.NewGuid(),
            FirstName = "ExistingFirst",
            LastName = "ExistingLast",
            Email = conflictEmail,
            Mobile = "+551111111111",
            Nationality = "Brazilian",
            BikeType = BikeTypeDto.Regular,
        });

        var mappedFileBytes = Encoding.UTF8.GetBytes(AllCanonicalHeaders + "\n" + BuildCsvRow(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [CustomerImportFieldNames.FirstName] = "IncomingFirst",
            [CustomerImportFieldNames.Email] = conflictEmail,
        }));

        // Act
        var conflictStates = await ImportCustomersConflictLoader.LoadConflictStates(
            customersApi,
            [new ImportConflictDto(conflictEmail)],
            mappedFileBytes);

        // Assert
        var conflictState = Assert.Single(conflictStates);
        Assert.Equal("IncomingFirst", conflictState.GetIncomingValue(CustomerImportFieldNames.FirstName));
        Assert.Equal(string.Empty, conflictState.GetExistingValue(CustomerImportFieldNames.FirstName));
        Assert.Equal(string.Empty, conflictState.GetExistingValue(CustomerImportFieldNames.Email));
    }

    private static string BuildCsvRow(IReadOnlyDictionary<string, string> valuesByField)
    {
        var values = CustomerImportHeaderMatcher.Fields
            .Select(field => valuesByField.GetValueOrDefault(field.Name, "v"));

        return string.Join(",", values);
    }

    private static CustomerDetailsDto BuildCustomerDetails(Guid customerId, string email, string lastName)
    {
        return new CustomerDetailsDto
        {
            Id = customerId,
            PersonalInfo = new PersonalInfoDto
            {
                FirstName = "ExistingFirst",
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
        };
    }
}
