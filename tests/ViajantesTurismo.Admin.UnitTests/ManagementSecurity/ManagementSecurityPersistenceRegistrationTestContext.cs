using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql;
using ViajantesTurismo.Management.Security;

namespace ViajantesTurismo.Admin.UnitTests.ManagementSecurity;

/// <summary>
/// Inspects the service registrations added by management security persistence for tests.
/// </summary>
internal static class ManagementSecurityPersistenceRegistrationTestContext
{
    /// <summary>
    /// Returns whether registration adds an <see cref="NpgsqlDataSource"/> service descriptor.
    /// </summary>
    public static bool HasUnkeyedNpgsqlDataSource()
    {
        var services = new ServiceCollection();
        services.AddManagementSecurityPersistence("Host=localhost;Database=security;Username=security;Password=test-only");

        return services.Any(descriptor => descriptor.ServiceType == typeof(NpgsqlDataSource));
    }

    /// <summary>
    /// Returns whether the registered security context options enable sensitive data logging.
    /// </summary>
    public static bool IsSensitiveDataLoggingEnabled()
    {
        var services = new ServiceCollection();
        services.AddManagementSecurityPersistence("Host=localhost;Database=security;Username=security;Password=test-only");
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<Microsoft.EntityFrameworkCore.DbContextOptions<ManagementSecurityDbContext>>();
        return options.FindExtension<CoreOptionsExtension>()?.IsSensitiveDataLoggingEnabled ?? false;
    }
}
