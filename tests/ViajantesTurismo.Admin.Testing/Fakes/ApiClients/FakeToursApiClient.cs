using System.Net;
using PandaBear.BuildingBlocks;
using PandaBear.HttpClients;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Testing.Builders;

namespace ViajantesTurismo.Admin.Testing.Fakes.ApiClients;

public sealed class FakeToursApiClient : IToursApiClient
{
    private readonly List<GetTourDto> _tours = [];
    private Exception? _createTourException;
    private HttpOutcome? _createTourOutcome;
    private Exception? _getTourByIdException;
    private Func<Guid, CancellationToken, Task<HttpOutcome<GetTourDto>>>? _getTourByIdHandler;
    private HttpOutcome<GetTourDto>? _getTourByIdOutcome;
    private Exception? _getToursException;
    private HttpOutcome<GetTourDto[]>? _getToursOutcome;
    private Func<TourOptionSearchDto, CancellationToken, Task<HttpOutcome<GetTourOptionsDto>>>? _searchTourOptionsHandler;
    private Exception? _updateTourException;
    private HttpOutcome? _updateTourOutcome;

    public int GetToursCallCount
    {
        get; private set;
    }

    public Task<HttpOutcome<GetTourDto[]>> GetTours(CancellationToken ct, int maxItems = int.MaxValue)
    {
        ct.ThrowIfCancellationRequested();
        GetToursCallCount++;

        if (_getToursException is not null)
        {
            throw _getToursException;
        }

        if (_getToursOutcome is not null)
        {
            return Task.FromResult(_getToursOutcome);
        }

        return Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, _tours.Take(maxItems).ToArray()));
    }

    public Task<HttpOutcome<GetTourOptionsDto>> SearchTourOptions(TourOptionSearchDto request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ct.ThrowIfCancellationRequested();
        if (_searchTourOptionsHandler is not null)
        {
            return _searchTourOptionsHandler(request, ct);
        }

        var key = SearchKey.Of(request.Query);
        var matches = _tours.Where(tour => SearchKey.Of(tour.Name).Contains(key, StringComparison.Ordinal) ||
            SearchKey.Of(tour.Identifier).Contains(key, StringComparison.Ordinal)).ToArray();
        var choices = matches.OrderByDescending(tour => tour.Id)
            .Take(Math.Clamp(request.Limit, 1, ContractConstants.MaxTourOptions))
            .Select(tour => new GetTourOptionDto(tour.Id, tour.Name, tour.Identifier, tour.StartDate))
            .ToArray();
        return Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, new GetTourOptionsDto(choices, matches.Length)));
    }

    public void SetSearchTourOptionsHandler(Func<TourOptionSearchDto, CancellationToken, Task<HttpOutcome<GetTourOptionsDto>>> handler) =>
        _searchTourOptionsHandler = handler ?? throw new ArgumentNullException(nameof(handler));

    public Task<HttpOutcome<GetTourDto>> GetTourById(Guid id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (_getTourByIdHandler is not null)
        {
            return _getTourByIdHandler(id, ct);
        }

        if (_getTourByIdException is not null)
        {
            throw _getTourByIdException;
        }

        if (_getTourByIdOutcome is not null)
        {
            return Task.FromResult(_getTourByIdOutcome);
        }

        var tour = _tours.FirstOrDefault(t => t.Id == id);
        return Task.FromResult(tour is null
            ? HttpOutcomeMapper.Status<GetTourDto>(HttpOutcomeKind.NotFound, HttpStatusCode.NotFound)
            : HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, tour));
    }

    public Task<HttpOutcome> CreateTour(CreateTourDto dto, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ct.ThrowIfCancellationRequested();

        if (_createTourException is not null)
        {
            throw _createTourException;
        }

        if (_createTourOutcome is not null)
        {
            return Task.FromResult(_createTourOutcome);
        }

        var newTour = DtoBuilders.WithCapacityFacts(new GetTourDto
        {
            Id = Guid.NewGuid(),
            Identifier = dto.Identifier,
            Name = dto.Name,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Price = dto.Price,
            SingleRoomSupplementPrice = dto.SingleRoomSupplementPrice,
            RegularBikePrice = dto.RegularBikePrice,
            EBikePrice = dto.EBikePrice,
            Currency = dto.Currency,
            IncludedServices = dto.IncludedServices,
            MinCustomers = dto.MinCustomers,
            MaxCustomers = dto.MaxCustomers,
            CurrentCustomerCount = 0
        });

        _tours.Add(newTour);

        return Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.Created, new Uri($"/api/v1/tours/{newTour.Id}", UriKind.Relative)));
    }

    public Task<HttpOutcome> UpdateTour(Guid id, UpdateTourDto dto, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ct.ThrowIfCancellationRequested();

        if (_updateTourException is not null)
        {
            throw _updateTourException;
        }

        if (_updateTourOutcome is not null)
        {
            return Task.FromResult(_updateTourOutcome);
        }

        var tour = _tours.FirstOrDefault(t => t.Id == id);
        if (tour is null)
        {
            return Task.FromResult(HttpOutcomeMapper.Status(HttpOutcomeKind.NotFound, HttpStatusCode.NotFound));
        }

        var index = _tours.IndexOf(tour);
        _tours[index] = DtoBuilders.WithCapacityFacts(tour with
        {
            Identifier = dto.Identifier,
            Name = dto.Name,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Price = dto.Price,
            SingleRoomSupplementPrice = dto.SingleRoomSupplementPrice,
            RegularBikePrice = dto.RegularBikePrice,
            EBikePrice = dto.EBikePrice,
            Currency = dto.Currency,
            IncludedServices = dto.IncludedServices,
            MinCustomers = dto.MinCustomers,
            MaxCustomers = dto.MaxCustomers
        });

        return Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.NoContent, location: null));
    }

    public void AddTour(GetTourDto tour) => _tours.Add(tour);

    public void AddTours(IEnumerable<GetTourDto> tours) => _tours.AddRange(tours);

    public void SetGetTourByIdHandler(Func<Guid, CancellationToken, Task<HttpOutcome<GetTourDto>>> handler) =>
        _getTourByIdHandler = handler ?? throw new ArgumentNullException(nameof(handler));

    public void SetGetTourByIdException(Exception exception) => _getTourByIdException = exception;

    public void SetGetTourByIdOutcome(HttpOutcome<GetTourDto> outcome) => _getTourByIdOutcome = outcome;

    public void SetGetToursException(Exception exception) => _getToursException = exception;

    public void SetGetToursOutcome(HttpOutcome<GetTourDto[]> outcome) => _getToursOutcome = outcome;

    public void SetCreateTourException(Exception exception) => _createTourException = exception;

    public void SetCreateTourOutcome(HttpOutcome outcome) => _createTourOutcome = outcome;

    public void SetUpdateTourException(Exception exception) => _updateTourException = exception;

    public void SetUpdateTourOutcome(HttpOutcome outcome) => _updateTourOutcome = outcome;
}
