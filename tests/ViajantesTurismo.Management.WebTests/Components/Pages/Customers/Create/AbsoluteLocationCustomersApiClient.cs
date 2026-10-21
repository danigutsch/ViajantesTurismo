using System.Net;
using PandaBear.HttpClients;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers.Create;

internal sealed class AbsoluteLocationCustomersApiClient : ICustomersApiClient
{
    public Task<HttpOutcome<GetCustomerDto[]>> GetCustomers(CancellationToken ct, int maxItems = 100) => throw new NotImplementedException();

    public Task<HttpOutcome<CustomerDetailsDto>> GetCustomerById(Guid id, CancellationToken ct) => throw new NotImplementedException();

    public Task<HttpOutcome> CreateCustomer(CreateCustomerDto dto, CancellationToken ct) =>
        Task.FromResult(new HttpOutcome
        {
            Kind = HttpOutcomeKind.Succeeded,
            StatusCode = HttpStatusCode.Created,
            Location = new Uri("https://example.test/customers/absolute-id?source=review", UriKind.Absolute)
        });

    public Task<HttpOutcome> UpdateCustomer(Guid id, UpdateCustomerDto dto, CancellationToken ct) => throw new NotImplementedException();

    public Task<HttpOutcome<ImportResultDto>> ImportCustomers(byte[] fileContent, string fileName, IProgress<long>? progress, CancellationToken ct) => throw new NotImplementedException();

    public Task<HttpOutcome<ImportResultDto>> CommitImportWithResolutions(
        byte[] fileContent,
        string fileName,
        IReadOnlyDictionary<string, string> conflictResolutions,
        IProgress<long>? progress,
        CancellationToken ct) => throw new NotImplementedException();

    /// <inheritdoc />
    public Task<HttpOutcome<ImportResultDto>> ImportCustomers(byte[] fileContent, string fileName, CancellationToken ct)
    {
        return ImportCustomers(fileContent, fileName, null, ct);
    }

    /// <inheritdoc />
    public Task<HttpOutcome<ImportResultDto>> CommitImportWithResolutions(byte[] fileContent, string fileName, IReadOnlyDictionary<string, string> conflictResolutions, CancellationToken ct)
    {
        return CommitImportWithResolutions(fileContent, fileName, conflictResolutions, null, ct);
    }
}
