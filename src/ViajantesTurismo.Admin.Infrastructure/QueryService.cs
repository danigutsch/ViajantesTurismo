using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Admin.Domain;
using ViajantesTurismo.AdminApi.Contracts;

namespace ViajantesTurismo.Admin.Infrastructure;

internal sealed class QueryService(ApplicationDbContext dbContext) : IQueryService
{
    public async Task<IReadOnlyList<GetTourDto>> GetAllTours(CancellationToken ct)
    {
        var tours = await dbContext.Tours.ToListAsync(ct);
        return
        [
            ..tours.Select(tour => new GetTourDto()
            {
                Id = tour.Id,
                Identifier = tour.Identifier,
                Name = tour.Name,
                StartDate = tour.StartDate,
                EndDate = tour.EndDate,
                Price = tour.Price,
                SingleRoomSupplementPrice = tour.SingleRoomSupplementPrice,
                RegularBikePrice = tour.RegularBikePrice,
                EBikePrice = tour.EBikePrice,
                Currency = (CurrencyDto)tour.Currency,
                IncludedServices = [..tour.IncludedServices]
            })
        ];
    }
}
