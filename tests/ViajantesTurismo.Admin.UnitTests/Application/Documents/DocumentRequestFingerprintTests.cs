using SharedKernel.Idempotency;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Admin.UnitTests.Documents;

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
        var actual = IdempotencyRequestFingerprint.Create(templateId, version);

        // Assert
        actual.ShouldBe(expected);
    }

    [Theory]
    [InlineData("", "1", "templateId")]
    [InlineData(" ", "1", "templateId")]
    [InlineData("template", "", "templateVersion")]
    [InlineData("template", " ", "templateVersion")]
    public async Task Template_validation_remains_at_the_application_boundary(string templateId, string version, string expectedParameter)
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var auditStore = new FakeDocumentAuditStore();
        var coordinator = new DocumentCommandIdempotency(new UnusedIdempotencyStore(), unitOfWork, TimeProvider.System);
        var command = new GenerateContractDraftCommand(
            Guid.CreateVersion7(), templateId, version, DocumentAuditTestData.CreateContext(),
            IdempotencyKey.From(Guid.CreateVersion7().ToString("N")));
        Func<Task> action = async () => await coordinator.Execute(
            command, DocumentAuditTestData.CreateWriter(auditStore, unitOfWork),
            _ => throw new InvalidOperationException("Template validation must precede source preparation."),
            CancellationToken.None);

        // Act
        var exception = await action.ShouldThrow<ArgumentException>();

        // Assert
        exception.ParamName.ShouldBe(expectedParameter);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
        auditStore.Records.ShouldBeEmpty();
    }
}
