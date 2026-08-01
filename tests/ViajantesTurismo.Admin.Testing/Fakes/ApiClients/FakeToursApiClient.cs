using System.Net;
using SharedKernel.HttpClients;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Testing.Fakes.ApiClients;

public sealed class FakeToursApiClient : IToursApiClient
{
    private readonly List<GetTourDto> _tours = [];
    private Exception? _createTourException;
    private ContractCommandOutcomeDto? _createTourOutcome;
    private Exception? _getTourByIdException;
    private Exception? _getToursException;
    private Exception? _updateTourException;

    public Task<ContractCommandOutcomeDto<GetTourDto[]>> GetTours(CancellationToken cancellationToken, int maxItems = int.MaxValue)
    {
        if (_getToursException is not null)
        {
            return Task.FromResult(new ContractCommandOutcomeDto<GetTourDto[]>
            {
                Kind = ContractCommandOutcomeKind.TransportFailure,
                Message = "HTTP transport failed."
            });
        }

        return Task.FromResult(ContractCommandOutcome.Succeeded(HttpStatusCode.OK, _tours.Take(maxItems).ToArray()));
    }

    public Task<ContractCommandOutcomeDto<GetTourDto>> GetTourById(Guid id, CancellationToken cancellationToken)
    {
        if (_getTourByIdException is not null)
        {
            return Task.FromResult(new ContractCommandOutcomeDto<GetTourDto>
            {
                Kind = ContractCommandOutcomeKind.TransportFailure,
                Message = "HTTP transport failed."
            });
        }

        var tour = _tours.FirstOrDefault(t => t.Id == id);
        return Task.FromResult(tour is null
            ? ContractCommandOutcome.Status<GetTourDto>(ContractCommandOutcomeKind.NotFound, HttpStatusCode.NotFound)
            : ContractCommandOutcome.Succeeded(HttpStatusCode.OK, tour));
    }

    public Task<ContractCommandOutcomeDto> CreateTour(CreateTourDto dto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (_createTourException is not null)
        {
            throw _createTourException;
        }

        if (_createTourOutcome is not null)
        {
            return Task.FromResult(_createTourOutcome);
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

        return Task.FromResult(ContractCommandOutcome.Succeeded(HttpStatusCode.Created, new Uri($"/api/v1/tours/{newTour.Id}", UriKind.Relative)));
    }

    public Task<ContractCommandOutcomeDto> UpdateTour(Guid id, UpdateTourDto dto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (_updateTourException is not null)
        {
            return Task.FromResult(new ContractCommandOutcomeDto
            {
                Kind = ContractCommandOutcomeKind.TransportFailure,
                Message = "HTTP transport failed."
            });
        }

        var tour = _tours.FirstOrDefault(t => t.Id == id);
        if (tour is null)
        {
            return Task.FromResult(ContractCommandOutcome.Status(ContractCommandOutcomeKind.NotFound, HttpStatusCode.NotFound));
        }

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

        return Task.FromResult(ContractCommandOutcome.Succeeded(HttpStatusCode.NoContent, location: null));
    }

    public void AddTour(GetTourDto tour) => _tours.Add(tour);

    public void SetGetTourByIdException(Exception exception) => _getTourByIdException = exception;

    public void SetGetToursException(Exception exception) => _getToursException = exception;

    public void SetCreateTourException(Exception exception) => _createTourException = exception;

    public void SetCreateTourOutcome(ContractCommandOutcomeDto outcome) => _createTourOutcome = outcome;

    public void SetUpdateTourException(Exception exception) => _updateTourException = exception;
}
