using Microsoft.Extensions.DependencyInjection;
using PandaBear.DispositionHold.EntityFrameworkCore;

namespace ViajantesTurismo.Admin.Infrastructure.DispositionHolds;

/// <summary>Registers the disposition hold store and guard for the Admin write context.</summary>
internal static class DispositionHoldRegistration
{
    /// <summary>Adds the hold table, store and guard to <see cref="AdminWriteDbContext" /> in the named schema.</summary>
    public static IServiceCollection AddAdminDispositionHolds(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddDispositionHolds<AdminWriteDbContext>(storage =>
        {
            storage.Schema = DispositionHoldSchema.Name;
            storage.TableName = DispositionHoldSchema.HoldsTable;
        });
    }
}
