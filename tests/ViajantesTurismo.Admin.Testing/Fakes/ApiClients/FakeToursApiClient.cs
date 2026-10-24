using System.Net;
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
    private HttpOutcome<GetTourDto>? _getTourByIdOutcome;
    private Exception? _getToursException;
    private HttpOutcome<GetTourDto[]>? _getToursOutcome;
    private Exception? _updateTourException;
    private HttpOutcome? _updateTourOutcome;

    public Task<HttpOutcome<GetTourDto[]>> GetTours(CancellationToken cancellationToken, int maxItems = int.MaxValue)
    {
        cancellationToken.ThrowIfCancellationRequested();

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

    public Task<HttpOutcome<GetTourDto>> GetTourById(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

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

    public Task<HttpOutcome> CreateTour(CreateTourDto dto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dto);
        cancellationToken.ThrowIfCancellationRequested();

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

    public Task<HttpOutcome> UpdateTour(Guid id, UpdateTourDto dto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dto);
        cancellationToken.ThrowIfCancellationRequested();

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

    public void SetGetTourByIdException(Exception exception) => _getTourByIdException = exception;

    public void SetGetTourByIdOutcome(HttpOutcome<GetTourDto> outcome) => _getTourByIdOutcome = outcome;

    public void SetGetToursException(Exception exception) => _getToursException = exception;

    public void SetGetToursOutcome(HttpOutcome<GetTourDto[]> outcome) => _getToursOutcome = outcome;

    public void SetCreateTourException(Exception exception) => _createTourException = exception;

    public void SetCreateTourOutcome(HttpOutcome outcome) => _createTourOutcome = outcome;

    public void SetUpdateTourException(Exception exception) => _updateTourException = exception;

    public void SetUpdateTourOutcome(HttpOutcome outcome) => _updateTourOutcome = outcome;
}
