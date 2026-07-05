using Microsoft.EntityFrameworkCore;
using SharedKernel.EntityFrameworkCore;

namespace SharedKernel.DomainEvents.EntityFrameworkCore;

/// <summary>
/// Adds domain-event dispatch interception to a DbContext.
/// </summary>
/// <typeparam name="TContext">The DbContext type.</typeparam>
internal sealed class DomainEventDispatchDbContextConfiguration<TContext>(
    DispatchDomainEventsSaveChangesInterceptor interceptor) : IDbContextConfiguration<TContext>
    where TContext : DbContext
{
    /// <inheritdoc />
    public void ConfigureOptions(DbContextOptionsBuilder optionsBuilder)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);

        optionsBuilder.AddInterceptors(interceptor);
    }
}
