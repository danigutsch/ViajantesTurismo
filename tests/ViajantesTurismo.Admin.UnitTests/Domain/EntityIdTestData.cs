using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Testing.Behavior;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

/// <summary>Entity factories for identifier tests.</summary>
internal static class EntityIdTestData
{
    /// <summary>Builds a default customer for identifier tests.</summary>
    public static Customer CreateCustomer()
    {
        return EntityBuilders.BuildCustomer(null);
    }

}
