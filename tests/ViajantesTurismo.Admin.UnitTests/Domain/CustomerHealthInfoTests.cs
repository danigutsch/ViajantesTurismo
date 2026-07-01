using ViajantesTurismo.Admin.Domain.Customers;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

public sealed class CustomerHealthInfoTests
{
    [Fact]
    public void Constructor_should_store_emergency_contact_and_medical_info()
    {
        // Arrange
        var emergencyContact = EmergencyContact.Create("Emergency Contact", "+9876543210").Value;
        var medicalInfo = MedicalInfo.Create("None", "None").Value;

        // Act
        var healthInfo = new CustomerHealthInfo(emergencyContact, medicalInfo);

        // Assert
        Assert.Same(emergencyContact, healthInfo.EmergencyContact);
        Assert.Same(medicalInfo, healthInfo.MedicalInfo);
    }
}
