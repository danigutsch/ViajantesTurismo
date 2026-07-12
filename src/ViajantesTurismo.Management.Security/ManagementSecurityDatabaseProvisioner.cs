using Microsoft.EntityFrameworkCore;

namespace ViajantesTurismo.Management.Security;

/// <summary>
/// Provisions Management security persistence from the migration service.
/// </summary>
public static class ManagementSecurityDatabaseProvisioner
{
    /// <summary>
    /// Applies Data Protection migrations and creates the distributed-ticket cache table.
    /// </summary>
    /// <param name="dbContext">The Management security database context.</param>
    /// <param name="ct">A cancellation token.</param>
    public static async Task Provision(ManagementSecurityDbContext dbContext, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        await dbContext.Database.MigrateAsync(ct).ConfigureAwait(false);
    }
}
