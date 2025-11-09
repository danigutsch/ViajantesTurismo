using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Infrastructure;

namespace ViajantesTurismo.Admin.IntegrationTests;

[Collection("Api collection")]
public sealed class CustomerApiTests : IDisposable
{
    private readonly HttpClient _client;
    private readonly ApplicationDbContext _dbContext;
    private readonly IServiceScope _scope;

    public CustomerApiTests(ApiFixture fixture)
    {
        _client = fixture.CreateClient();

        using var scope = fixture.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var seeder = new Seeder(dbContext);
        seeder.Seed();

        _scope = fixture.Services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    public void Dispose()
    {
        _client.Dispose();
        _dbContext.Dispose();
        _scope.Dispose();
    }

    [Fact]
    public async Task Can_Get_Customers()
    {
        // Act
        var response = await _client.GetAsync(new Uri("/customers", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var customers = await response.Content.ReadFromJsonAsync<GetCustomerDto[]>(TestContext.Current.CancellationToken);
        Assert.NotNull(customers);
        Assert.NotEmpty(customers);
    }

    [Fact]
    public async Task Can_Create_Customer()
    {
        // Arrange
        var request = new CreateCustomerDto
        {
            PersonalInfo = new PersonalInfoDto
            {
                FirstName = "John",
                LastName = "Doe",
                BirthDate = new DateTime(1990, 1, 1).ToUniversalTime(),
                Gender = "Male",
                Nationality = "American",
                Profession = "Engineer"
            },
            IdentificationInfo = new IdentificationInfoDto
            {
                NationalId = "123456789",
                IdNationality = "American"
            },
            ContactInfo = new ContactInfoDto
            {
                Email = "john.doe@example.com",
                Mobile = "+15551234567",
                Instagram = "@johndoe",
                Facebook = "john.doe"
            },
            Address = new AddressDto
            {
                Street = "123 Main St",
                Complement = null,
                Neighborhood = "Downtown",
                PostalCode = "12345",
                City = "New York",
                State = "NY",
                Country = "USA"
            },
            PhysicalInfo = new PhysicalInfoDto
            {
                WeightKg = 75.0m,
                HeightCentimeters = 180,
                BikeType = BikeTypeDto.Regular
            },
            AccommodationPreferences = new AccommodationPreferencesDto
            {
                RoomType = RoomTypeDto.SingleRoom,
                BedType = BedTypeDto.SingleBed,
                CompanionId = null
            },
            EmergencyContact = new EmergencyContactDto
            {
                Name = "Jane Doe",
                Mobile = "+15559876543"
            },
            MedicalInfo = new MedicalInfoDto
            {
                Allergies = null,
                AdditionalInfo = null
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync(new Uri("/customers", UriKind.Relative), request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var customer = _dbContext.Customers.FirstOrDefault(c => c.PersonalInfo.FirstName == request.PersonalInfo.FirstName && c.PersonalInfo.LastName == request.PersonalInfo.LastName);
        Assert.NotNull(customer);
    }

    [Fact]
    public async Task Can_Get_Customer_By_Id()
    {
        // Arrange
        var request = new CreateCustomerDto
        {
            PersonalInfo = new PersonalInfoDto
            {
                FirstName = "Jane",
                LastName = "Smith",
                BirthDate = new DateTime(1985, 5, 15).ToUniversalTime(),
                Gender = "Female",
                Nationality = "Canadian",
                Profession = "Teacher"
            },
            IdentificationInfo = new IdentificationInfoDto
            {
                NationalId = "987654321",
                IdNationality = "Canadian"
            },
            ContactInfo = new ContactInfoDto
            {
                Email = "jane.smith@example.com",
                Mobile = "+14161234567",
                Instagram = "@janesmith",
                Facebook = "jane.smith"
            },
            Address = new AddressDto
            {
                Street = "456 Oak St",
                Complement = "Apt 2",
                Neighborhood = "Midtown",
                PostalCode = "M5V 1A1",
                City = "Toronto",
                State = "ON",
                Country = "Canada"
            },
            PhysicalInfo = new PhysicalInfoDto
            {
                WeightKg = 65.0m,
                HeightCentimeters = 165,
                BikeType = BikeTypeDto.EBike
            },
            AccommodationPreferences = new AccommodationPreferencesDto
            {
                RoomType = RoomTypeDto.DoubleRoom,
                BedType = BedTypeDto.DoubleBed,
                CompanionId = null
            },
            EmergencyContact = new EmergencyContactDto
            {
                Name = "Bob Smith",
                Mobile = "+14169876543"
            },
            MedicalInfo = new MedicalInfoDto
            {
                Allergies = "Peanuts",
                AdditionalInfo = null
            }
        };
        var createResponse = await _client.PostAsJsonAsync(new Uri("/customers", UriKind.Relative), request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var location = createResponse.Headers.Location;
        Assert.NotNull(location);

        // Act
        var response = await _client.GetAsync(location, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var customerDto = await response.Content.ReadFromJsonAsync<CustomerDetailsDto>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(customerDto);
        Assert.Equal(request.PersonalInfo.FirstName, customerDto.PersonalInfo.FirstName);
        Assert.Equal(request.PersonalInfo.LastName, customerDto.PersonalInfo.LastName);
        Assert.Equal(request.ContactInfo.Email, customerDto.ContactInfo.Email);
        Assert.Equal(request.ContactInfo.Mobile, customerDto.ContactInfo.Mobile);
        Assert.Equal(request.PersonalInfo.Nationality, customerDto.PersonalInfo.Nationality);
    }

    [Fact]
    public async Task Get_Customer_By_Id_Returns_Not_Found_For_Invalid_Id()
    {
        // Arrange
        const int invalidId = -1;

        // Act
        var response = await _client.GetAsync(new Uri($"/customers/{invalidId}", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Can_Update_Customer()
    {
        // Arrange
        var createRequest = new CreateCustomerDto
        {
            PersonalInfo = new PersonalInfoDto
            {
                FirstName = "Alice",
                LastName = "Johnson",
                BirthDate = new DateTime(1992, 3, 10).ToUniversalTime(),
                Gender = "Female",
                Nationality = "British",
                Profession = "Designer"
            },
            IdentificationInfo = new IdentificationInfoDto
            {
                NationalId = "UK123456",
                IdNationality = "British"
            },
            ContactInfo = new ContactInfoDto
            {
                Email = "alice.johnson@example.com",
                Mobile = "+447123456789",
                Instagram = "@alicejohnson",
                Facebook = "alice.johnson"
            },
            Address = new AddressDto
            {
                Street = "789 High St",
                Complement = null,
                Neighborhood = "Westminster",
                PostalCode = "SW1A 1AA",
                City = "London",
                State = "England",
                Country = "UK"
            },
            PhysicalInfo = new PhysicalInfoDto
            {
                WeightKg = 60.0m,
                HeightCentimeters = 170,
                BikeType = BikeTypeDto.Regular
            },
            AccommodationPreferences = new AccommodationPreferencesDto
            {
                RoomType = RoomTypeDto.SingleRoom,
                BedType = BedTypeDto.SingleBed,
                CompanionId = null
            },
            EmergencyContact = new EmergencyContactDto
            {
                Name = "Bob Johnson",
                Mobile = "+447987654321"
            },
            MedicalInfo = new MedicalInfoDto
            {
                Allergies = null,
                AdditionalInfo = null
            }
        };
        var createResponse = await _client.PostAsJsonAsync(new Uri("/customers", UriKind.Relative), createRequest, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var location = createResponse.Headers.Location;
        Assert.NotNull(location);

        var customerId = int.Parse(location.ToString().Split('/').Last());

        var updateRequest = new UpdateCustomerDto
        {
            PersonalInfo = new PersonalInfoDto
            {
                FirstName = "Alice",
                LastName = "Johnson-Smith", // Updated last name
                BirthDate = new DateTime(1992, 3, 10).ToUniversalTime(),
                Gender = "Female",
                Nationality = "British",
                Profession = "Senior Designer" // Updated profession
            },
            IdentificationInfo = new IdentificationInfoDto
            {
                NationalId = "UK123456",
                IdNationality = "British"
            },
            ContactInfo = new ContactInfoDto
            {
                Email = "alice.smith@example.com", // Updated email
                Mobile = "+447123456789",
                Instagram = "@alicesmith", // Updated Instagram
                Facebook = "alice.smith" // Updated Facebook
            },
            Address = new AddressDto
            {
                Street = "456 Baker St", // Updated address
                Complement = "Flat 3B",
                Neighborhood = "Marylebone",
                PostalCode = "NW1 6XE",
                City = "London",
                State = "England",
                Country = "UK"
            },
            PhysicalInfo = new PhysicalInfoDto
            {
                WeightKg = 62.0m, // Updated weight
                HeightCentimeters = 170,
                BikeType = BikeTypeDto.EBike // Updated bike type
            },
            AccommodationPreferences = new AccommodationPreferencesDto
            {
                RoomType = RoomTypeDto.DoubleRoom, // Updated room type
                BedType = BedTypeDto.DoubleBed, // Updated bed type
                CompanionId = null
            },
            EmergencyContact = new EmergencyContactDto
            {
                Name = "Robert Johnson", // Updated emergency contact name
                Mobile = "+447987654321"
            },
            MedicalInfo = new MedicalInfoDto
            {
                Allergies = "Lactose", // Added allergy
                AdditionalInfo = "Prefers vegetarian meals"
            }
        };

        // Act
        var response = await _client.PutAsJsonAsync(new Uri($"/customers/{customerId}", UriKind.Relative), updateRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await _client.GetAsync(location, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var updatedCustomer = await getResponse.Content.ReadFromJsonAsync<CustomerDetailsDto>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(updatedCustomer);
        Assert.Equal(updateRequest.PersonalInfo.LastName, updatedCustomer.PersonalInfo.LastName);
        Assert.Equal(updateRequest.PersonalInfo.Profession, updatedCustomer.PersonalInfo.Profession);
        Assert.Equal(updateRequest.ContactInfo.Email, updatedCustomer.ContactInfo.Email);
        Assert.Equal(updateRequest.ContactInfo.Instagram, updatedCustomer.ContactInfo.Instagram);
        Assert.Equal(updateRequest.Address.Street, updatedCustomer.Address.Street);
        Assert.Equal(updateRequest.Address.Complement, updatedCustomer.Address.Complement);
        Assert.Equal(updateRequest.PhysicalInfo.WeightKg, updatedCustomer.PhysicalInfo.WeightKg);
        Assert.Equal(updateRequest.PhysicalInfo.BikeType, updatedCustomer.PhysicalInfo.BikeType);
        Assert.Equal(updateRequest.AccommodationPreferences.RoomType, updatedCustomer.AccommodationPreferences.RoomType);
        Assert.Equal(updateRequest.MedicalInfo.Allergies, updatedCustomer.MedicalInfo.Allergies);
        Assert.Equal(updateRequest.MedicalInfo.AdditionalInfo, updatedCustomer.MedicalInfo.AdditionalInfo);
    }

    [Fact]
    public async Task Update_Customer_Returns_Not_Found_For_Invalid_Id()
    {
        // Arrange
        const int invalidId = 999999;
        var updateRequest = new UpdateCustomerDto
        {
            PersonalInfo = new PersonalInfoDto
            {
                FirstName = "Test",
                LastName = "User",
                BirthDate = new DateTime(1990, 1, 1).ToUniversalTime(),
                Gender = "Male",
                Nationality = "American",
                Profession = "Tester"
            },
            IdentificationInfo = new IdentificationInfoDto
            {
                NationalId = "TEST123",
                IdNationality = "American"
            },
            ContactInfo = new ContactInfoDto
            {
                Email = "test@example.com",
                Mobile = "+15551234567",
                Instagram = null,
                Facebook = null
            },
            Address = new AddressDto
            {
                Street = "123 Test St",
                Complement = null,
                Neighborhood = "Test",
                PostalCode = "12345",
                City = "Test City",
                State = "TS",
                Country = "USA"
            },
            PhysicalInfo = new PhysicalInfoDto
            {
                WeightKg = 70.0m,
                HeightCentimeters = 175,
                BikeType = BikeTypeDto.Regular
            },
            AccommodationPreferences = new AccommodationPreferencesDto
            {
                RoomType = RoomTypeDto.SingleRoom,
                BedType = BedTypeDto.SingleBed,
                CompanionId = null
            },
            EmergencyContact = new EmergencyContactDto
            {
                Name = "Test Emergency",
                Mobile = "+15559876543"
            },
            MedicalInfo = new MedicalInfoDto
            {
                Allergies = null,
                AdditionalInfo = null
            }
        };

        // Act
        var response = await _client.PutAsJsonAsync(new Uri($"/customers/{invalidId}", UriKind.Relative), updateRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
