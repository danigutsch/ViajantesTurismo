using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql;
using ViajantesTurismo.Management.Security;

namespace ViajantesTurismo.Admin.UnitTests.ManagementSecurity;

internal static class ManagementSecurityPersistenceRegistrationTestContext
{
    public static bool HasUnkeyedNpgsqlDataSource()
    {
        var services = new ServiceCollection();
        services.AddManagementSecurityPersistence("Host=localhost;Database=security;Username=security;Password=test-only");

        return services.Any(descriptor => descriptor.ServiceType == typeof(NpgsqlDataSource));
    }

    public static bool IsSensitiveDataLoggingEnabled()
    {
        var services = new ServiceCollection();
        services.AddManagementSecurityPersistence("Host=localhost;Database=security;Username=security;Password=test-only");
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<Microsoft.EntityFrameworkCore.DbContextOptions<ManagementSecurityDbContext>>();
        return options.FindExtension<CoreOptionsExtension>()?.IsSensitiveDataLoggingEnabled ?? false;
    }
}
