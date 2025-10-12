using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.AdminApi.Contracts;

namespace ViajantesTurismo.IntegrationTests;

[Collection("Api collection")]
public sealed class CustomerApiTests : IDisposable
{
    private readonly HttpClient _client;
    private readonly ApplicationDbContext _dbContext;
    private readonly ApiFixture _fixture;
    private readonly IServiceScope _scope;

    public CustomerApiTests(ApiFixture fixture)
    {
        _fixture = fixture;
        _client = _fixture.CreateClient();

        using var scope = _fixture.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var seeder = new Seeder(dbContext);
        seeder.Seed();

        _scope = _fixture.Services.CreateScope();
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

        var customers = await response.Content.ReadFromJsonAsync<GetCustomerDto[]>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(customers);
        Assert.NotEmpty(customers);
    }

    [Fact]
    public async Task Can_Create_Customer()
    {
        // Arrange
        var request = new CreateCustomerDto
        {
            PersonalInfo = new PersonalInfoStepDto
            {
                FirstName = "John",
                LastName = "Doe",
                BirthDate = new DateTime(1990, 1, 1).ToUniversalTime(),
                Gender = "Male",
                Nationality = "American",
                Profession = "Engineer"
            },
            IdentificationInfo = new IdentificationInfoStepDto
            {
                NationalId = "123456789",
                IdNationality = "American"
            },
            ContactInfo = new ContactInfoStepDto
            {
                Email = "john.doe@example.com",
                Mobile = "+15551234567",
                Instagram = "@johndoe",
                Facebook = "john.doe"
            },
            Address = new AddressStepDto
            {
                Street = "123 Main St",
                Complement = null,
                Neighborhood = "Downtown",
                PostalCode = "12345",
                City = "New York",
                State = "NY",
                Country = "USA"
            },
            PhysicalInfo = new PhysicalInfoStepDto
            {
                WeightKg = 75.0m,
                HeightCentimeters = 180,
                BikeType = BikeTypeDto.Regular
            },
            AccommodationPreferences = new AccommodationPreferencesStepDto
            {
                RoomType = RoomTypeDto.SingleRoom,
                BedType = BedTypeDto.SingleBed,
                CompanionId = null
            },
            EmergencyContact = new EmergencyContactStepDto
            {
                Name = "Jane Doe",
                Mobile = "+15559876543"
            },
            MedicalInfo = new MedicalInfoStepDto
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
        // Arrange: create a customer first
        var request = new CreateCustomerDto
        {
            PersonalInfo = new PersonalInfoStepDto
            {
                FirstName = "Jane",
                LastName = "Smith",
                BirthDate = new DateTime(1985, 5, 15).ToUniversalTime(),
                Gender = "Female",
                Nationality = "Canadian",
                Profession = "Teacher"
            },
            IdentificationInfo = new IdentificationInfoStepDto
            {
                NationalId = "987654321",
                IdNationality = "Canadian"
            },
            ContactInfo = new ContactInfoStepDto
            {
                Email = "jane.smith@example.com",
                Mobile = "+14161234567",
                Instagram = "@janesmith",
                Facebook = "jane.smith"
            },
            Address = new AddressStepDto
            {
                Street = "456 Oak St",
                Complement = "Apt 2",
                Neighborhood = "Midtown",
                PostalCode = "M5V 1A1",
                City = "Toronto",
                State = "ON",
                Country = "Canada"
            },
            PhysicalInfo = new PhysicalInfoStepDto
            {
                WeightKg = 65.0m,
                HeightCentimeters = 165,
                BikeType = BikeTypeDto.EBike
            },
            AccommodationPreferences = new AccommodationPreferencesStepDto
            {
                RoomType = RoomTypeDto.DoubleRoom,
                BedType = BedTypeDto.DoubleBed,
                CompanionId = null
            },
            EmergencyContact = new EmergencyContactStepDto
            {
                Name = "Bob Smith",
                Mobile = "+14169876543"
            },
            MedicalInfo = new MedicalInfoStepDto
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
        var customerDto = await response.Content.ReadFromJsonAsync<GetCustomerDto>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(customerDto);
        Assert.Equal(request.PersonalInfo.FirstName, customerDto.FirstName);
        Assert.Equal(request.PersonalInfo.LastName, customerDto.LastName);
        Assert.Equal(request.ContactInfo.Email, customerDto.Email);
        Assert.Equal(request.ContactInfo.Mobile, customerDto.Mobile);
        Assert.Equal(request.PersonalInfo.Nationality, customerDto.Nationality);
    }

    [Fact]
    public async Task Get_Customer_By_Id_Returns_NotFound_For_Invalid_Id()
    {
        // Arrange
        const int invalidId = -1;

        // Act
        var response = await _client.GetAsync(new Uri($"/customers/{invalidId}", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
