using System.Net;
using SharedKernel.HttpClients;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers.Create;

internal sealed class AbsoluteLocationCustomersApiClient : ICustomersApiClient
{
    public Task<ContractCommandOutcomeDto<GetCustomerDto[]>> GetCustomers(CancellationToken ct, int maxItems = 100) => throw new NotImplementedException();

    public Task<ContractCommandOutcomeDto<CustomerDetailsDto>> GetCustomerById(Guid id, CancellationToken ct) => throw new NotImplementedException();

    public Task<ContractCommandOutcomeDto> CreateCustomer(CreateCustomerDto dto, CancellationToken ct) =>
        Task.FromResult(new ContractCommandOutcomeDto
        {
            Kind = ContractCommandOutcomeKind.Succeeded,
            StatusCode = HttpStatusCode.Created,
            Location = new Uri("https://example.test/customers/absolute-id?source=review", UriKind.Absolute)
        });

    public Task<ContractCommandOutcomeDto> UpdateCustomer(Guid id, UpdateCustomerDto dto, CancellationToken ct) => throw new NotImplementedException();

    public Task<ContractCommandOutcomeDto<ImportResultDto>> ImportCustomers(byte[] fileContent, string fileName, CancellationToken ct) => throw new NotImplementedException();

    public Task<ContractCommandOutcomeDto<ImportResultDto>> CommitImportWithResolutions(
        byte[] fileContent,
        string fileName,
        IReadOnlyDictionary<string, string> conflictResolutions,
        CancellationToken ct) => throw new NotImplementedException();
}
