using SharedKernel.Results;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Documents;

[Trait(TestTraitNames.CapabilityName, Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
public sealed class DocumentLineageTests
{
    [Fact]
    public void EnsureTemplateIsCurrent_reports_missing_stale_and_current_revisions()
    {
        // Arrange
        var lineage = DocumentLineageTestData.Create();
        var current = lineage.Revisions.ShouldHaveSingleItem();
        var stale = DocumentLineage.Create(
            Guid.CreateVersion7(),
            DocumentType.BookingConfirmationContract,
            DocumentAudience.Customer,
            DocumentLineageTestData.CreateContent("0"),
            new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc),
            DocumentAuditTestData.CreateContext()).Value;

        // Act and Assert
        lineage.EnsureTemplateIsCurrent(current.Id).IsSuccess.ShouldBeTrue();
        lineage.EnsureTemplateIsCurrent(Guid.CreateVersion7()).Status.ShouldBe(ResultStatus.NotFound);
        stale.EnsureTemplateIsCurrent(stale.Revisions.ShouldHaveSingleItem().Id).Status.ShouldBe(ResultStatus.Conflict);
    }

    [Fact]
    public void Create_assigns_persistent_identity_and_lineage_key()
    {
        // Arrange
        var bookingId = Guid.CreateVersion7();

        // Act
        var result = DocumentLineage.Create(
            bookingId,
            DocumentType.BookingConfirmationContract,
            DocumentAudience.Customer,
            DocumentLineageTestData.CreateContent(),
            new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc),
            DocumentAuditTestData.CreateContext());

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var lineage = result.Value;
        lineage.Id.ShouldNotBe(Guid.Empty);
        lineage.BookingId.ShouldBe(bookingId);
        lineage.Type.ShouldBe(DocumentType.BookingConfirmationContract);
        lineage.HighestFinalizedRevision.ShouldBe(0);
        lineage.Version.ShouldBe(0);
        lineage.Revisions.ShouldHaveSingleItem().DocumentLineageId.ShouldBe(lineage.Id);
    }

    [Fact]
    public void Create_uses_the_supplied_identity_for_the_lineage_and_its_revision()
    {
        // Arrange
        var lineageId = Guid.CreateVersion7();
        var now = new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var result = DocumentLineage.Create(Guid.CreateVersion7(), DocumentType.BookingConfirmationContract, DocumentAudience.Customer,
            DocumentLineageTestData.CreateContent(), now, DocumentAuditTestData.CreateContext(), lineageId);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(lineageId);
        result.Value.Revisions.ShouldHaveSingleItem().DocumentLineageId.ShouldBe(lineageId);
    }

    [Fact]
    public void Create_rejects_an_explicit_empty_lineage_identity()
    {
        // Arrange
        var bookingId = Guid.CreateVersion7();
        var now = new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var result = DocumentLineage.Create(bookingId, DocumentType.BookingConfirmationContract, DocumentAudience.Customer,
            DocumentLineageTestData.CreateContent(), now, DocumentAuditTestData.CreateContext(), Guid.Empty);

        // Assert
        result.Status.ShouldBe(ResultStatus.Invalid);
    }

    [Fact]
    public void Create_copies_fields_instead_of_attaching_caller_instances()
    {
        // Arrange
        var content = DocumentLineageTestData.CreateContent();

        // Act
        var result = DocumentLineage.Create(
            Guid.CreateVersion7(),
            DocumentType.BookingConfirmationContract,
            DocumentAudience.Customer,
            content,
            new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc),
            DocumentAuditTestData.CreateContext());

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var ownedFields = result.Value.Revisions.ShouldHaveSingleItem().Fields;
        ReferenceEquals(content.Fields[0], ownedFields[0]).ShouldBeFalse();
        ReferenceEquals(content.Fields[1], ownedFields[1]).ShouldBeFalse();
        content.Fields.Select(field => field.SortOrder).ShouldBe([0, 0]);
        ownedFields.Select(field => field.SortOrder).ShouldBe([0, 1]);
    }

    [Fact]
    public void DocumentLineage_exposes_no_public_audit_only_mutator()
    {
        // Arrange
        var publicMethodNames = typeof(DocumentLineage)
            .GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
            .Select(method => method.Name)
            .ToArray();

        // Act
        var exposesAuditOnlyMutation = publicMethodNames.Contains(
            "RecordSuccessfulAudit",
            StringComparer.Ordinal);

        // Assert
        exposesAuditOnlyMutation.ShouldBeFalse();
    }

    [Fact]
    public void Create_records_the_captured_branding_source_revision()
    {
        // Arrange
        var content = DocumentLineageTestData.CreateContent() with
        {
            BrandingSourceRevision = 42
        };

        // Act
        var result = DocumentLineage.Create(
            Guid.CreateVersion7(),
            DocumentType.BookingConfirmationContract,
            DocumentAudience.Customer,
            content,
            new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc),
            DocumentAuditTestData.CreateContext());

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Revisions.ShouldHaveSingleItem().BrandingSourceRevision.ShouldBe(42);
    }

    [Fact]
    public void Create_rejects_an_unspecified_branding_source_revision()
    {
        // Arrange
        var content = DocumentLineageTestData.CreateContent() with
        {
            BrandingSourceRevision = default
        };

        // Act
        var result = DocumentLineage.Create(
            Guid.CreateVersion7(),
            DocumentType.BookingConfirmationContract,
            DocumentAudience.Customer,
            content,
            new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc),
            DocumentAuditTestData.CreateContext());

        // Assert
        result.Status.ShouldBe(ResultStatus.Invalid);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public void Create_rejects_a_non_positive_branding_source_revision(long revision)
    {
        // Arrange
        var content = DocumentLineageTestData.CreateContent() with
        {
            BrandingSourceRevision = revision
        };

        // Act
        var result = DocumentLineage.Create(
            Guid.CreateVersion7(),
            DocumentType.BookingConfirmationContract,
            DocumentAudience.Customer,
            content,
            new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc),
            DocumentAuditTestData.CreateContext());

        // Assert
        result.Status.ShouldBe(ResultStatus.Invalid);
    }

    [Fact]
    public void CreateRevision_records_the_branding_source_revision_of_the_new_capture()
    {
        // Arrange
        var now = new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc);
        var lineage = DocumentLineageTestData.Create(now: now);
        var previous = lineage.Revisions.ShouldHaveSingleItem();

        // Act
        var result = lineage.CreateRevision(
            previous.Id,
            DocumentLineageTestData.CreateContent("2") with
            {
                BrandingSourceRevision = 7
            },
            now.AddMinutes(1),
            DocumentAuditTestData.CreateContext());

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.BrandingSourceRevision.ShouldBe(7);
        previous.BrandingSourceRevision.ShouldBe(1);
    }

    [Fact]
    public void CreateRevision_rejects_an_unspecified_branding_source_revision()
    {
        // Arrange
        var now = new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc);
        var lineage = DocumentLineageTestData.Create(now: now);
        var previous = lineage.Revisions.ShouldHaveSingleItem();
        var content = DocumentLineageTestData.CreateContent("2") with
        {
            BrandingSourceRevision = default
        };

        // Act
        var result = lineage.CreateRevision(
            previous.Id,
            content,
            now.AddMinutes(1),
            DocumentAuditTestData.CreateContext());

        // Assert
        result.Status.ShouldBe(ResultStatus.Invalid);
        lineage.Revisions.ShouldHaveSingleItem().Id.ShouldBe(previous.Id);
    }

    [Fact]
    public void CreateRevision_emits_regenerate_event_and_increments_version_once()
    {
        // Arrange
        var now = new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc);
        var lineage = DocumentLineageTestData.Create(now: now);
        var previous = lineage.Revisions.ShouldHaveSingleItem();
        var auditContext = DocumentAuditTestData.CreateContext();

        // Act
        var result = lineage.CreateRevision(
            previous.Id,
            DocumentLineageTestData.CreateContent("2"),
            now.AddMinutes(1),
            auditContext);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        lineage.HighestRevision.ShouldBe(2);
        lineage.Version.ShouldBe(1);
        var auditEvent = lineage.GetDomainEvents()
            .ShouldHaveSingleItem()
            .ShouldBeOfType<DocumentLifecycleAuditDomainEvent>();
        auditEvent.Operation.ShouldBe(DocumentAuditOperation.Regenerate);
        auditEvent.DocumentId.ShouldBe(result.Value.Id);
        auditEvent.DocumentRevision.ShouldBe(2);
    }

    [Fact]
    public void BeginReview_emits_matching_event_and_increments_version_once()
    {
        // Arrange
        var lineage = DocumentLineageTestData.Create();
        var document = lineage.Revisions.ShouldHaveSingleItem();
        var auditContext = DocumentAuditTestData.CreateContext();

        // Act
        var result = lineage.BeginReview(document.Id, TimeProvider.System.GetUtcNow().UtcDateTime, auditContext);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        lineage.Version.ShouldBe(1);
        var auditEvent = lineage.GetDomainEvents()
            .ShouldHaveSingleItem()
            .ShouldBeOfType<DocumentLifecycleAuditDomainEvent>();
        auditEvent.Operation.ShouldBe(DocumentAuditOperation.BeginReview);
        auditEvent.DocumentId.ShouldBe(document.Id);
    }

    [Fact]
    public void Rejected_lifecycle_operation_emits_no_event_and_does_not_increment_version()
    {
        // Arrange
        var lineage = DocumentLineageTestData.Create();
        var document = lineage.Revisions.ShouldHaveSingleItem();

        // Act
        var result = lineage.Approve(document.Id, TimeProvider.System.GetUtcNow().UtcDateTime, DocumentAuditTestData.CreateContext());

        // Assert
        result.IsFailure.ShouldBeTrue();
        lineage.Version.ShouldBe(0);
        lineage.GetDomainEvents().ShouldBeEmpty();
    }

    [Fact]
    public void CreateRevision_creates_distinct_owned_fields()
    {
        // Arrange
        var lineage = DocumentLineageTestData.Create();
        var previous = lineage.Revisions.ShouldHaveSingleItem();
        var content = DocumentLineageTestData.CreateContent("2");

        // Act
        var result = lineage.CreateRevision(
            previous.Id,
            content,
            TimeProvider.System.GetUtcNow().UtcDateTime,
            DocumentAuditTestData.CreateContext());

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var replacement = result.Value;
        ReferenceEquals(content.Fields[0], replacement.Fields[0]).ShouldBeFalse();
        ReferenceEquals(previous.Fields[0], replacement.Fields[0]).ShouldBeFalse();
        previous.DocumentLineageId.ShouldBe(lineage.Id);
        replacement.DocumentLineageId.ShouldBe(lineage.Id);
    }

    [Fact]
    public void CanFinalizeRevision_rejects_a_revision_that_does_not_advance_history()
    {
        // Arrange
        var now = new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc);
        var lineage = DocumentLineageTestData.Create(now: now);
        var document = lineage.Revisions.ShouldHaveSingleItem();
        var auditContext = DocumentAuditTestData.CreateContext();
        lineage.BeginReview(document.Id, now, auditContext).IsSuccess.ShouldBeTrue();
        lineage.Approve(document.Id, now, auditContext).IsSuccess.ShouldBeTrue();
        lineage.Finalize(document.Id, "artifact"u8.ToArray(), now, auditContext).IsSuccess.ShouldBeTrue();

        // Act
        var result = lineage.CanFinalizeRevision(document.Revision);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Status.ShouldBe(ResultStatus.Conflict);
        lineage.HighestFinalizedRevision.ShouldBe(document.Revision);
    }

    [Fact]
    public void CanFinalizeRevision_does_not_mutate_the_lineage()
    {
        // Arrange
        var lineage = DocumentLineageTestData.Create();

        // Act
        var result = lineage.CanFinalizeRevision(1);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        lineage.HighestFinalizedRevision.ShouldBe(0);
    }
}
