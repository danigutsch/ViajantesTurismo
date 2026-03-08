using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.Tests.Shared;

public sealed class FakeCustomersApiClient : ICustomersApiClient
{
    private readonly List<CustomerDetailsDto> _customerDetails = [];
    private readonly List<GetCustomerDto> _customers = [];
    private Exception? _getCustomerByIdException;
    private Exception? _getCustomersException;
    private ImportResultDto? _importResult;
    private Exception? _importCustomersException;

    public Task<IReadOnlyList<GetCustomerDto>> GetCustomers(CancellationToken cancellationToken, int maxItems = 100)
    {
        if (_getCustomersException is not null)
        {
            throw _getCustomersException;
        }

        return Task.FromResult<IReadOnlyList<GetCustomerDto>>([.. _customers.Take(maxItems)]);
    }

    public Task<CustomerDetailsDto?> GetCustomerById(Guid id, CancellationToken cancellationToken)
    {
        if (_getCustomerByIdException is not null)
        {
            throw _getCustomerByIdException;
        }

        return Task.FromResult(_customerDetails.FirstOrDefault(c => c.Id == id));
    }

    public Task<Uri> CreateCustomer(CreateCustomerDto dto, CancellationToken cancellationToken)
    {
        var customerId = Guid.NewGuid();
        return Task.FromResult(new Uri($"/customers/{customerId}", UriKind.Relative));
    }

    public Task UpdateCustomer(Guid id, UpdateCustomerDto dto, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task<ImportResultDto> ImportCustomers(byte[] fileContent, string fileName, CancellationToken cancellationToken)
    {
        if (_importCustomersException is not null)
        {
            throw _importCustomersException;
        }

        if (_importResult is not null)
        {
            return Task.FromResult(_importResult);
        }

        throw new NotImplementedException();
    }

    public void AddCustomer(GetCustomerDto customer) => _customers.Add(customer);

    public void AddCustomerDetails(CustomerDetailsDto customer) => _customerDetails.Add(customer);

    public void SetGetCustomersException(Exception exception) => _getCustomersException = exception;

    public void SetGetCustomerByIdException(Exception exception) => _getCustomerByIdException = exception;

    public void SetImportCustomersResult(ImportResultDto result) => _importResult = result;

    public void SetImportCustomersException(Exception exception) => _importCustomersException = exception;
}
