using JetBrains.Annotations;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.BehaviorTests.Context;

[UsedImplicitly]
public sealed class TourContext
{
    public required string Identifier { get; set; }
    public required string Name { get; set; }
    public required DateTime StartDate { get; set; }
    public required DateTime EndDate { get; set; }
    public required decimal BasePrice { get; set; }
    public required decimal SingleRoomSupplementPrice { get; set; }
    public required decimal RegularBikePrice { get; set; }
    public required decimal EBikePrice { get; set; }
#pragma warning disable CA1002 // Do not expose generic lists - acceptable for test context
    public List<string> IncludedServices { get; init; } = [];
#pragma warning restore CA1002
    public required Tour Tour { get; set; }
    public required object Result { get; set; }
}
