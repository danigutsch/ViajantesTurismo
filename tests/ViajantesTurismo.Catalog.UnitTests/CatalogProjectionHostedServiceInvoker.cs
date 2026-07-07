using System.Reflection;
using SharedKernel.Testing.Assertions;
using ViajantesTurismo.Catalog.Infrastructure;

namespace ViajantesTurismo.Catalog.UnitTests;

internal static class CatalogProjectionHostedServiceInvoker
{
    public static async ValueTask<int> ExecuteBatch(CatalogProjectionHostedService service, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(service);

        var method = typeof(CatalogProjectionHostedService).GetMethod("ExecuteBatch", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Catalog projection ExecuteBatch method was not found.");
        var result = method.Invoke(service, [ct]);
        var batch = result.ShouldBeOfType<ValueTask<int>>();

        return await batch;
    }
}
