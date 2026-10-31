using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Application.Documents.Holds;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.UnitTests.Documents;

/// <summary>Composes the hold handlers over in-memory fakes with one known synthetic document and lineage.</summary>
internal sealed class DocumentHoldHarness
{
    /// <summary>The synthetic actor id used by default.</summary>
    public const string ActorId = "actor-0001";

    /// <summary>The synthetic correlation id used by default.</summary>
    public const string CorrelationId = "9a3ca841b4354928861c660a6e4e1b99";

    private DocumentHoldHarness(DateTimeOffset now)
    {
        Now = now;
        Clock = new FakeTimeProvider(now);
        Store = new InMemoryDispositionHoldStore();
        Query = new FakeDocumentHoldQuery(Store);
        Query.LineageByDocument[DocumentId] = LineageId;
        UnitOfWork = new FakeUnitOfWork();
        Transaction = new FakeDocumentDispositionTransaction();
        AuditStore = new FakeDocumentAuditStore();
        var auditWriter = new DocumentAuditWriter(AuditStore, UnitOfWork, Clock);
        Place = new PlaceDocumentHoldCommandHandler(Query, Store, Transaction, Clock, auditWriter);
        Release = new ReleaseDocumentHoldCommandHandler(Query, Store, Transaction, Clock, auditWriter);
        List = new ListDocumentHoldsQueryHandler(Query, Clock);
    }

    /// <summary>Gets the synthetic document revision id.</summary>
    public Guid DocumentId { get; } = Guid.CreateVersion7();

    /// <summary>Gets the synthetic lineage id of the document.</summary>
    public Guid LineageId { get; } = Guid.CreateVersion7();

    /// <summary>Gets the fixed clock time.</summary>
    public DateTimeOffset Now
    {
        get;
    }

    /// <summary>Gets the fixed clock.</summary>
    public FakeTimeProvider Clock
    {
        get;
    }

    /// <summary>Gets the in-memory hold store.</summary>
    public InMemoryDispositionHoldStore Store
    {
        get;
    }

    /// <summary>Gets the document audit store fake.</summary>
    public FakeDocumentAuditStore AuditStore
    {
        get;
    }

    /// <summary>Gets the lineage and hold query fake.</summary>
    public FakeDocumentHoldQuery Query
    {
        get;
    }

    /// <summary>Gets the unit of work fake.</summary>
    public FakeUnitOfWork UnitOfWork
    {
        get;
    }

    /// <summary>Gets the transaction fake.</summary>
    public FakeDocumentDispositionTransaction Transaction
    {
        get;
    }

    /// <summary>Gets the place handler.</summary>
    public PlaceDocumentHoldCommandHandler Place
    {
        get;
    }

    /// <summary>Gets the release handler.</summary>
    public ReleaseDocumentHoldCommandHandler Release
    {
        get;
    }

    /// <summary>Gets the list handler.</summary>
    public ListDocumentHoldsQueryHandler List
    {
        get;
    }

    /// <summary>Creates a harness at a fixed time.</summary>
    public static DocumentHoldHarness Create() => new(new DateTimeOffset(2026, 10, 31, 12, 0, 0, TimeSpan.Zero));

    /// <summary>Creates a valid place command for the synthetic document.</summary>
    public PlaceDocumentHoldCommand CreatePlaceCommand(string purposeCode = "purpose.claim-001") => new(
        DocumentId,
        PandaBear.DispositionHold.HoldBasis.LegalClaim,
        purposeCode,
        "authority:case-0001",
        Now.AddDays(90),
        null,
        CreateAuditContext(ActorId));

    /// <summary>Creates a valid release command for a hold on the synthetic document.</summary>
    public ReleaseDocumentHoldCommand CreateReleaseCommand(Guid holdId, string actorId = ActorId) => new(
        DocumentId,
        holdId,
        "reason.case-closed",
        CreateAuditContext(actorId));

    /// <summary>Creates a document audit context for the given actor.</summary>
    public static DocumentAuditContext CreateAuditContext(string actorId) => DocumentAuditContext.Create(actorId, CorrelationId).Value;
}
