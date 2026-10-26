using ViajantesTurismo.Admin.Application.Mappings;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.UnitTests.Mappings;

public sealed class CustomerMapperGuardTests
{
    [Fact]
    public void Customer_definition_mappings_reject_missing_inputs_and_name_the_missing_parameter()
    {
        // Arrange
        var emergencyContact = new EmergencyContactDto { Name = "Emergency Name", Mobile = "+5511888888888" };
        var medicalInfo = new MedicalInfoDto { Allergies = null, AdditionalInfo = null };
        (string Parameter, Action Map)[] cases =
        [
            ("dto", () => CustomerMapper.MapToAccommodationPreferencesDefinition(null!)),
            ("dto", () => CustomerMapper.MapToPersonalInfoDefinition(null!)),
            ("dto", () => CustomerMapper.MapToIdentificationInfoDefinition(null!)),
            ("dto", () => CustomerMapper.MapToContactInfoDefinition(null!)),
            ("dto", () => CustomerMapper.MapToAddressDefinition(null!)),
            ("dto", () => CustomerMapper.MapToPhysicalInfoDefinition(null!)),
            ("emergencyContact", () => CustomerMapper.MapToHealthDefinition(null!, medicalInfo)),
            ("medicalInfo", () => CustomerMapper.MapToHealthDefinition(emergencyContact, null!)),
            ("customer", () => CustomerMapper.MapToDefinition(null!)),
        ];

        // Act and assert
        foreach (var (parameter, map) in cases)
        {
            map.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe(parameter);
        }
    }
}
