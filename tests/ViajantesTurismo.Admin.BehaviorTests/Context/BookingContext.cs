using JetBrains.Annotations;
using ViajantesTurismo.Admin.Domain.Bookings;
using Result = ViajantesTurismo.Common.Results.Result;

namespace ViajantesTurismo.Admin.BehaviorTests.Context;

[UsedImplicitly]
public sealed class BookingContext
{
    public Booking Booking { get; set; } = null!;
    public Result Result { get; set; }
    public Action Action { get; set; } = null!;
}
