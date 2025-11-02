using JetBrains.Annotations;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.BehaviorTests.Context;

[UsedImplicitly]
public sealed class TourContext
{
    public string Identifier { get; set; } = null!;
    public string Name { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal BasePrice { get; set; }
    public decimal SingleRoomSupplementPrice { get; set; }
    public decimal RegularBikePrice { get; set; }
    public decimal EBikePrice { get; set; }
    public Tour Tour { get; set; } = null!;
    public object Result { get; set; } = null!;
}