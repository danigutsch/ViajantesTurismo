using System.Net;
using SharedKernel.HttpClients;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers.Create;

/// <summary>Customers API client stub whose create call returns a Created outcome with an absolute Location URI.</summary>
internal sealed class AbsoluteLocationCustomersApiClient : ICustomersApiClient
{
    /// <inheritdoc />
    public Task<HttpOutcome<GetCustomerDto[]>> GetCustomers(CancellationToken ct, int maxItems = 100) => throw new NotImplementedException();

    /// <inheritdoc />
    public Task<HttpOutcome<GetCustomerPageDto>> GetCustomerPage(int page, CustomerPageSort sort, bool descending, CancellationToken ct) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public Task<HttpOutcome<GetCustomerOptionsDto>> SearchCustomerOptions(CustomerOptionSearchDto request, CancellationToken ct) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public Task<HttpOutcome<Guid?[]>> FindCustomerImportConflictIds(string[] emails, CancellationToken ct) => throw new NotImplementedException();

    /// <inheritdoc />
    public Task<HttpOutcome<CustomerDetailsDto>> GetCustomerById(Guid id, CancellationToken ct) => throw new NotImplementedException();

    /// <inheritdoc />
    public Task<HttpOutcome> CreateCustomer(CreateCustomerDto dto, CancellationToken ct) =>
        Task.FromResult(new HttpOutcome
        {
            Kind = HttpOutcomeKind.Succeeded,
            StatusCode = HttpStatusCode.Created,
            Location = new Uri("https://example.test/customers/absolute-id?source=review", UriKind.Absolute)
        });

    /// <inheritdoc />
    public Task<HttpOutcome> UpdateCustomer(Guid id, UpdateCustomerDto dto, CancellationToken ct) => throw new NotImplementedException();

    /// <inheritdoc />
    public Task<HttpOutcome<ImportResultDto>> ImportCustomers(ReadOnlyMemory<byte> fileContent, string fileName, IProgress<long>? progress, CancellationToken ct) => throw new NotImplementedException();

    /// <inheritdoc />
    public Task<HttpOutcome<ImportResultDto>> CommitImportWithResolutions(
        ReadOnlyMemory<byte> fileContent,
        string fileName,
        IReadOnlyDictionary<string, string> conflictResolutions,
        IProgress<long>? progress,
        CancellationToken ct) => throw new NotImplementedException();

    /// <inheritdoc />
    public Task<HttpOutcome<ImportResultDto>> ImportCustomers(ReadOnlyMemory<byte> fileContent, string fileName, CancellationToken ct)
    {
        return ImportCustomers(fileContent, fileName, null, ct);
    }

    /// <inheritdoc />
    public Task<HttpOutcome<ImportResultDto>> CommitImportWithResolutions(ReadOnlyMemory<byte> fileContent, string fileName, IReadOnlyDictionary<string, string> conflictResolutions, CancellationToken ct)
    {
        return CommitImportWithResolutions(fileContent, fileName, conflictResolutions, null, ct);
    }
}
