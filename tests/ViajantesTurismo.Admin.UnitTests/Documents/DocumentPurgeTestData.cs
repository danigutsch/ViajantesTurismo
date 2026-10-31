using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.UnitTests.Documents;

/// <summary>Builds purge runners over the in-memory fakes.</summary>
internal static class DocumentPurgeTestData
{
    /// <summary>Creates a runner that records evidence into <paramref name="evidence" />.</summary>
    public static DocumentPurgeRunner CreateRunner(
        FakeDocumentPurgeEvidenceStore evidence,
        TimeProvider clock,
        IUnitOfWork? unitOfWork = null) =>
        new(new FakeDocumentDispositionTransaction(), evidence, unitOfWork ?? new FakeUnitOfWork(), clock);
}
