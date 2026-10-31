using PandaBear.Testing;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Documents;

[Trait(SharedKernelTestTraitNames.CapabilityName, Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
public sealed class DocumentAuditPurgeRecordTests
{
    private static readonly DateTime Now = new(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_keeps_only_metadata_and_never_expires()
    {
        // Act
        var result = DocumentAuditPurgeRecord.Create("system", "corr", 4, 0, Now, DocumentAuditOutcome.Succeeded, Now);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var record = result.Value;
        record.PurgedCount.ShouldBe(4);
        record.RetentionCutoffUtc.ShouldBe(Now);
        record.Outcome.ShouldBe(DocumentAuditOutcome.Succeeded);
        typeof(DocumentAuditPurgeRecord).GetProperties()
            .Select(property => property.Name)
            .Order()
            .ToArray()
            .ShouldBe(["ActorId", "CorrelationId", "HeldCount", "Id", "OccurredAtUtc", "Outcome", "PurgedCount", "RetentionCutoffUtc", "UndecidableCount"]);
    }

    [Theory]
    [InlineData("", "corr", 0)]
    [InlineData("actor", "", 0)]
    [InlineData("actor", "corr", -1)]
    public void Create_rejects_invalid_metadata(string actorId, string correlationId, int purgedCount)
    {
        // Act
        var result = DocumentAuditPurgeRecord.Create(actorId, correlationId, purgedCount, 0, Now, DocumentAuditOutcome.Succeeded, Now);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Create_rejects_non_utc_timestamps()
    {
        // Act
        var result = DocumentAuditPurgeRecord.Create(
            "actor", "corr", 0, 0, DateTime.SpecifyKind(Now, DateTimeKind.Unspecified), DocumentAuditOutcome.Succeeded, Now);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Create_rejects_a_rejected_outcome_that_claims_purged_records()
    {
        // Act
        var result = DocumentAuditPurgeRecord.Create("actor", "corr", 2, 0, Now, DocumentAuditOutcome.Rejected, Now);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }
}
