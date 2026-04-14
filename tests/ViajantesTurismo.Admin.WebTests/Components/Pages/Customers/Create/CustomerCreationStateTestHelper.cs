using ViajantesTurismo.Admin.Web;
using ViajantesTurismo.Admin.Web.Models;

namespace ViajantesTurismo.Admin.WebTests.Components.Pages.Customers.Create;

internal static class CustomerCreationStateTestHelper
{
    internal static void SeedCompletedState(
        CustomerCreationState state,
        bool includeOptionalSocials = true,
        bool includeCompanion = false,
        bool includeMedicalDetails = true)
    {
        ArgumentNullException.ThrowIfNull(state);

        state.SetPersonalInfo(new PersonalInfoFormModel
        {
            FirstName = "Ana",
            LastName = "Silva",
            BirthDate = new DateTime(1990, 5, 1, 0, 0, 0, DateTimeKind.Unspecified),
            Gender = "Female",
            Nationality = "Brazilian",
            Occupation = "Designer",
        });

        state.SetIdentificationInfo(new IdentificationInfoFormModel
        {
            NationalId = "123456789",
            IdNationality = "Brazil",
        });

        state.SetContactInfo(new ContactInfoFormModel
        {
            Email = "ana.silva@example.com",
            Mobile = "+55 11 99999-9999",
            Instagram = includeOptionalSocials ? "@ana.silva" : null,
            Facebook = includeOptionalSocials ? "facebook.com/ana.silva" : null,
        });

        state.SetAddress(new AddressFormModel
        {
            Street = "Rua das Flores",
            Neighborhood = "Centro",
            PostalCode = "01000-000",
            City = "São Paulo",
            State = "SP",
            Country = "Brazil",
        });

        state.SetPhysicalInfo(new PhysicalInfoFormModel
        {
            WeightKg = 65,
            HeightCentimeters = 170,
            BikeType = BikeTypeDto.Regular,
        });

        state.SetAccommodationPreferences(new AccommodationPreferencesFormModel
        {
            RoomType = RoomTypeDto.DoubleOccupancy,
            BedType = BedTypeDto.SingleBed,
            CompanionId = includeCompanion ? Guid.Parse("11111111-1111-1111-1111-111111111111") : null,
        });

        state.SetEmergencyContact(new EmergencyContactFormModel
        {
            Name = "Maria Silva",
            Mobile = "+55 11 98888-8888",
        });

        state.SetMedicalInfo(new MedicalInfoFormModel
        {
            Allergies = includeMedicalDetails ? "None" : null,
            AdditionalInfo = includeMedicalDetails ? "N/A" : null,
        });
    }
}
