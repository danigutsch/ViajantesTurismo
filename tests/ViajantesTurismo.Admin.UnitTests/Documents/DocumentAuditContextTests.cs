using SharedKernel.Testing;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Documents;

[Trait(SharedKernelTestTraitNames.CapabilityName, Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
public sealed class DocumentAuditContextTests
{
    [Fact]
    public void Create_accepts_values_at_the_audit_limits()
    {
        // Arrange
        var actorId = new string('a', DocumentAuditLimits.MaxActorIdLength);
        var correlationId = new string('c', DocumentAuditLimits.MaxCorrelationIdLength);

        // Act
        var result = DocumentAuditContext.Create(actorId, correlationId);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ActorId.ShouldBe(actorId);
        result.Value.CorrelationId.ShouldBe(correlationId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_a_blank_actor_identifier(string actorId)
    {
        // Arrange
        const string correlationId = "9a3ca841b4354928861c660a6e4e1b99";

        // Act
        var result = DocumentAuditContext.Create(actorId, correlationId);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Create_rejects_an_actor_identifier_that_exceeds_the_audit_limit()
    {
        // Arrange
        var actorId = new string('a', DocumentAuditLimits.MaxActorIdLength + 1);

        // Act
        var result = DocumentAuditContext.Create(actorId, "9a3ca841b4354928861c660a6e4e1b99");

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_a_blank_correlation_identifier(string correlationId)
    {
        // Arrange
        const string actorId = "9c5ff2e6-8b35-4f78-9df3-ef15af8e92a4";

        // Act
        var result = DocumentAuditContext.Create(actorId, correlationId);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Create_rejects_a_correlation_identifier_that_exceeds_the_audit_limit()
    {
        // Arrange
        var correlationId = new string('c', DocumentAuditLimits.MaxCorrelationIdLength + 1);

        // Act
        var result = DocumentAuditContext.Create("9c5ff2e6-8b35-4f78-9df3-ef15af8e92a4", correlationId);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }
}
