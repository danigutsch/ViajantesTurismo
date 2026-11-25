using SharedKernel.DispositionHold;
using SharedKernel.Results;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.UnitTests.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Application.Documents.Holds;

[Trait(TestTraitNames.CapabilityName, Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
public sealed class DocumentHoldAuditTests
{
    [Fact]
    public async Task Place_adds_exactly_one_succeeded_audit_record_saved_with_the_hold()
    {
        // Arrange
        var harness = DocumentHoldHarness.Create();

        // Act
        var result = await harness.Place.Handle(harness.CreatePlaceCommand(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var record = harness.AuditStore.Records.ShouldHaveSingleItem();
        record.Operation.ShouldBe(DocumentAuditOperation.PlaceHold);
        record.Outcome.ShouldBe(DocumentAuditOutcome.Succeeded);
        record.ReasonCode.ShouldBe(DocumentAuditReasonCode.ManualHoldPlacement);
        record.ActorId.ShouldBe(DocumentHoldHarness.ActorId);
        record.CorrelationId.ShouldBe(DocumentHoldHarness.CorrelationId);
        record.DocumentId.ShouldBe(harness.DocumentId);
        record.HoldId.ShouldBe(result.Value.Id);
        record.HoldBasis.ShouldBe(nameof(HoldBasis.LegalClaim));
        record.BookingId.ShouldBeNull();
        record.DocumentRevision.ShouldBeNull();
        harness.UnitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Release_adds_exactly_one_succeeded_audit_record_with_the_hold_basis()
    {
        // Arrange
        var harness = DocumentHoldHarness.Create();
        var placed = await harness.Place.Handle(harness.CreatePlaceCommand(), TestContext.Current.CancellationToken);
        harness.AuditStore.Records.Clear();

        // Act
        var result = await harness.Release.Handle(harness.CreateReleaseCommand(placed.Value.Id, "actor-0002"), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var record = harness.AuditStore.Records.ShouldHaveSingleItem();
        record.Operation.ShouldBe(DocumentAuditOperation.ReleaseHold);
        record.Outcome.ShouldBe(DocumentAuditOutcome.Succeeded);
        record.ReasonCode.ShouldBe(DocumentAuditReasonCode.ManualHoldRelease);
        record.ActorId.ShouldBe("actor-0002");
        record.HoldId.ShouldBe(placed.Value.Id);
        record.HoldBasis.ShouldBe(nameof(HoldBasis.LegalClaim));
    }

    [Fact]
    public async Task Place_of_an_unknown_document_defers_a_rejection_and_stores_no_hold()
    {
        // Arrange
        var harness = DocumentHoldHarness.Create();
        var unknown = Guid.CreateVersion7();
        var command = harness.CreatePlaceCommand() with
        {
            DocumentId = unknown
        };

        // Act
        var result = await harness.Place.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ResultStatus.NotFound);
        harness.Store.Holds.ShouldBeEmpty();
        var record = command.TakeRejectionAudit().ShouldNotBeNull();
        record.ReasonCode.ShouldBe(DocumentAuditReasonCode.DocumentNotFound);
        record.DocumentId.ShouldBeNull();
        record.DocumentLineageId.ShouldBeNull();
        harness.AuditStore.Records.ShouldBeEmpty();
    }

    [Fact]
    public async Task Place_with_an_invalid_field_defers_validation_evidence_and_stores_no_hold()
    {
        // Arrange
        var harness = DocumentHoldHarness.Create();
        var command = harness.CreatePlaceCommand("jane doe@example.invalid");

        // Act
        var result = await harness.Place.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ResultStatus.Invalid);
        harness.Store.Holds.ShouldBeEmpty();
        var record = command.TakeRejectionAudit().ShouldNotBeNull();
        record.ReasonCode.ShouldBe(DocumentAuditReasonCode.ValidationRejected);
        record.DocumentId.ShouldBe(command.DocumentId);
        harness.AuditStore.Records.ShouldBeEmpty();
    }

    [Fact]
    public async Task Place_with_an_unknown_basis_defers_a_rejection_without_the_invalid_basis()
    {
        // Arrange
        var harness = DocumentHoldHarness.Create();
        var command = harness.CreatePlaceCommand() with
        {
            Basis = (HoldBasis)99
        };

        // Act
        var result = await harness.Place.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ResultStatus.Invalid);
        harness.Store.Holds.ShouldBeEmpty();
        var rejection = command.TakeRejectionAudit().ShouldNotBeNull();
        rejection.ReasonCode.ShouldBe(DocumentAuditReasonCode.ValidationRejected);
        rejection.HoldBasis.ShouldBeNull();
    }

    [Fact]
    public async Task A_second_release_records_a_state_conflict_rejection_and_keeps_the_first_release()
    {
        // Arrange
        var harness = DocumentHoldHarness.Create();
        var placed = await harness.Place.Handle(harness.CreatePlaceCommand(), TestContext.Current.CancellationToken);
        _ = await harness.Release.Handle(harness.CreateReleaseCommand(placed.Value.Id, "actor-0002"), TestContext.Current.CancellationToken);
        harness.AuditStore.Records.Clear();
        var command = harness.CreateReleaseCommand(placed.Value.Id, "actor-0003");

        // Act
        var result = await harness.Release.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ResultStatus.Conflict);
        harness.Store.Holds.ShouldHaveSingleItem().ReleasedBy.ShouldBe("actor-0002");
        var record = command.TakeRejectionAudit().ShouldNotBeNull();
        record.ReasonCode.ShouldBe(DocumentAuditReasonCode.StateConflict);
        record.DocumentId.ShouldBe(harness.DocumentId);
        record.DocumentLineageId.ShouldBe(harness.LineageId);
        record.HoldId.ShouldBe(placed.Value.Id);
        harness.AuditStore.Records.ShouldBeEmpty();
    }

    [Fact]
    public async Task Release_of_an_unknown_hold_records_a_hold_not_found_rejection()
    {
        // Arrange
        var harness = DocumentHoldHarness.Create();
        var unknownHold = Guid.CreateVersion7();
        var command = harness.CreateReleaseCommand(unknownHold);

        // Act
        var result = await harness.Release.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ResultStatus.NotFound);
        var record = command.TakeRejectionAudit().ShouldNotBeNull();
        record.ReasonCode.ShouldBe(DocumentAuditReasonCode.HoldNotFound);
        record.HoldId.ShouldBe(unknownHold);
        record.HoldBasis.ShouldBeNull();
        harness.AuditStore.Records.ShouldBeEmpty();
    }

    [Fact]
    public async Task Release_of_an_unknown_document_records_a_document_not_found_rejection()
    {
        // Arrange
        var harness = DocumentHoldHarness.Create();
        var command = harness.CreateReleaseCommand(Guid.CreateVersion7()) with
        {
            DocumentId = Guid.CreateVersion7()
        };

        // Act
        var result = await harness.Release.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ResultStatus.NotFound);
        var record = command.TakeRejectionAudit().ShouldNotBeNull();
        record.ReasonCode.ShouldBe(DocumentAuditReasonCode.DocumentNotFound);
        record.DocumentId.ShouldBeNull();
        record.DocumentLineageId.ShouldBeNull();
        record.HoldId.ShouldBe(command.HoldId);
        harness.AuditStore.Records.ShouldBeEmpty();
    }

    [Fact]
    public async Task Audit_records_carry_no_free_text_or_hold_codes()
    {
        // Arrange
        var harness = DocumentHoldHarness.Create();
        var command = harness.CreatePlaceCommand("purpose.jane-doe-marker") with
        {
            AuthorityReference = "authority:jane-doe-marker"
        };
        var placed = await harness.Place.Handle(command, TestContext.Current.CancellationToken);
        var release = harness.CreateReleaseCommand(placed.Value.Id) with
        {
            ReasonCode = "reason.jane-doe-marker"
        };

        // Act
        _ = await harness.Release.Handle(release, TestContext.Current.CancellationToken);

        // Assert
        harness.AuditStore.Records.Count.ShouldBe(2);
        foreach (var record in harness.AuditStore.Records)
        {
            var rendered = string.Join(
                ' ',
                typeof(DocumentAuditRecord).GetProperties().Select(property => property.GetValue(record)?.ToString()));
            rendered.ShouldNotContain("marker", StringComparison.Ordinal);
        }
    }
}
