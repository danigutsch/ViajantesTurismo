using System.Net;
using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.Tests.Shared.Fakes.ApiClients;

public sealed class FakeToursApiClient : IToursApiClient
{
    private readonly List<GetTourDto> _tours = [];
    private Exception? _createTourException;
    private Exception? _getTourByIdException;
    private Exception? _getToursException;
    private Exception? _updateTourException;

    public Task<GetTourDto[]> GetTours(CancellationToken cancellationToken, int maxItems = int.MaxValue)
    {
        if (_getToursException is not null)
        {
            throw _getToursException;
        }

        return Task.FromResult(_tours.Take(maxItems).ToArray());
    }

    public Task<GetTourDto?> GetTourById(Guid id, CancellationToken cancellationToken)
    {
        if (_getTourByIdException is not null)
        {
            throw _getTourByIdException;
        }

        return Task.FromResult(_tours.FirstOrDefault(t => t.Id == id));
    }

    public Task<Uri> CreateTour(CreateTourDto dto, CancellationToken cancellationToken)
    {
        if (_createTourException is not null)
        {
            throw _createTourException;
        }

        var newTour = new GetTourDto
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
        };

        _tours.Add(newTour);

        return Task.FromResult(new Uri($"/tours/{newTour.Id}", UriKind.Relative));
    }

    public Task UpdateTour(Guid id, UpdateTourDto dto, CancellationToken cancellationToken)
    {
        if (_updateTourException is not null)
        {
            throw _updateTourException;
        }

        var tour = _tours.FirstOrDefault(t => t.Id == id)
                   ?? throw new HttpRequestException("Tour not found", null, HttpStatusCode.NotFound);

        var index = _tours.IndexOf(tour);
        _tours[index] = tour with
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
        };

        return Task.CompletedTask;
    }

    public void AddTour(GetTourDto tour) => _tours.Add(tour);

    public void SetGetTourByIdException(Exception exception) => _getTourByIdException = exception;

    public void SetGetToursException(Exception exception) => _getToursException = exception;

    public void SetCreateTourException(Exception exception) => _createTourException = exception;

    public void SetUpdateTourException(Exception exception) => _updateTourException = exception;
}
