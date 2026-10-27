namespace ViajantesTurismo.Management.WebTests.Components.Pages.Documents;

/// <summary>Test data factory for document details page tests.</summary>
internal static class DocumentDetailsTestData
{
    /// <summary>Builds a document DTO in the given status with one editable field unless fields are supplied; finalized-like statuses get a finalized time.</summary>
    public static GetDocumentDto Create(
        DocumentStatusDto status,
        bool hasFinalizedArtifact = false,
        IReadOnlyList<GetDocumentFieldDto>? fields = null) => new()
        {
            Id = Guid.CreateVersion7(),
            BookingId = Guid.CreateVersion7(),
            Revision = 1,
            TemplateId = "tour-service-contract",
            TemplateVersion = "1",
            SourceVersion = "SOURCE-VERSION",
            Status = status,
            Fields = fields ??
        [
            new GetDocumentFieldDto
            {
                FieldId = "greeting",
                Label = "Greeting",
                RenderedValue = "Dear customer",
                IsEditable = true
            }
        ],
            CreatedAt = TimeProvider.System.GetUtcNow().UtcDateTime,
            UpdatedAt = TimeProvider.System.GetUtcNow().UtcDateTime,
            FinalizedAt = status is DocumentStatusDto.Finalized
            or DocumentStatusDto.Superseded
            or DocumentStatusDto.Voided
                ? TimeProvider.System.GetUtcNow().UtcDateTime
                : null,
            HasFinalizedArtifact = hasFinalizedArtifact
        };
}
