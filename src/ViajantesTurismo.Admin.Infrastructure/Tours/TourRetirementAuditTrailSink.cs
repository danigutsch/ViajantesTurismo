using PandaBear.AuditTrail;
using PandaBear.EntityFrameworkCore;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Infrastructure.Tours;

/// <summary>Stages retirement audit evidence in the current Admin save operation.</summary>
internal sealed class TourRetirementAuditTrailSink : IAuditTrailSink<TourRetirementAuditRecord>
{
    /// <inheritdoc />
    public ValueTask Append(TourRetirementAuditRecord entry, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ct.ThrowIfCancellationRequested();

        if (CurrentSaveChangesDbContext.Current is not AdminWriteDbContext dbContext)
        {
            throw new InvalidOperationException("Tour retirement audit entries require the current AdminWriteDbContext SaveChanges operation.");
        }

        dbContext.TourRetirementAuditRecords.Add(entry);
        return ValueTask.CompletedTask;
    }
}
