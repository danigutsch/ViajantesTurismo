using Microsoft.EntityFrameworkCore;

namespace ViajantesTurismo.Admin.Infrastructure;

internal sealed class DomainEventDispatchDbContextModule(
    DispatchDomainEventsSaveChangesInterceptor interceptor) : IAdminWriteDbContextModule
{
    public void Configure(DbContextOptionsBuilder optionsBuilder)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);

        optionsBuilder.AddInterceptors(interceptor);
    }

    public void Configure(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
    }
}
