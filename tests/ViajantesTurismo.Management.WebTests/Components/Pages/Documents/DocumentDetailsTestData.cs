namespace ViajantesTurismo.Management.WebTests.Components.Pages.Documents;

internal static class DocumentDetailsTestData
{
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
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            FinalizedAt = status is DocumentStatusDto.Finalized
            or DocumentStatusDto.Superseded
            or DocumentStatusDto.Voided
                ? DateTime.UtcNow
                : null,
            HasFinalizedArtifact = hasFinalizedArtifact
        };
}
