using System.Net;
using SharedKernel.BuildingBlocks;
using SharedKernel.HttpClients;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Testing.Fakes.ApiClients;

public sealed class FakeCustomersApiClient : ICustomersApiClient
{
    private readonly List<CustomerDetailsDto> _customerDetails = [];
    private readonly List<GetCustomerDto> _customers = [];
    private Func<string[], CancellationToken, Task<Guid?[]>>? _findImportConflictIdsHandler;
    private Func<ReadOnlyMemory<byte>, string, IReadOnlyDictionary<string, string>, IProgress<long>?, CancellationToken, Task<ImportResultDto>>? _commitImportHandler;
    private Func<ReadOnlyMemory<byte>, string, IProgress<long>?, CancellationToken, Task<ImportResultDto>>? _importCustomersHandler;
    public CancellationToken LastCommitCancellationToken
    {
        get; private set;
    }
    public CancellationToken LastImportCancellationToken
    {
        get; private set;
    }
    public int ImportCustomersCallCount
    {
        get; private set;
    }
    private ImportResultDto? _commitImportResult;
    private HttpOutcome<ImportResultDto>? _commitImportOutcome;
    private Exception? _createCustomerException;
    private HttpOutcome? _createCustomerOutcome;
    private Exception? _getCustomerByIdException;
    private HttpOutcome<CustomerDetailsDto>? _getCustomerByIdOutcome;
    private HttpOutcome<Guid?[]>? _findImportConflictIdsOutcome;
    private HttpOutcome<GetCustomerPageDto>? _getCustomerPageOutcome;
    private Func<CustomerOptionSearchDto, CancellationToken, Task<HttpOutcome<GetCustomerOptionsDto>>>? _searchCustomerOptionsHandler;
    private Exception? _importCustomersException;
    private HttpOutcome<ImportResultDto>? _importCustomersOutcome;
    private ImportResultDto? _importResult;
    private Exception? _updateCustomerException;
    private HttpOutcome? _updateCustomerOutcome;

    public ReadOnlyMemory<byte> LastCommitFileContent
    {
        get; private set;
    }

    public string? LastCommitFileName
    {
        get; private set;
    }

    public IReadOnlyDictionary<string, string>? LastCommitConflictResolutions
    {
        get; private set;
    }

    /// <summary>Gets the most recent customer page number and sort.</summary>
    public (int Page, CustomerPageSort Sort, bool Descending)? LastCustomerPageRequest
    {
        get; private set;
    }

    /// <summary>Gets the most recent customer update request.</summary>
    public UpdateCustomerDto? LastUpdatedCustomer
    {
        get; private set;
    }

    /// <inheritdoc />
    public Task<HttpOutcome<GetCustomerPageDto>> GetCustomerPage(int page, CustomerPageSort sort, bool descending, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        LastCustomerPageRequest = (page, sort, descending);
        if (_getCustomerPageOutcome is not null)
        {
            return Task.FromResult(_getCustomerPageOutcome);
        }

        var ordered = sort switch
        {
            CustomerPageSort.Email => OrderCustomers(customer => customer.Email, descending),
            CustomerPageSort.Nationality => OrderCustomers(customer => customer.Nationality, descending),
            _ => descending
                ? OrderCustomers(customer => customer.FirstName, descending).ThenByDescending(customer => customer.LastName, StringComparer.Ordinal)
                : OrderCustomers(customer => customer.FirstName, descending).ThenBy(customer => customer.LastName, StringComparer.Ordinal)
        };
        var window = PageWindow.Create(page, ContractConstants.CustomerListPageSize, _customers.Count);
        var rows = ordered.ThenBy(customer => customer.Id).Skip(window.Skip).Take(window.Take)
            .Select(customer => new GetCustomerListItemDto(customer.Id, customer.FirstName, customer.LastName,
                customer.Email, customer.Mobile, customer.Nationality)).ToArray();
        return Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, new GetCustomerPageDto(rows, window.Total)));
    }

    private IOrderedEnumerable<GetCustomerDto> OrderCustomers(Func<GetCustomerDto, string> key, bool descending) =>
        descending ? _customers.OrderByDescending(key, StringComparer.Ordinal) : _customers.OrderBy(key, StringComparer.Ordinal);

    public Task<HttpOutcome<GetCustomerOptionsDto>> SearchCustomerOptions(CustomerOptionSearchDto request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ct.ThrowIfCancellationRequested();

        if (_searchCustomerOptionsHandler is not null)
        {
            return _searchCustomerOptionsHandler(request, ct);
        }

        var query = SearchKey.Of(request.Query);
        var eligible = _customers.Where(customer => customer.Id != request.ExcludedCustomerId);
        var matches = (Guid.TryParse(query, out var id)
            ? eligible.Where(customer => customer.Id == id)
            : eligible.Where(customer => SearchKey.Of($"{customer.FirstName} {customer.LastName}")
                .Contains(query, StringComparison.Ordinal))).ToArray();
        var choices = matches.OrderBy(customer => customer.FirstName)
            .ThenBy(customer => customer.LastName)
            .ThenBy(customer => customer.Id)
            .Take(Math.Clamp(request.Limit, 1, ContractConstants.MaxCustomerOptions))
            .Select(customer => new GetCustomerOptionDto(
                customer.Id, customer.FirstName, customer.LastName, customer.Email, customer.BikeType))
            .ToArray();
        return Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, new GetCustomerOptionsDto(choices, matches.Length)));
    }

    public void SetSearchCustomerOptionsHandler(Func<CustomerOptionSearchDto, CancellationToken, Task<HttpOutcome<GetCustomerOptionsDto>>> handler) =>
        _searchCustomerOptionsHandler = handler ?? throw new ArgumentNullException(nameof(handler));

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

    public async Task<HttpOutcome<Guid?[]>> FindCustomerImportConflictIds(string[] emails, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(emails);
        ct.ThrowIfCancellationRequested();

        if (_findImportConflictIdsHandler is not null)
        {
            return HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, await _findImportConflictIdsHandler(emails, ct));
        }

        if (_findImportConflictIdsOutcome is not null)
        {
            return _findImportConflictIdsOutcome;
        }

        var matches = emails.Select(email => _customers.FirstOrDefault(
            customer => customer.Email.Equals(email, StringComparison.OrdinalIgnoreCase))?.Id).ToArray();
        return HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, matches);
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
        LastUpdatedCustomer = dto;

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

    public async Task<HttpOutcome<ImportResultDto>> ImportCustomers(ReadOnlyMemory<byte> fileContent, string fileName, IProgress<long>? progress, CancellationToken ct)
    {
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
            progress?.Report(fileContent.Length);
            return HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, _importResult);
        }

        throw new NotImplementedException();
    }

    public async Task<HttpOutcome<ImportResultDto>> CommitImportWithResolutions(
        ReadOnlyMemory<byte> fileContent,
        string fileName,
        IReadOnlyDictionary<string, string> conflictResolutions,
        IProgress<long>? progress,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(fileName);
        ArgumentNullException.ThrowIfNull(conflictResolutions);
        ct.ThrowIfCancellationRequested();

        LastCommitFileContent = fileContent;
        LastCommitFileName = fileName;
        LastCommitConflictResolutions = new Dictionary<string, string>(conflictResolutions, StringComparer.OrdinalIgnoreCase);
        LastCommitCancellationToken = ct;

        if (_commitImportHandler is not null)
        {
            var result = await _commitImportHandler(fileContent, fileName, conflictResolutions, progress, ct);
            return HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, result);
        }

        ct.ThrowIfCancellationRequested();

        if (_commitImportOutcome is not null)
        {
            return _commitImportOutcome;
        }

        if (_commitImportResult is not null)
        {
            progress?.Report(fileContent.Length);
            return HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, _commitImportResult);
        }

        throw new NotImplementedException();
    }

    public void AddCustomer(GetCustomerDto customer) => _customers.Add(customer);

    public void AddCustomers(IEnumerable<GetCustomerDto> customers) => _customers.AddRange(customers);

    public void AddCustomerDetails(CustomerDetailsDto customer) => _customerDetails.Add(customer);

    public void SetGetCustomerByIdException(Exception exception) => _getCustomerByIdException = exception;

    public void SetGetCustomerByIdOutcome(HttpOutcome<CustomerDetailsDto> outcome) => _getCustomerByIdOutcome = outcome;

    /// <summary>Sets the response for requests to the customer page.</summary>
    public void SetGetCustomerPageOutcome(HttpOutcome<GetCustomerPageDto> outcome) => _getCustomerPageOutcome = outcome;

    /// <summary>Sets the response for an import conflict lookup.</summary>
    public void SetFindImportConflictIdsOutcome(HttpOutcome<Guid?[]> outcome) => _findImportConflictIdsOutcome = outcome;

    /// <summary>Replaces the import conflict lookup with a test-owned handler.</summary>
    public void SetFindImportConflictIdsHandler(Func<string[], CancellationToken, Task<Guid?[]>> handler) => _findImportConflictIdsHandler = handler;

    public void SetCreateCustomerException(Exception exception) => _createCustomerException = exception;

    public void SetCreateCustomerOutcome(HttpOutcome outcome) => _createCustomerOutcome = outcome;

    public void SetUpdateCustomerException(Exception exception) => _updateCustomerException = exception;

    public void SetUpdateCustomerOutcome(HttpOutcome outcome) => _updateCustomerOutcome = outcome;

    public void SetImportCustomersResult(ImportResultDto result) => _importResult = result;

    public void SetImportCustomersHandler(Func<ReadOnlyMemory<byte>, string, IProgress<long>?, CancellationToken, Task<ImportResultDto>> handler) => _importCustomersHandler = handler;

    public void SetCommitImportHandler(
        Func<ReadOnlyMemory<byte>, string, IReadOnlyDictionary<string, string>, IProgress<long>?, CancellationToken, Task<ImportResultDto>> handler) => _commitImportHandler = handler;

    public void SetCommitImportResult(ImportResultDto result) => _commitImportResult = result;

    public void SetCommitImportOutcome(HttpOutcome<ImportResultDto> outcome) => _commitImportOutcome = outcome;

    public void SetImportCustomersException(Exception exception) => _importCustomersException = exception;

    public void SetImportCustomersOutcome(HttpOutcome<ImportResultDto> outcome) => _importCustomersOutcome = outcome;

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
