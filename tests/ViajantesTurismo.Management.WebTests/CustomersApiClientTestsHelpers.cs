using Microsoft.Extensions.Logging.Abstractions;

namespace ViajantesTurismo.Management.WebTests;

internal static class CustomersApiClientTestsHelpers
{
    public static ICustomersApiClient CreateSut(HttpClient httpClient)
    {
        return new CustomersApiClient(httpClient, NullLogger<CustomersApiClient>.Instance);
    }
}
