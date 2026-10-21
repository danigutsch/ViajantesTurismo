using System.Net;
using PandaBear.HttpClients;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Testing.Fakes.ApiClients;

public sealed class FakeCustomersApiClient : ICustomersApiClient
{
    private readonly List<CustomerDetailsDto> _customerDetails = [];
    private readonly List<GetCustomerDto> _customers = [];
    private Func<CancellationToken, int, Task<IReadOnlyList<GetCustomerDto>>>? _getCustomersHandler;
    private Func<byte[], string, IReadOnlyDictionary<string, string>, IProgress<long>?, CancellationToken, Task<ImportResultDto>>? _commitImportHandler;
    private Func<byte[], string, IProgress<long>?, CancellationToken, Task<ImportResultDto>>? _importCustomersHandler;
    public CancellationToken LastCommitCancellationToken { get; private set; }
    public CancellationToken LastImportCancellationToken { get; private set; }
    public int ImportCustomersCallCount { get; private set; }
    private ImportResultDto? _commitImportResult;
    private Exception? _createCustomerException;
    private HttpOutcome? _createCustomerOutcome;
    private Exception? _getCustomerByIdException;
    private HttpOutcome<CustomerDetailsDto>? _getCustomerByIdOutcome;
    private Exception? _getCustomersException;
    private HttpOutcome<GetCustomerDto[]>? _getCustomersOutcome;
    private Exception? _importCustomersException;
    private HttpOutcome<ImportResultDto>? _importCustomersOutcome;
    private ImportResultDto? _importResult;
    private Exception? _updateCustomerException;
    private HttpOutcome? _updateCustomerOutcome;

    public IReadOnlyList<byte>? LastCommitFileContent { get; private set; }

    public string? LastCommitFileName { get; private set; }

    public IReadOnlyDictionary<string, string>? LastCommitConflictResolutions { get; private set; }

    public int? LastGetCustomersMaxItems { get; private set; }

    public async Task<HttpOutcome<GetCustomerDto[]>> GetCustomers(CancellationToken ct, int maxItems = 100)
    {
        ct.ThrowIfCancellationRequested();
        LastGetCustomersMaxItems = maxItems;

        if (_getCustomersHandler is not null)
        {
            return HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, (await _getCustomersHandler(ct, maxItems)).ToArray());
        }

        if (_getCustomersException is not null)
        {
            throw _getCustomersException;
        }

        if (_getCustomersOutcome is not null)
        {
            return _getCustomersOutcome;
        }

        return HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, _customers.Take(maxItems).ToArray());
    }

    public Task<HttpOutcome<CustomerDetailsDto>> GetCustomerById(Guid id, CancellationToken ct)
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
            ? HttpOutcomeMapper.Status<CustomerDetailsDto>(HttpOutcomeKind.NotFound, HttpStatusCode.NotFound)
            : HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, customer));
    }

    public Task<HttpOutcome> CreateCustomer(CreateCustomerDto dto, CancellationToken ct)
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
        return Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.Created, new Uri($"/api/v1/customers/{customerId}", UriKind.Relative)));
    }

    public Task<HttpOutcome> UpdateCustomer(Guid id, UpdateCustomerDto dto, CancellationToken ct)
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

        return Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.NoContent, location: null));
    }

    public async Task<HttpOutcome<ImportResultDto>> ImportCustomers(byte[] fileContent, string fileName, IProgress<long>? progress, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(fileContent);
        ArgumentNullException.ThrowIfNull(fileName);
        ct.ThrowIfCancellationRequested();

        LastImportCancellationToken = ct;
        ImportCustomersCallCount++;
        if (_importCustomersHandler is not null)
        {
            var result = await _importCustomersHandler(fileContent, fileName, progress, ct);
            return HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, result);
        }

        if (_importCustomersException is not null)
        {
            throw _importCustomersException;
        }

        if (_importCustomersOutcome is not null)
        {
            return _importCustomersOutcome;
        }

        if (_importResult is not null)
        {
            progress?.Report(fileContent.LongLength);
            return HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, _importResult);
        }

        throw new NotImplementedException();
    }

    public async Task<HttpOutcome<ImportResultDto>> CommitImportWithResolutions(
        byte[] fileContent,
        string fileName,
        IReadOnlyDictionary<string, string> conflictResolutions,
        IProgress<long>? progress,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(fileContent);
        ArgumentNullException.ThrowIfNull(fileName);
        ArgumentNullException.ThrowIfNull(conflictResolutions);
        ct.ThrowIfCancellationRequested();

        LastCommitFileContent = [.. fileContent];
        LastCommitFileName = fileName;
        LastCommitConflictResolutions = new Dictionary<string, string>(conflictResolutions, StringComparer.OrdinalIgnoreCase);
        LastCommitCancellationToken = ct;

        if (_commitImportHandler is not null)
        {
            var result = await _commitImportHandler(fileContent, fileName, conflictResolutions, progress, ct);
            return HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, result);
        }

        ct.ThrowIfCancellationRequested();

        if (_commitImportResult is not null)
        {
            progress?.Report(fileContent.LongLength);
            return HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, _commitImportResult);
        }

        throw new NotImplementedException();
    }

    public void AddCustomer(GetCustomerDto customer) => _customers.Add(customer);

    public void AddCustomerDetails(CustomerDetailsDto customer) => _customerDetails.Add(customer);

    public void SetGetCustomerByIdException(Exception exception) => _getCustomerByIdException = exception;

    public void SetGetCustomerByIdOutcome(HttpOutcome<CustomerDetailsDto> outcome) => _getCustomerByIdOutcome = outcome;

    public void SetGetCustomersException(Exception exception) => _getCustomersException = exception;

    public void SetGetCustomersHandler(Func<CancellationToken, int, Task<IReadOnlyList<GetCustomerDto>>> handler) => _getCustomersHandler = handler;

    public void SetGetCustomersOutcome(HttpOutcome<GetCustomerDto[]> outcome) => _getCustomersOutcome = outcome;

    public void SetCreateCustomerException(Exception exception) => _createCustomerException = exception;

    public void SetCreateCustomerOutcome(HttpOutcome outcome) => _createCustomerOutcome = outcome;

    public void SetUpdateCustomerException(Exception exception) => _updateCustomerException = exception;

    public void SetUpdateCustomerOutcome(HttpOutcome outcome) => _updateCustomerOutcome = outcome;

    public void SetImportCustomersResult(ImportResultDto result) => _importResult = result;

    public void SetImportCustomersHandler(Func<byte[], string, IProgress<long>?, CancellationToken, Task<ImportResultDto>> handler) => _importCustomersHandler = handler;

    public void SetCommitImportHandler(
        Func<byte[], string, IReadOnlyDictionary<string, string>, IProgress<long>?, CancellationToken, Task<ImportResultDto>> handler) => _commitImportHandler = handler;

    public void SetCommitImportResult(ImportResultDto result) => _commitImportResult = result;

    public void SetImportCustomersException(Exception exception) => _importCustomersException = exception;

    public void SetImportCustomersOutcome(HttpOutcome<ImportResultDto> outcome) => _importCustomersOutcome = outcome;

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
