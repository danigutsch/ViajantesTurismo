using JetBrains.Annotations;
using SharedKernel.Domain;
using SharedKernel.Results;

namespace ViajantesTurismo.Admin.Domain.Documents;

/// <summary>Owns every revision and lifecycle invariant for one booking document lineage.</summary>
public sealed class DocumentLineage : IAggregateRoot<Guid>
{
    private readonly List<IDomainEvent> _domainEvents = [];
    private readonly List<DocumentDraft> _revisions = [];

    private DocumentLineage(
        Guid id,
        Guid bookingId,
        DocumentType type,
        DocumentAudience audience,
        DocumentDraft initialRevision)
    {
        Id = id;
        BookingId = bookingId;
        Type = type;
        Audience = audience;
        HighestRevision = 1;
        _revisions.Add(initialRevision);
    }

    /// <summary>Required by Entity Framework Core for materialization.</summary>
    [UsedImplicitly]
    private DocumentLineage()
    {
    }

    /// <summary>Gets the opaque lineage identifier.</summary>
    public Guid Id { get; private init; }

    /// <summary>Gets the source booking identifier.</summary>
    public Guid BookingId { get; private init; }

    /// <summary>Gets the document type shared by every revision.</summary>
    public DocumentType Type { get; private init; }

    /// <summary>Gets the intended audience shared by every revision.</summary>
    public DocumentAudience Audience { get; private init; }

    /// <summary>Gets the highest revision that has ever been finalized.</summary>
    public int HighestFinalizedRevision { get; private set; }

    /// <summary>Gets the highest revision number ever allocated in this lineage.</summary>
    public int HighestRevision { get; private set; }

    /// <summary>Gets the aggregate concurrency version.</summary>
    public long Version { get; private set; }

    /// <summary>Gets the revisions owned by this lineage.</summary>
    public IReadOnlyList<DocumentDraft> Revisions => _revisions.AsReadOnly();

    /// <inheritdoc />
    public IReadOnlyCollection<IDomainEvent> GetDomainEvents() => _domainEvents.AsReadOnly();

    /// <inheritdoc />
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>Creates a lineage with its initial draft revision.</summary>
    /// <param name="bookingId">The source booking identifier.</param>
    /// <param name="type">The document type.</param>
    /// <param name="audience">The intended document audience.</param>
    /// <param name="content">The initial revision content.</param>
    /// <param name="createdAt">The creation time.</param>
    /// <param name="auditContext">The actor and correlation metadata.</param>
    /// <returns>The created lineage or a validation failure.</returns>
    public static Result<DocumentLineage> Create(
        Guid bookingId,
        DocumentType type,
        DocumentAudience audience,
        DocumentDraftContent content,
        DateTime createdAt,
        DocumentAuditContext auditContext)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (auditContext is null)
        {
            return DocumentAuditErrors.AuditContextRequired().ConvertError<DocumentLineage>();
        }

        if (bookingId == Guid.Empty)
        {
            return DocumentLineageErrors.BookingIdRequired();
        }

        if (!Enum.IsDefined(type))
        {
            return DocumentLineageErrors.InvalidDocumentType();
        }

        var lineageId = Guid.CreateVersion7();
        var revisionResult = DocumentDraft.CreateForLineage(
            lineageId,
            bookingId,
            type,
            audience,
            content,
            createdAt);
        if (revisionResult.IsFailure)
        {
            return revisionResult.ConvertError<DocumentDraft, DocumentLineage>();
        }

        var lineage = new DocumentLineage(
            lineageId,
            bookingId,
            type,
            audience,
            revisionResult.Value);
        lineage.AddSuccessfulAudit(revisionResult.Value, DocumentAuditOperation.Generate, auditContext);
        return Result.Ok(lineage);
    }

    internal static DocumentLineage Restore(IEnumerable<DocumentDraft> revisions)
    {
        ArgumentNullException.ThrowIfNull(revisions);
        var revisionList = revisions.OrderBy(revision => revision.Revision).ToList();
        if (revisionList.Count == 0)
        {
            throw new ArgumentException("At least one document revision is required.", nameof(revisions));
        }

        var first = revisionList[0];
        var lineage = new DocumentLineage(
            first.DocumentLineageId,
            first.BookingId,
            first.Type,
            first.Audience,
            first);
        lineage._revisions.AddRange(revisionList.Skip(1));
        lineage.HighestRevision = revisionList.Max(revision => revision.Revision);
        lineage.HighestFinalizedRevision = revisionList
            .Where(revision => revision.FinalizedAt is not null)
            .Select(revision => revision.Revision)
            .DefaultIfEmpty()
            .Max();
        return lineage;
    }

    /// <summary>Creates the next revision from an existing revision in this lineage.</summary>
    /// <param name="replacesDocumentId">The revision being replaced.</param>
    /// <param name="content">The replacement content.</param>
    /// <param name="createdAt">The creation time.</param>
    /// <param name="auditContext">The actor and correlation metadata.</param>
    /// <returns>The created revision or a validation failure.</returns>
    public Result<DocumentDraft> CreateRevision(
        Guid replacesDocumentId,
        DocumentDraftContent content,
        DateTime createdAt,
        DocumentAuditContext auditContext)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (auditContext is null)
        {
            return DocumentAuditErrors.AuditContextRequired().ConvertError<DocumentDraft>();
        }

        var replaced = GetRevision(replacesDocumentId);
        if (replaced is null)
        {
            return DocumentErrors.DocumentNotFound(replacesDocumentId).ConvertError<DocumentDraft>();
        }

        var revisionResult = replaced.CreateReplacement(content, HighestRevision + 1, createdAt);
        if (revisionResult.IsSuccess)
        {
            _revisions.Add(revisionResult.Value);
            HighestRevision = revisionResult.Value.Revision;
            Version++;
            AddSuccessfulAudit(revisionResult.Value, DocumentAuditOperation.Regenerate, auditContext);
        }

        return revisionResult;
    }

    /// <summary>Checks whether a revision advances finalization history.</summary>
    /// <param name="revision">The revision number.</param>
    /// <returns>A success result when the revision may finalize; otherwise, a conflict.</returns>
    public Result CanFinalizeRevision(int revision) =>
        revision > HighestFinalizedRevision
            ? Result.Ok()
            : DocumentLineageErrors.FinalizedRevisionMustAdvance(revision, HighestFinalizedRevision);

    /// <summary>Advances the monotonic finalization marker.</summary>
    /// <param name="revision">The finalized revision number.</param>
    /// <returns>A success result or a historical-finalization conflict.</returns>
    private Result RecordFinalizedRevision(int revision)
    {
        var result = CanFinalizeRevision(revision);
        if (result.IsSuccess)
        {
            HighestFinalizedRevision = revision;
        }

        return result;
    }

    /// <summary>Gets one revision by its public document identifier.</summary>
    /// <param name="documentId">The document revision identifier.</param>
    /// <returns>The revision, or <see langword="null" /> when absent.</returns>
    public DocumentDraft? GetRevision(Guid documentId) =>
        _revisions.FirstOrDefault(revision => revision.Id == documentId);

    /// <summary>Starts or restarts staff review for one revision.</summary>
    public Result BeginReview(Guid documentId, DateTime now, DocumentAuditContext auditContext) =>
        Apply(
            documentId,
            revision => revision.BeginReview(now),
            DocumentAuditOperation.BeginReview,
            auditContext);

    /// <summary>Records requested changes for one revision.</summary>
    public Result RequestChanges(Guid documentId, DateTime now, DocumentAuditContext auditContext) =>
        Apply(
            documentId,
            revision => revision.RequestChanges(now),
            DocumentAuditOperation.RequestChanges,
            auditContext);

    /// <summary>Updates a staff-editable field in one revision.</summary>
    public Result UpdateField(
        Guid documentId,
        string fieldId,
        string value,
        DateTime now,
        DocumentAuditContext auditContext) =>
        Apply(
            documentId,
            revision => revision.UpdateField(fieldId, value, now),
            DocumentAuditOperation.UpdateField,
            auditContext);

    /// <summary>Approves one revision under active staff review.</summary>
    public Result Approve(Guid documentId, DateTime now, DocumentAuditContext auditContext) =>
        Apply(documentId, revision => revision.Approve(now), DocumentAuditOperation.Approve, auditContext);

    /// <summary>Finalizes one revision while preserving monotonic lineage history.</summary>
    public Result Finalize(
        Guid documentId,
        byte[] artifactContent,
        DateTime now,
        DocumentAuditContext auditContext)
    {
        if (auditContext is null)
        {
            return DocumentAuditErrors.AuditContextRequired();
        }

        var target = GetRevision(documentId);
        if (target is null)
        {
            return DocumentErrors.DocumentNotFound(documentId);
        }

        var historyResult = CanFinalizeRevision(target.Revision);
        if (historyResult.IsFailure)
        {
            return historyResult;
        }

        var result = target.Finalize(artifactContent, now);
        if (result.IsFailure)
        {
            return result;
        }

        foreach (var previous in _revisions.Where(revision =>
                     revision.Revision < target.Revision && revision.Status == DocumentStatus.Finalized))
        {
            var supersedeResult = previous.Supersede(now);
            if (supersedeResult.IsFailure)
            {
                return supersedeResult;
            }
        }

        var recordResult = RecordFinalizedRevision(target.Revision);
        if (recordResult.IsSuccess)
        {
            Version++;
            AddSuccessfulAudit(target, DocumentAuditOperation.Finalize, auditContext);
        }

        return recordResult;
    }

    /// <summary>Voids one finalized revision without erasing finalization history.</summary>
    public Result Void(
        Guid documentId,
        string reason,
        DateTime now,
        DocumentAuditContext auditContext) =>
        Apply(documentId, revision => revision.Void(reason, now), DocumentAuditOperation.Void, auditContext);

    private Result Apply(
        Guid documentId,
        Func<DocumentDraft, Result> operation,
        DocumentAuditOperation auditOperation,
        DocumentAuditContext auditContext)
    {
        if (auditContext is null)
        {
            return DocumentAuditErrors.AuditContextRequired();
        }

        var revision = GetRevision(documentId);
        if (revision is null)
        {
            return DocumentErrors.DocumentNotFound(documentId);
        }

        var result = operation(revision);
        if (result.IsSuccess)
        {
            Version++;
            AddSuccessfulAudit(revision, auditOperation, auditContext);
        }

        return result;
    }

    private void AddSuccessfulAudit(
        DocumentDraft revision,
        DocumentAuditOperation operation,
        DocumentAuditContext auditContext) =>
        _domainEvents.Add(new DocumentLifecycleAuditDomainEvent(
            auditContext.ActorId,
            auditContext.CorrelationId,
            revision.Id,
            BookingId,
            revision.Revision,
            operation));
}
