using JetBrains.Annotations;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.BehaviorTests.Context;

[UsedImplicitly]
public sealed class BookingCustomerContext
{
    public Result<BookingCustomer>? BookingCustomerCreationResult { get; set; }
}
