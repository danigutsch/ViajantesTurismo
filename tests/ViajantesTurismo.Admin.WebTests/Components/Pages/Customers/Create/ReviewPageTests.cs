using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Tests.Shared.Fakes.ApiClients;
using ViajantesTurismo.Admin.Web;
using ViajantesTurismo.Admin.Web.Components.Pages.Customers.Create;
using ViajantesTurismo.Admin.Web.Models;

namespace ViajantesTurismo.Admin.WebTests.Components.Pages.Customers.Create;

public sealed class ReviewPageTests : BunitContext
{
    private readonly FakeCustomersApiClient _fakeCustomersApi = new();
    private readonly CustomerCreationState _state = new();

    public ReviewPageTests()
    {
        Services.AddSingleton(_state);
        Services.AddSingleton<ICustomersApiClient>(_fakeCustomersApi);
    }

    [Fact]
    public async Task SubmitCustomer_When_Create_Fails_Shows_Sanitized_Error_Message()
    {
        // Arrange
        SeedCompletedState();
        _fakeCustomersApi.SetCreateCustomerException(new InvalidOperationException("Backend is on vacation"));

        var cut = Render<Review>();

        // Act
        var submitButton = cut.FindAll("button")
            .First(button => button.TextContent.Contains("Create Customer", StringComparison.Ordinal));
        await cut.InvokeAsync(() => submitButton.Click());

        // Assert
        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find(".alert.alert-danger");
            Assert.Contains("We couldn't create the customer right now. Please try again.", alert.TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain("Backend is on vacation", alert.TextContent, StringComparison.Ordinal);
        });
    }

    private void SeedCompletedState()
    {
        _state.SetPersonalInfo(new PersonalInfoFormModel
        {
            FirstName = "Ana",
            LastName = "Silva",
            BirthDate = new DateTime(1990, 5, 1),
            Gender = "Female",
            Nationality = "Brazilian",
            Occupation = "Designer"
        });

        _state.SetIdentificationInfo(new IdentificationInfoFormModel
        {
            NationalId = "123456789",
            IdNationality = "Brazil"
        });

        _state.SetContactInfo(new ContactInfoFormModel
        {
            Email = "ana.silva@example.com",
            Mobile = "+55 11 99999-9999"
        });

        _state.SetAddress(new AddressFormModel
        {
            Street = "Rua das Flores",
            Neighborhood = "Centro",
            PostalCode = "01000-000",
            City = "São Paulo",
            State = "SP",
            Country = "Brazil"
        });

        _state.SetPhysicalInfo(new PhysicalInfoFormModel
        {
            WeightKg = 65,
            HeightCentimeters = 170,
            BikeType = BikeTypeDto.Regular
        });

        _state.SetAccommodationPreferences(new AccommodationPreferencesFormModel
        {
            RoomType = RoomTypeDto.DoubleOccupancy,
            BedType = BedTypeDto.SingleBed
        });

        _state.SetEmergencyContact(new EmergencyContactFormModel
        {
            Name = "Maria Silva",
            Mobile = "+55 11 98888-8888"
        });

        _state.SetMedicalInfo(new MedicalInfoFormModel
        {
            Allergies = "None",
            AdditionalInfo = "N/A"
        });
    }
}
