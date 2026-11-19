using System.Reflection;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ViajantesTurismo.Branding.Infrastructure;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.Management.Security;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>Locates the EF Core types that supply architecture documentation facts.</summary>
internal static class ArchitectureMetadataSources
{
    private static readonly Type[] Types =
    [
        .. typeof(Admin.Infrastructure.InfrastructureDependencyInjection).Assembly.GetTypes(),
        .. typeof(BrandingDbContext).Assembly.GetTypes(),
        .. typeof(CatalogDbContext).Assembly.GetTypes(),
        .. typeof(ManagementSecurityDbContext).Assembly.GetTypes(),
    ];

    /// <summary>Gets the concrete application context types.</summary>
    internal static IEnumerable<Type> DbContexts() =>
        Types.Where(type => !type.IsAbstract && typeof(DbContext).IsAssignableFrom(type));

    /// <summary>Finds the checked-in EF model snapshot for the named context.</summary>
    internal static ModelSnapshot Snapshot(string contextName)
    {
        var type = Types.ShouldHaveSingleItem(type => typeof(ModelSnapshot).IsAssignableFrom(type)
            && type.GetCustomAttribute<DbContextAttribute>()?.ContextType.Name == contextName);
        return ((ModelSnapshot?)Activator.CreateInstance(type, nonPublic: true)).ShouldNotBeNull();
    }
}
