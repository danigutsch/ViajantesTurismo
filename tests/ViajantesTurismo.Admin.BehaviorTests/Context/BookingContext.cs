using JetBrains.Annotations;
using ViajantesTurismo.Admin.Domain.Bookings;
using Result = ViajantesTurismo.Common.Results.Result;

namespace ViajantesTurismo.Admin.BehaviorTests.Context;

[UsedImplicitly]
public sealed class BookingContext
{
    public required Booking Booking { get; set; }
    public required Result Result { get; set; }
    public required Action Action { get; set; }
}
