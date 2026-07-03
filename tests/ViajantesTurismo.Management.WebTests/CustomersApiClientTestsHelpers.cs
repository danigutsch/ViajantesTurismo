using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;

namespace ViajantesTurismo.Management.WebTests;

internal static class CustomersApiClientTestsHelpers
{
    public static ICustomersApiClient CreateSut(HttpClient httpClient, ILogger<CustomersApiClient>? logger = null)
    {
        return new CustomersApiClient(httpClient, logger ?? NullLogger<CustomersApiClient>.Instance);
    }
}
