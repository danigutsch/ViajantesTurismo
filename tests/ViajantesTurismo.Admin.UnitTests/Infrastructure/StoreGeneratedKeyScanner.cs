using Microsoft.EntityFrameworkCore.Metadata;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

/// <summary>Finds primary keys of an EF Core model that the store or EF generates instead of the caller.</summary>
internal static class StoreGeneratedKeyScanner
{
    /// <summary>Lists <c>Entity.Property</c> for every key property of a non-owned entity type that is generated on add.</summary>
    public static string[] FindGeneratedKeys(IModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return
        [
            .. model.GetEntityTypes()
                .Where(entity => !entity.IsOwned() && entity.FindPrimaryKey() is not null)
                .SelectMany(entity => entity.FindPrimaryKey()!.Properties
                    .Where(property => property.ValueGenerated.HasFlag(ValueGenerated.OnAdd))
                    .Select(property => $"{entity.DisplayName()}.{property.Name}"))
                .Order(StringComparer.Ordinal)
        ];
    }
}
