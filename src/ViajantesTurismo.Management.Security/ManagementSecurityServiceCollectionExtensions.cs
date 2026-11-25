using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ViajantesTurismo.Management.Security;

/// <summary>
/// Registers persisted Management security state.
/// </summary>
public static class ManagementSecurityServiceCollectionExtensions
{
    private const string SecurityDataSourceName = "management-security";
    private const string TicketCacheDataSourceName = "management-ticket-cache";

    /// <summary>
    /// Adds Data Protection key persistence and server-side Management ticket storage.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The security database connection string.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddManagementSecurityPersistence(this IServiceCollection services, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<ManagementSecurityDbContext>(options => options.UseNpgsql(connectionString,
            static npgsql => npgsql.ConfigureDataSource(static dataSource => dataSource.Name = SecurityDataSourceName)));
        services.AddDistributedPostgresCache(options =>
        {
            options.ConnectionString = connectionString;
            options.ConfigureDataSourceBuilder = static dataSource => dataSource.Name = TicketCacheDataSourceName;
            options.SchemaName = ManagementSecurityDefaults.SchemaName;
            options.TableName = ManagementSecurityDefaults.TicketTableName;
            options.CreateIfNotExists = false;
            options.UseWAL = true;
        });

        return services;
    }
}
