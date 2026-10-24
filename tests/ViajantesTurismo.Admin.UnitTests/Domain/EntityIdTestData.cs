using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Testing.Behavior;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

internal static class EntityIdTestData
{
    public static Customer CreateCustomer()
    {
        return EntityBuilders.BuildCustomer(null);
    }

}
