using ViajantesTurismo.Admin.Application.Mappings;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Testing.Behavior;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

public sealed class CustomerReviseTests
{
    [Fact]
    public void Revise_applies_every_change_when_all_values_are_valid()
    {
        // Arrange
        var customer = EntityBuilders.BuildCustomer(null);
        var definition = CustomerMapper.MapToDefinition(EntityBuilders.BuildCustomer(new CustomerOptions(
            FirstName: "Revised",
            PassportNumber: "B98765432",
            Email: "revised@example.com",
            City: "Porto",
            HeightCentimeters: 180,
            PreferredBed: BedType.DoubleBed,
            EmergencyContactName: "New Contact",
            Allergies: "Pollen")));
        var today = DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime);

        // Act
        var result = customer.Revise(definition, today);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        customer.PersonalInfo.FirstName.ShouldBe("Revised");
        customer.IdentificationInfo.NationalId.ShouldBe("B98765432");
        customer.ContactInfo.Email.ShouldBe("revised@example.com");
        customer.Address.City.ShouldBe("Porto");
        customer.PhysicalInfo.HeightCentimeters.ShouldBe(180);
        customer.AccommodationPreferences.BedType.ShouldBe(BedType.DoubleBed);
        customer.EmergencyContact.Name.ShouldBe("New Contact");
        customer.MedicalInfo.Allergies.ShouldBe("Pollen");
    }

    [Fact]
    public void Revise_leaves_the_customer_unchanged_when_any_value_is_invalid()
    {
        // Arrange
        var customer = EntityBuilders.BuildCustomer(null);
        var originalPersonalInfo = customer.PersonalInfo;
        var originalAddress = customer.Address;
        var originalMedicalInfo = customer.MedicalInfo;
        var revised = CustomerMapper.MapToDefinition(
            EntityBuilders.BuildCustomer(new CustomerOptions(FirstName: "Revised", City: "Porto")));
        var definition = revised with
        {
            HealthInfo = revised.HealthInfo with
            {
                Allergies = new string('a', 501)
            }
        };
        var today = DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime);

        // Act
        var result = customer.Revise(definition, today);

        // Assert
        result.IsFailure.ShouldBeTrue();
        customer.PersonalInfo.ShouldBeSameAs(originalPersonalInfo);
        customer.Address.ShouldBeSameAs(originalAddress);
        customer.MedicalInfo.ShouldBeSameAs(originalMedicalInfo);
    }
}
