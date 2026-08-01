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
    private ContractCommandOutcomeDto<CustomerDetailsDto>? _getCustomerByIdOutcome;
    private Exception? _getCustomersException;
    private ContractCommandOutcomeDto<GetCustomerDto[]>? _getCustomersOutcome;
    private Exception? _importCustomersException;
    private ContractCommandOutcomeDto<ImportResultDto>? _importCustomersOutcome;
    private ImportResultDto? _importResult;
    private Exception? _updateCustomerException;
    private ContractCommandOutcomeDto? _updateCustomerOutcome;

    public IReadOnlyList<byte>? LastCommitFileContent { get; private set; }

    public string? LastCommitFileName { get; private set; }

    public IReadOnlyDictionary<string, string>? LastCommitConflictResolutions { get; private set; }

    public int? LastGetCustomersMaxItems { get; private set; }

    public Task<ContractCommandOutcomeDto<GetCustomerDto[]>> GetCustomers(CancellationToken ct, int maxItems = 100)
    {
        ct.ThrowIfCancellationRequested();
        LastGetCustomersMaxItems = maxItems;

        if (_getCustomersException is not null)
        {
            throw _getCustomersException;
        }

        if (_getCustomersOutcome is not null)
        {
            return Task.FromResult(_getCustomersOutcome);
        }

        return Task.FromResult(ContractCommandOutcome.Succeeded(
            HttpStatusCode.OK,
            _customers.Take(maxItems).ToArray()));
    }

    public Task<ContractCommandOutcomeDto<CustomerDetailsDto>> GetCustomerById(Guid id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (_getCustomerByIdException is not null)
        {
            throw _getCustomerByIdException;
        }

        if (_getCustomerByIdOutcome is not null)
        {
            return Task.FromResult(_getCustomerByIdOutcome);
        }

        var customer = _customerDetails.FirstOrDefault(c => c.Id == id);
        return Task.FromResult(customer is null
            ? ContractCommandOutcome.Status<CustomerDetailsDto>(ContractCommandOutcomeKind.NotFound, HttpStatusCode.NotFound)
            : ContractCommandOutcome.Succeeded(HttpStatusCode.OK, customer));
    }

    public Task<ContractCommandOutcomeDto> CreateCustomer(CreateCustomerDto dto, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ct.ThrowIfCancellationRequested();

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
        ArgumentNullException.ThrowIfNull(dto);
        ct.ThrowIfCancellationRequested();

        if (_updateCustomerException is not null)
        {
            throw _updateCustomerException;
        }

        if (_updateCustomerOutcome is not null)
        {
            return Task.FromResult(_updateCustomerOutcome);
        }

        return Task.FromResult(ContractCommandOutcome.Succeeded(HttpStatusCode.NoContent, location: null));
    }

    public Task<ContractCommandOutcomeDto<ImportResultDto>> ImportCustomers(byte[] fileContent, string fileName, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(fileContent);
        ArgumentNullException.ThrowIfNull(fileName);
        ct.ThrowIfCancellationRequested();

        if (_importCustomersException is not null)
        {
            throw _importCustomersException;
        }

        if (_importCustomersOutcome is not null)
        {
            return Task.FromResult(_importCustomersOutcome);
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
        ArgumentNullException.ThrowIfNull(fileContent);
        ArgumentNullException.ThrowIfNull(fileName);
        ArgumentNullException.ThrowIfNull(conflictResolutions);
        ct.ThrowIfCancellationRequested();

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

    public void SetGetCustomerByIdOutcome(ContractCommandOutcomeDto<CustomerDetailsDto> outcome) => _getCustomerByIdOutcome = outcome;

    public void SetGetCustomersException(Exception exception) => _getCustomersException = exception;

    public void SetGetCustomersOutcome(ContractCommandOutcomeDto<GetCustomerDto[]> outcome) => _getCustomersOutcome = outcome;

    public void SetCreateCustomerException(Exception exception) => _createCustomerException = exception;

    public void SetCreateCustomerOutcome(ContractCommandOutcomeDto outcome) => _createCustomerOutcome = outcome;

    public void SetUpdateCustomerException(Exception exception) => _updateCustomerException = exception;

    public void SetUpdateCustomerOutcome(ContractCommandOutcomeDto outcome) => _updateCustomerOutcome = outcome;

    public void SetImportCustomersResult(ImportResultDto result) => _importResult = result;

    public void SetCommitImportResult(ImportResultDto result) => _commitImportResult = result;

    public void SetImportCustomersException(Exception exception) => _importCustomersException = exception;

    public void SetImportCustomersOutcome(ContractCommandOutcomeDto<ImportResultDto> outcome) => _importCustomersOutcome = outcome;
}
