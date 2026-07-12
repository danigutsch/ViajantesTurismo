using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ViajantesTurismo.Management.Security;

/// <summary>
/// Creates the Management security context for Entity Framework tooling.
/// </summary>
public sealed class ManagementSecurityDbContextFactory : IDesignTimeDbContextFactory<ManagementSecurityDbContext>
{
    /// <inheritdoc />
    public ManagementSecurityDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ManagementSecurityDbContext>()
            .UseNpgsql("Host=localhost;Database=management_security_design;Username=postgres")
            .Options;
        return new ManagementSecurityDbContext(options);
    }
}
