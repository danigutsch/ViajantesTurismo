using Microsoft.Extensions.Logging;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.UnitTests.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure.Documents;

[TestFocus(TestFocus.CoreBehavior)]
[Trait(TestTraitNames.CapabilityName, Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
public sealed class DocumentDraftRetentionDrainTests
{
    [Fact]
    public async Task Purge_failure_records_metadata_without_private_details()
    {
        // Arrange
        const string privateData = "SYNTHETIC_PII_PURGE_6A71";
        var now = new DateTimeOffset(2026, 7, 11, 0, 0, 0, TimeSpan.Zero);
        var expired = DocumentDraftTestData.Create(now.UtcDateTime.AddDays(-DocumentLimits.DraftRetentionDays));
        await using var fixture = DocumentDraftRetentionDrainFixture.Create(now, expired, new InvalidOperationException(privateData));

        // Act
        Func<Task> purge = () => fixture.RunDrain(TestContext.Current.CancellationToken).AsTask();
        await purge.ShouldThrow<InvalidOperationException>();

        // Assert
        fixture.DocumentStore.Documents.ContainsKey(expired.Id).ShouldBeTrue();
        var (count, tags) = fixture.Metrics.Measurements.ShouldHaveSingleItem();
        count.ShouldBe(1L);
        tags.ShouldBe(
        [
            new KeyValuePair<string, object?>("document.operation", "purge_drafts"),
            new KeyValuePair<string, object?>("document.outcome", "failed"),
        ]);
        var operationLog = fixture.Logs.Records.Where(record => record.EventId.Name == "DocumentOperationCompleted").ShouldHaveSingleItem();
        operationLog.Level.ShouldBe(LogLevel.Error);
        operationLog.Exception.ShouldBeNull();
        operationLog.Message.ShouldNotContain(privateData, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Canceled_purge_records_cancellation_without_an_error()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 7, 11, 0, 0, 0, TimeSpan.Zero);
        var expired = DocumentDraftTestData.Create(now.UtcDateTime.AddDays(-DocumentLimits.DraftRetentionDays));
        using var stopping = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        await using var fixture = DocumentDraftRetentionDrainFixture.Create(now, expired, new OperationCanceledException(stopping.Token));
        await stopping.CancelAsync();

        // Act
        Func<Task> purge = () => fixture.RunBatch(stopping.Token).AsTask();
        await purge.ShouldThrow<OperationCanceledException>();

        // Assert
        fixture.DocumentStore.Documents.ContainsKey(expired.Id).ShouldBeTrue();
        var (count, tags) = fixture.Metrics.Measurements.ShouldHaveSingleItem();
        count.ShouldBe(1L);
        tags.ShouldBe(
        [
            new KeyValuePair<string, object?>("document.operation", "purge_drafts"),
            new KeyValuePair<string, object?>("document.outcome", "canceled"),
        ]);
        var operationLog = fixture.Logs.Records.Where(record => record.EventId.Name == "DocumentOperationCompleted").ShouldHaveSingleItem();
        operationLog.Level.ShouldBe(LogLevel.Information);
        operationLog.Exception.ShouldBeNull();
    }

    [Fact]
    public async Task RunBatch_purges_expired_drafts_through_the_generated_mediator()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 7, 11, 0, 0, 0, TimeSpan.Zero);
        var expired = DocumentDraftTestData.Create(now.UtcDateTime.AddDays(-DocumentLimits.DraftRetentionDays));
        await using var fixture = DocumentDraftRetentionDrainFixture.Create(now, expired);

        // Act
        var removedCount = await fixture.RunBatch(TestContext.Current.CancellationToken);

        // Assert
        removedCount.ShouldBe(1);
        fixture.DocumentStore.Documents.ContainsKey(expired.Id).ShouldBeFalse();
        var (count, tags) = fixture.Metrics.Measurements.ShouldHaveSingleItem();
        count.ShouldBe(1L);
        tags.ShouldBe(
        [
            new KeyValuePair<string, object?>("document.operation", "purge_drafts"),
            new KeyValuePair<string, object?>("document.outcome", "succeeded"),
        ]);
    }

    [Fact]
    public async Task A_single_drain_finishes_after_the_first_empty_batch()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 7, 11, 0, 0, 0, TimeSpan.Zero);
        var expired = DocumentDraftTestData.Create(now.UtcDateTime.AddDays(-DocumentLimits.DraftRetentionDays));
        await using var fixture = DocumentDraftRetentionDrainFixture.Create(now, expired);

        // Act
        await fixture.RunDrain(TestContext.Current.CancellationToken);

        // Assert
        fixture.DocumentStore.Documents.ContainsKey(expired.Id).ShouldBeFalse();
        var completed = fixture.Logs.Records.Count(record => record.EventId.Name == "DocumentOperationCompleted");
        completed.ShouldBe(2);
    }
}
