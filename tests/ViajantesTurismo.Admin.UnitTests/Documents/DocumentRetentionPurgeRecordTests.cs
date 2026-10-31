using PandaBear.Testing;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Documents;

[Trait(SharedKernelTestTraitNames.CapabilityName, Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
public sealed class DocumentRetentionPurgeRecordTests
{
    private static readonly DateTime Now = new(2026, 10, 31, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_keeps_only_counts_kind_outcome_and_time()
    {
        // Act
        var result = DocumentRetentionPurgeRecord.Create(
            DocumentRetentionPurgeKind.Drafts, 4, 2, DocumentRetentionPurgeOutcome.Succeeded, Now);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var record = result.Value;
        record.Kind.ShouldBe(DocumentRetentionPurgeKind.Drafts);
        record.PurgedCount.ShouldBe(4);
        record.HeldCount.ShouldBe(2);
        record.Outcome.ShouldBe(DocumentRetentionPurgeOutcome.Succeeded);
        record.OccurredAtUtc.ShouldBe(Now);
        typeof(DocumentRetentionPurgeRecord).GetProperties()
            .Select(property => property.Name)
            .Order()
            .ToArray()
            .ShouldBe(["HeldCount", "Id", "Kind", "OccurredAtUtc", "Outcome", "PurgedCount"]);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    public void Create_rejects_negative_counts(int purged, int held)
    {
        // Act
        var result = DocumentRetentionPurgeRecord.Create(
            DocumentRetentionPurgeKind.IdempotencyEntries, purged, held, DocumentRetentionPurgeOutcome.Succeeded, Now);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.ErrorDetails.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull().Keys.ShouldBe(["purgeEvidence"]);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(0, 1)]
    public void Create_rejects_a_refused_outcome_that_claims_counts(int purged, int held)
    {
        // Act
        var result = DocumentRetentionPurgeRecord.Create(
            DocumentRetentionPurgeKind.Drafts, purged, held, DocumentRetentionPurgeOutcome.Refused, Now);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.ErrorDetails.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull().Keys.ShouldBe(["purgeEvidence"]);
    }

    [Fact]
    public void Create_rejects_a_non_utc_timestamp()
    {
        // Act
        var result = DocumentRetentionPurgeRecord.Create(
            DocumentRetentionPurgeKind.Drafts, 0, 0, DocumentRetentionPurgeOutcome.Succeeded,
            DateTime.SpecifyKind(Now, DateTimeKind.Unspecified));

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.ErrorDetails.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull().Keys.ShouldBe(["occurredAtUtc"]);
    }

    [Fact]
    public void Create_rejects_undefined_enum_values()
    {
        // Act
        var kind = DocumentRetentionPurgeRecord.Create(
            (DocumentRetentionPurgeKind)99, 0, 0, DocumentRetentionPurgeOutcome.Succeeded, Now);
        var outcome = DocumentRetentionPurgeRecord.Create(
            DocumentRetentionPurgeKind.Drafts, 0, 0, (DocumentRetentionPurgeOutcome)99, Now);

        // Assert
        kind.IsFailure.ShouldBeTrue();
        kind.ErrorDetails.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull().Keys.ShouldBe(["purgeEvidence"]);
        outcome.IsFailure.ShouldBeTrue();
        outcome.ErrorDetails.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull().Keys.ShouldBe(["purgeEvidence"]);
    }
}
