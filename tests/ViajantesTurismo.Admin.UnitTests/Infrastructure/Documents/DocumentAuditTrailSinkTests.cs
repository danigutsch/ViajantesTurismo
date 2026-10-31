using PandaBear.EntityFrameworkCore;
using ViajantesTurismo.Admin.Infrastructure.Documents;
using ViajantesTurismo.Admin.UnitTests.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure.Documents;

public sealed class DocumentAuditTrailSinkTests
{
    [Fact]
    public async Task Append_stamps_the_record_with_the_lineage_of_its_tracked_document()
    {
        // Arrange
        await using var scope = DocumentAuditTrailSinkTestData.CreateContext();
        var dbContext = scope.DbContext;
        var lineage = DocumentLineageTestData.Create();
        var draft = lineage.Revisions[0];
        dbContext.DocumentLineages.Add(lineage);
        var record = DocumentAuditTrailSinkTestData.CreateRecord(draft.Id);
        using var ambient = CurrentSaveChangesDbContext.Enter(dbContext);

        // Act
        await new DocumentAuditTrailSink().Append(record, CancellationToken.None);

        // Assert
        dbContext.Entry(record).Property(DocumentAuditLineage.PropertyName).CurrentValue
            .ShouldBe(draft.DocumentLineageId);
    }

    [Fact]
    public async Task Append_stamps_the_no_lineage_marker_when_the_document_does_not_exist()
    {
        // Arrange
        await using var scope = DocumentAuditTrailSinkTestData.CreateContext();
        var dbContext = scope.DbContext;
        var record = DocumentAuditTrailSinkTestData.CreateRecord(Guid.CreateVersion7());
        using var ambient = CurrentSaveChangesDbContext.Enter(dbContext);

        // Act
        await new DocumentAuditTrailSink().Append(record, CancellationToken.None);

        // Assert
        dbContext.Entry(record).Property(DocumentAuditLineage.PropertyName).CurrentValue
            .ShouldBe(DocumentAuditLineage.NoLineage);
    }

    [Fact]
    public async Task Append_throws_when_no_save_is_in_progress()
    {
        // Arrange
        var record = DocumentAuditTrailSinkTestData.CreateRecord(Guid.CreateVersion7());
        Func<Task> append = async () => await new DocumentAuditTrailSink().Append(record, CancellationToken.None);

        // Act
        var exception = await append.ShouldThrow<InvalidOperationException>();

        // Assert
        exception.Message.ShouldContain("current AdminWriteDbContext SaveChanges", StringComparison.Ordinal);
    }
}
