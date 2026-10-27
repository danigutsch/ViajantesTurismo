using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using ViajantesTurismo.Admin.Contracts.Http;

namespace ViajantesTurismo.Admin.ContractTests.ApiClients;

/// <summary>
/// Creates the customers API client under test.
/// </summary>
internal static class CustomersApiClientTestsHelpers
{
    /// <summary>
    /// Creates a customers API client over the given HTTP client, using a null logger when none is supplied.
    /// </summary>
    public static ICustomersApiClient CreateSut(HttpClient httpClient, ILogger<CustomersApiClient>? logger = null)
    {
        return new CustomersApiClient(httpClient, logger ?? NullLogger<CustomersApiClient>.Instance);
    }
}
