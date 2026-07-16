using Microsoft.Extensions.DependencyInjection;
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
}
