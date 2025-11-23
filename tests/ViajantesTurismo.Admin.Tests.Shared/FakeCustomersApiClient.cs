using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.Tests.Shared;

public sealed class FakeCustomersApiClient : ICustomersApiClient
{
    private readonly List<CustomerDetailsDto> _customerDetails = [];
    private readonly List<GetCustomerDto> _customers = [];
    private Exception? _getCustomerByIdException;
    private Exception? _getCustomersException;

    public Task<IReadOnlyList<GetCustomerDto>> GetCustomers(CancellationToken cancellationToken, int maxItems = 100)
    {
        if (_getCustomersException is not null)
        {
            throw _getCustomersException;
        }

        return Task.FromResult<IReadOnlyList<GetCustomerDto>>(_customers.Take(maxItems).ToArray());
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

    public void AddCustomer(GetCustomerDto customer) => _customers.Add(customer);

    public void AddCustomerDetails(CustomerDetailsDto customer) => _customerDetails.Add(customer);

    public void SetGetCustomersException(Exception exception) => _getCustomersException = exception;

    public void SetGetCustomerByIdException(Exception exception) => _getCustomerByIdException = exception;
}
