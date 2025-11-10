using JetBrains.Annotations;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.BehaviorTests.Context;

[UsedImplicitly]
public sealed class TourContext
{
    public required string Identifier { get; set; }
    public required string Name { get; set; }
    public required DateTime StartDate { get; set; }
    public required DateTime EndDate { get; set; }
    public required decimal BasePrice { get; set; }
    public required decimal DoubleRoomSupplementPrice { get; set; }
    public required decimal RegularBikePrice { get; set; }
    public required decimal EBikePrice { get; set; }
    public ICollection<string> IncludedServices { get; } = [];
    public required Tour Tour { get; set; }
    public required object Result { get; set; }
    public Result? UpdateResult { get; set; }
    public Result<Booking>? BookingResult { get; set; }
}
