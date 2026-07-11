using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using ViajantesTurismo.Admin.Contracts.Http;

namespace ViajantesTurismo.Admin.ContractTests.ApiClients;

internal static class CustomersApiClientTestsHelpers
{
    public static ICustomersApiClient CreateSut(HttpClient httpClient, ILogger<CustomersApiClient>? logger = null)
    {
        return new CustomersApiClient(httpClient, logger ?? NullLogger<CustomersApiClient>.Instance);
    }
}
