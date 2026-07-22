using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Mappings;

/// <summary>Maps generated-document domain objects to Admin-safe read DTOs.</summary>
public static class DocumentMapper
{
    /// <summary>Maps a document revision to its Admin read projection.</summary>
    public static GetDocumentDto MapToGetDocumentDto(DocumentDraft document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return new GetDocumentDto
        {
            Id = document.Id,
            BookingId = document.BookingId,
            Revision = document.Revision,
            TemplateId = document.TemplateId,
            TemplateVersion = document.TemplateVersion,
            SourceVersion = document.SourceVersion,
            Status = MapToDocumentStatusDto(document.Status),
            Fields = [.. document.Fields
                .OrderBy(field => field.SortOrder)
                .Select(field => new GetDocumentFieldDto
                {
                    FieldId = field.FieldId,
                    Label = field.Label,
                    RenderedValue = field.RenderedValue,
                    IsEditable = field.IsEditable
                })],
            CreatedAt = document.CreatedAt,
            UpdatedAt = document.UpdatedAt,
            FinalizedAt = document.FinalizedAt,
            ReplacesDocumentId = document.ReplacesDocumentId,
            HasFinalizedArtifact = document.Status == DocumentStatus.Finalized
                && document.GetFinalizedArtifactContent() is not null
        };
    }

    /// <summary>Maps a document lifecycle status to its public DTO representation.</summary>
    public static DocumentStatusDto MapToDocumentStatusDto(DocumentStatus status)
    {
        return status switch
        {
            DocumentStatus.DraftGenerated => DocumentStatusDto.DraftGenerated,
            DocumentStatus.InReview => DocumentStatusDto.InReview,
            DocumentStatus.ChangesRequested => DocumentStatusDto.ChangesRequested,
            DocumentStatus.Approved => DocumentStatusDto.Approved,
            DocumentStatus.Finalized => DocumentStatusDto.Finalized,
            DocumentStatus.Superseded => DocumentStatusDto.Superseded,
            DocumentStatus.Voided => DocumentStatusDto.Voided,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Invalid document status value.")
        };
    }
}
