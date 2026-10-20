using SharedKernel.Testing;
using ViajantesTurismo.Admin.Application.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Application.Documents;

[Trait(SharedKernelTestTraitNames.CapabilityName, Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
public sealed class DocumentRequestFingerprintTests
{
    [Theory]
    [InlineData("abc", "1", "E00F2689192B0CB46E6AEF0203A77E3C8CE549DB5640C011D3D864ED0CAF6799")]
    [InlineData("🚌", "é", "0E25966EA49FD26AC1275DEFD6D3870F09DE6BEAE3EA7C93955B968DA09747B9")]
    public void Existing_document_fingerprint_encoding_remains_compatible(string templateId, string version, string expected)
    {
        // Act
        var actual = DocumentCommandIdempotency.CreateRequestFingerprint(templateId, version);

        // Assert
        actual.ShouldBe(expected);
    }

    [Theory]
    [InlineData("", "1", "templateId")]
    [InlineData(" ", "1", "templateId")]
    [InlineData("template", "", "templateVersion")]
    [InlineData("template", " ", "templateVersion")]
    public void Template_validation_remains_at_the_application_boundary(string templateId, string version, string expectedParameter)
    {
        // Arrange
        Action action = () => DocumentCommandIdempotency.CreateRequestFingerprint(templateId, version);

        // Act
        var exception = action.ShouldThrow<ArgumentException>();

        // Assert
        exception.ParamName.ShouldBe(expectedParameter);
    }
}
