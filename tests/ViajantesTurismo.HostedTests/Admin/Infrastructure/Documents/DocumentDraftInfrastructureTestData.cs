using System.Globalization;
using ViajantesTurismo.Admin.Testing.Builders;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Documents;

/// <summary>Builds document drafts, lineages and content for infrastructure tests.</summary>
internal static class DocumentDraftInfrastructureTestData
{
    /// <summary>Creates draft content for the tour service contract template at the given template version.</summary>
    public static DocumentDraftContent CreateContent(string templateVersion = "1") => new(
        "tour-service-contract",
        templateVersion,
        Guid.CreateVersion7().ToString("N"),
        [
            DocumentField.Create("booking-reference", "Booking reference", "ABC123", DocumentPrivacyClassification.Operational, false).Value,
            DocumentField.Create("greeting", "Greeting", "Dear customer", DocumentPrivacyClassification.PersonalData, true).Value,
        ],
        Guid.CreateVersion7().ToString("N"),
        "Viajantes Turismo",
        DocumentLogoTestData.DataUri,
        "#000000",
        "#000000",
        "#ffffff",
        "#000000",
        "system-ui, sans-serif",
        "system-ui, sans-serif",
        "Viajantes Turismo");

    /// <summary>Creates a fixed audit context for document operations.</summary>
    public static DocumentAuditContext CreateAuditContext() => DocumentAuditContext.Create(
        "9c5ff2e6-8b35-4f78-9df3-ef15af8e92a4",
        "9a3ca841b4354928861c660a6e4e1b99").Value;

    /// <summary>Creates a lineage holding one revision and no pending domain events.</summary>
    public static DocumentLineage CreateLineage(DateTime createdAt, Guid? bookingId = null)
    {
        var result = DocumentLineage.Create(
            bookingId ?? Guid.CreateVersion7(),
            DocumentType.BookingConfirmationContract,
            DocumentAudience.Customer,
            CreateContent(),
            createdAt,
            CreateAuditContext());
        result.IsSuccess.ShouldBeTrue();
        result.Value.ClearDomainEvents();
        return result.Value;
    }

    /// <summary>Creates a lineage with the given number of revisions, one minute apart, each with a distinct template version.</summary>
    public static DocumentLineage CreateLineageWithRevisions(DateTime firstCreatedAt, int revisionCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(revisionCount, 1);

        var lineage = CreateLineage(firstCreatedAt);
        var previous = lineage.Revisions.ShouldHaveSingleItem();
        var auditContext = CreateAuditContext();

        for (var revision = 2; revision <= revisionCount; revision++)
        {
            var result = lineage.CreateRevision(
                previous.Id,
                CreateContent(revision.ToString(CultureInfo.InvariantCulture)),
                firstCreatedAt.AddMinutes(revision - 1),
                auditContext);
            result.IsSuccess.ShouldBeTrue();
            previous = result.Value;
        }

        lineage.ClearDomainEvents();
        return lineage;
    }

    /// <summary>Creates an unfinalized contract draft with a booking reference and a personal-data greeting field.</summary>
    public static DocumentDraft CreateDraft(DateTime createdAt, Guid? bookingId = null)
    {
        var result = DocumentDraft.Create(
            bookingId ?? Guid.CreateVersion7(),
            DocumentType.BookingConfirmationContract,
            DocumentAudience.Customer,
            "tour-service-contract",
            "1",
            Guid.CreateVersion7().ToString("N"),
            [
                DocumentField.Create("booking-reference", "Booking reference", "ABC123", DocumentPrivacyClassification.Operational, false).Value,
                DocumentField.Create("greeting", "Greeting", "Dear customer", DocumentPrivacyClassification.PersonalData, true).Value,
            ],
            Guid.CreateVersion7().ToString("N"),
            "Viajantes Turismo",
            DocumentLogoTestData.DataUri,
            createdAt);

        result.IsSuccess.ShouldBeTrue();
        return result.Value;
    }

    /// <summary>Creates a draft and moves it through review and approval to finalized.</summary>
    public static DocumentDraft CreateFinalizedDraft(DateTime createdAt, Guid? bookingId = null)
    {
        var document = CreateDraft(createdAt, bookingId);
        document.BeginReview(createdAt).IsSuccess.ShouldBeTrue();
        document.Approve(createdAt).IsSuccess.ShouldBeTrue();
        document.Finalize("artifact"u8.ToArray(), createdAt).IsSuccess.ShouldBeTrue();
        return document;
    }
}
