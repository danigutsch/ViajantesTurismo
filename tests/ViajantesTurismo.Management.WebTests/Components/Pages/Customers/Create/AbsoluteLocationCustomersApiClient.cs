namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers.Create;

internal sealed class AbsoluteLocationCustomersApiClient : ICustomersApiClient
{
    public Task<IReadOnlyList<GetCustomerDto>> GetCustomers(CancellationToken cancellationToken, int maxItems = 100) => throw new NotImplementedException();

    public Task<CustomerDetailsDto?> GetCustomerById(Guid id, CancellationToken cancellationToken) => throw new NotImplementedException();

    public Task<Uri> CreateCustomer(CreateCustomerDto dto, CancellationToken cancellationToken) =>
        Task.FromResult(new Uri("https://example.test/customers/absolute-id?source=review", UriKind.Absolute));

    public Task UpdateCustomer(Guid id, UpdateCustomerDto dto, CancellationToken cancellationToken) => throw new NotImplementedException();

    public Task<ImportResultDto> ImportCustomers(byte[] fileContent, string fileName, CancellationToken cancellationToken) => throw new NotImplementedException();

    public Task<ImportResultDto> CommitImportWithResolutions(byte[] fileContent, string fileName, IReadOnlyDictionary<string, string> conflictResolutions, CancellationToken cancellationToken) => throw new NotImplementedException();
}
