using System.Net;
using SharedKernel.HttpClients;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Testing.Fakes.ApiClients;

public sealed class FakeCustomersApiClient : ICustomersApiClient
{
    private readonly List<CustomerDetailsDto> _customerDetails = [];
    private readonly List<GetCustomerDto> _customers = [];
    private ImportResultDto? _commitImportResult;
    private Exception? _createCustomerException;
    private ContractCommandOutcomeDto? _createCustomerOutcome;
    private Exception? _getCustomerByIdException;
    private Exception? _getCustomersException;
    private ContractCommandOutcomeDto<GetCustomerDto[]>? _getCustomersOutcome;
    private Exception? _importCustomersException;
    private ImportResultDto? _importResult;
    private Exception? _updateCustomerException;

    public IReadOnlyList<byte>? LastCommitFileContent { get; private set; }

    public string? LastCommitFileName { get; private set; }

    public IReadOnlyDictionary<string, string>? LastCommitConflictResolutions { get; private set; }

    public int? LastGetCustomersMaxItems { get; private set; }

    public Task<ContractCommandOutcomeDto<GetCustomerDto[]>> GetCustomers(CancellationToken ct, int maxItems = 100)
    {
        LastGetCustomersMaxItems = maxItems;

        if (_getCustomersOutcome is not null)
        {
            return Task.FromResult(_getCustomersOutcome);
        }

        if (_getCustomersException is not null)
        {
            return Task.FromResult(new ContractCommandOutcomeDto<GetCustomerDto[]>
            {
                Kind = ContractCommandOutcomeKind.TransportFailure,
                Message = "HTTP transport failed."
            });
        }

        return Task.FromResult(ContractCommandOutcome.Succeeded(
            HttpStatusCode.OK,
            _customers.Take(maxItems).ToArray()));
    }

    public Task<ContractCommandOutcomeDto<CustomerDetailsDto>> GetCustomerById(Guid id, CancellationToken ct)
    {
        if (_getCustomerByIdException is not null)
        {
            return Task.FromResult(new ContractCommandOutcomeDto<CustomerDetailsDto>
            {
                Kind = ContractCommandOutcomeKind.TransportFailure,
                Message = "HTTP transport failed."
            });
        }

        var customer = _customerDetails.FirstOrDefault(c => c.Id == id);
        return Task.FromResult(customer is null
            ? ContractCommandOutcome.Status<CustomerDetailsDto>(ContractCommandOutcomeKind.NotFound, HttpStatusCode.NotFound)
            : ContractCommandOutcome.Succeeded(HttpStatusCode.OK, customer));
    }

    public Task<ContractCommandOutcomeDto> CreateCustomer(CreateCustomerDto dto, CancellationToken ct)
    {
        if (_createCustomerException is not null)
        {
            throw _createCustomerException;
        }

        if (_createCustomerOutcome is not null)
        {
            return Task.FromResult(_createCustomerOutcome);
        }

        var customerId = Guid.NewGuid();
        return Task.FromResult(ContractCommandOutcome.Succeeded(HttpStatusCode.Created, new Uri($"/api/v1/customers/{customerId}", UriKind.Relative)));
    }

    public Task<ContractCommandOutcomeDto> UpdateCustomer(Guid id, UpdateCustomerDto dto, CancellationToken ct)
    {
        if (_updateCustomerException is not null)
        {
            return Task.FromResult(new ContractCommandOutcomeDto
            {
                Kind = ContractCommandOutcomeKind.TransportFailure,
                Message = "HTTP transport failed."
            });
        }

        return Task.FromResult(ContractCommandOutcome.Succeeded(HttpStatusCode.NoContent, location: null));
    }

    public Task<ContractCommandOutcomeDto<ImportResultDto>> ImportCustomers(byte[] fileContent, string fileName, CancellationToken ct)
    {
        if (_importCustomersException is not null)
        {
            return Task.FromResult(new ContractCommandOutcomeDto<ImportResultDto>
            {
                Kind = ContractCommandOutcomeKind.TransportFailure,
                Message = "HTTP transport failed."
            });
        }

        if (_importResult is not null)
        {
            return Task.FromResult(ContractCommandOutcome.Succeeded(HttpStatusCode.OK, _importResult));
        }

        throw new NotImplementedException();
    }

    public Task<ContractCommandOutcomeDto<ImportResultDto>> CommitImportWithResolutions(
        byte[] fileContent,
        string fileName,
        IReadOnlyDictionary<string, string> conflictResolutions,
        CancellationToken ct)
    {
        LastCommitFileContent = [.. fileContent];
        LastCommitFileName = fileName;
        LastCommitConflictResolutions = new Dictionary<string, string>(conflictResolutions, StringComparer.OrdinalIgnoreCase);

        if (_commitImportResult is not null)
        {
            return Task.FromResult(ContractCommandOutcome.Succeeded(HttpStatusCode.OK, _commitImportResult));
        }

        throw new NotImplementedException();
    }

    public void AddCustomer(GetCustomerDto customer) => _customers.Add(customer);

    public void AddCustomerDetails(CustomerDetailsDto customer) => _customerDetails.Add(customer);

    public void SetGetCustomerByIdException(Exception exception) => _getCustomerByIdException = exception;

    public void SetGetCustomersException(Exception exception) => _getCustomersException = exception;

    public void SetGetCustomersOutcome(ContractCommandOutcomeDto<GetCustomerDto[]> outcome) => _getCustomersOutcome = outcome;

    public void SetCreateCustomerException(Exception exception) => _createCustomerException = exception;

    public void SetCreateCustomerOutcome(ContractCommandOutcomeDto outcome) => _createCustomerOutcome = outcome;

    public void SetUpdateCustomerException(Exception exception) => _updateCustomerException = exception;

    public void SetImportCustomersResult(ImportResultDto result) => _importResult = result;

    public void SetCommitImportResult(ImportResultDto result) => _commitImportResult = result;

    public void SetImportCustomersException(Exception exception) => _importCustomersException = exception;
}
