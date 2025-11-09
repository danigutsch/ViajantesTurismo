using JetBrains.Annotations;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.BehaviorTests.Context;

[UsedImplicitly]
public sealed class BookingContext
{
    public required Booking Booking { get; set; } = null!;
    public required object Result { get; set; } = null!;
    public required Action Action { get; set; } = null!;
}
