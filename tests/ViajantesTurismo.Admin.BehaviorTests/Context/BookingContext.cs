using JetBrains.Annotations;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.BehaviorTests.Context;

[UsedImplicitly]
public sealed class BookingContext
{
    public required Booking Booking { get; set; }

    public Result<Booking>? BookingCreationResult { get; set; }
    public Result? BookingOperationResult { get; set; }
    public Result<BookingCustomer>? BookingCustomerResult { get; set; }

    public required object Result { get; set; }
    public required Action Action { get; set; }
}
