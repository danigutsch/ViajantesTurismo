using System.Reflection;

namespace ViajantesTurismo.Management.WebTests;

internal static class CustomersApiClientTestsHelpers
{
    public static ICustomersApiClient CreateSut(HttpClient httpClient)
    {
        var clientType = Type.GetType("ViajantesTurismo.Management.Web.CustomersApiClient, ViajantesTurismo.Management.Web", throwOnError: true);

        Assert.NotNull(clientType);
        var instance = Activator.CreateInstance(
            clientType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: [httpClient],
            culture: null);

        return Assert.IsType<ICustomersApiClient>(instance, exactMatch: false);
    }
}
