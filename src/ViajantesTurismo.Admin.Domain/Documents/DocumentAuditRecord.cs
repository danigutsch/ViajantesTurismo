using JetBrains.Annotations;
using SharedKernel.AuditTrail;
using SharedKernel.Domain;
using SharedKernel.Results;

namespace ViajantesTurismo.Admin.Domain.Documents;

/// <summary>Represents immutable metadata for a document operation.</summary>
public sealed class DocumentAuditRecord : IEntity<Guid>, IAuditTrailEntry
{
    private DocumentAuditRecord(
        string actorId,
        Guid? documentId,
        Guid? bookingId,
        int? documentRevision,
        DocumentAuditOperation operation,
        DocumentAuditOutcome outcome,
        DocumentAuditReasonCode reasonCode,
        string correlationId,
        DateTime occurredAtUtc)
    {
        Id = Guid.CreateVersion7();
        ActorId = actorId;
        DocumentId = documentId;
        BookingId = bookingId;
        DocumentRevision = documentRevision;
        Operation = operation;
        Outcome = outcome;
        ReasonCode = reasonCode;
        CorrelationId = correlationId;
        OccurredAtUtc = occurredAtUtc;
        RetentionExpiresAt = occurredAtUtc.AddMonths(DocumentAuditLimits.RetentionMonths);
    }

    /// <summary>DO NOT USE. Required by Entity Framework Core for materialization.</summary>
    [UsedImplicitly]
    private DocumentAuditRecord()
    {
    }

    /// <summary>Gets the opaque audit record identifier.</summary>
    public Guid Id { get; private init; }

    /// <summary>Gets the opaque authenticated actor identifier.</summary>
    public string ActorId { get; private init; } = default!;

    /// <summary>Gets the affected document identifier when available.</summary>
    public Guid? DocumentId { get; private init; }

    /// <summary>Gets the affected booking identifier when available.</summary>
    public Guid? BookingId { get; private init; }

    /// <summary>Gets the document revision when available.</summary>
    public int? DocumentRevision { get; private init; }

    /// <summary>Gets the operation that was requested.</summary>
    public DocumentAuditOperation Operation { get; private init; }

    /// <summary>Gets whether the operation completed or was rejected.</summary>
    public DocumentAuditOutcome Outcome { get; private init; }

    /// <summary>Gets a bounded, non-content reason code.</summary>
    public DocumentAuditReasonCode ReasonCode { get; private init; }

    /// <summary>Gets the server-generated correlation identifier.</summary>
    public string CorrelationId { get; private init; } = default!;

    /// <summary>Gets when the operation occurred in UTC.</summary>
    public DateTime OccurredAtUtc { get; private init; }

    /// <summary>Gets when this audit record becomes eligible for retention purge.</summary>
    public DateTime RetentionExpiresAt { get; private init; }

    /// <summary>Creates an immutable document audit record from approved metadata only.</summary>
    public static Result<DocumentAuditRecord> Create(
        string actorId,
        Guid? documentId,
        Guid? bookingId,
        int? documentRevision,
        DocumentAuditOperation operation,
        DocumentAuditOutcome outcome,
        DocumentAuditReasonCode reasonCode,
        string correlationId,
        DateTime occurredAtUtc)
    {
        if (string.IsNullOrWhiteSpace(actorId) || actorId.Length > DocumentAuditLimits.MaxActorIdLength)
        {
            return DocumentAuditErrors.InvalidActorId().ConvertError<DocumentAuditRecord>();
        }

        if (string.IsNullOrWhiteSpace(correlationId) || correlationId.Length > DocumentAuditLimits.MaxCorrelationIdLength)
        {
            return DocumentAuditErrors.InvalidCorrelationId().ConvertError<DocumentAuditRecord>();
        }

        if (occurredAtUtc.Kind != DateTimeKind.Utc)
        {
            return DocumentAuditErrors.InvalidOccurredAtUtc().ConvertError<DocumentAuditRecord>();
        }

        if (documentId == Guid.Empty || bookingId == Guid.Empty)
        {
            return DocumentAuditErrors.InvalidResourceId().ConvertError<DocumentAuditRecord>();
        }

        if (documentRevision is <= 0)
        {
            return DocumentAuditErrors.InvalidDocumentRevision().ConvertError<DocumentAuditRecord>();
        }

        if (!Enum.IsDefined(operation) ||
            !Enum.IsDefined(outcome) ||
            !Enum.IsDefined(reasonCode) ||
            !IsValidReasonCode(operation, outcome, reasonCode) ||
            !HasValidResourceMetadata(documentId, bookingId, documentRevision, operation, outcome))
        {
            return DocumentAuditErrors.InvalidEvidence().ConvertError<DocumentAuditRecord>();
        }

        return Result.Ok(new DocumentAuditRecord(
            actorId,
            documentId,
            bookingId,
            documentRevision,
            operation,
            outcome,
            reasonCode,
            correlationId,
            occurredAtUtc));
    }

    private static bool IsValidReasonCode(
        DocumentAuditOperation operation,
        DocumentAuditOutcome outcome,
        DocumentAuditReasonCode reasonCode) => outcome switch
        {
            DocumentAuditOutcome.Succeeded => operation switch
            {
                DocumentAuditOperation.Generate or
                    DocumentAuditOperation.BeginReview or
                    DocumentAuditOperation.RequestChanges or
                    DocumentAuditOperation.UpdateField or
                    DocumentAuditOperation.Approve => reasonCode == DocumentAuditReasonCode.ManualOperation,
                DocumentAuditOperation.Finalize => reasonCode == DocumentAuditReasonCode.ManualFinalize,
                DocumentAuditOperation.Regenerate => reasonCode == DocumentAuditReasonCode.ManualRegeneration,
                DocumentAuditOperation.Void => reasonCode == DocumentAuditReasonCode.ManualVoid,
                DocumentAuditOperation.Read or DocumentAuditOperation.Download => reasonCode == DocumentAuditReasonCode.None,
                _ => false,
            },
            DocumentAuditOutcome.Rejected => operation switch
            {
                DocumentAuditOperation.Generate => reasonCode is
                    DocumentAuditReasonCode.BookingNotFound or
                    DocumentAuditReasonCode.StateConflict or
                    DocumentAuditReasonCode.ValidationRejected or
                    DocumentAuditReasonCode.TourNotFound,
                DocumentAuditOperation.Read => reasonCode == DocumentAuditReasonCode.DocumentNotFound,
                DocumentAuditOperation.BeginReview or
                    DocumentAuditOperation.RequestChanges or
                    DocumentAuditOperation.Approve or
                    DocumentAuditOperation.Finalize => reasonCode is
                        DocumentAuditReasonCode.DocumentNotFound or DocumentAuditReasonCode.StateConflict,
                DocumentAuditOperation.Void => reasonCode is
                    DocumentAuditReasonCode.DocumentNotFound or
                    DocumentAuditReasonCode.ValidationRejected or
                    DocumentAuditReasonCode.StateConflict,
                DocumentAuditOperation.UpdateField => reasonCode is
                    DocumentAuditReasonCode.DocumentNotFound or
                    DocumentAuditReasonCode.ValidationRejected or
                    DocumentAuditReasonCode.StateConflict,
                DocumentAuditOperation.Regenerate => reasonCode is
                    DocumentAuditReasonCode.DocumentNotFound or
                    DocumentAuditReasonCode.BookingNotFound or
                    DocumentAuditReasonCode.StateConflict or
                    DocumentAuditReasonCode.ValidationRejected or
                    DocumentAuditReasonCode.TourNotFound,
                DocumentAuditOperation.Download => reasonCode is
                    DocumentAuditReasonCode.DocumentNotFound or DocumentAuditReasonCode.ArtifactUnavailable,
                _ => false,
            },
            _ => false,
        };

    private static bool HasValidResourceMetadata(
        Guid? documentId,
        Guid? bookingId,
        int? documentRevision,
        DocumentAuditOperation operation,
        DocumentAuditOutcome outcome)
    {
        if (outcome == DocumentAuditOutcome.Succeeded)
        {
            return documentId is not null && bookingId is not null && documentRevision is not null;
        }

        if (operation == DocumentAuditOperation.Generate)
        {
            return documentId is null && bookingId is not null && documentRevision is null;
        }

        return documentId is not null &&
            ((bookingId is null && documentRevision is null) ||
             (bookingId is not null && documentRevision is not null));
    }
}
