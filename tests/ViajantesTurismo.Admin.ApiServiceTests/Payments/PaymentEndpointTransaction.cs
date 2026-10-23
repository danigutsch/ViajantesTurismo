using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Payments;

namespace ViajantesTurismo.Admin.ApiServiceTests.Payments;

internal sealed class PaymentEndpointTransaction : IPaymentTransaction, IPaymentHistoryStore
{
    public Task Save(Domain.Tours.Tour tour, Guid bookingId, CancellationToken ct) => throw new InvalidOperationException("The HTTP boundary must not execute persistence in this probe.");
    public Guid ResultId
    {
        get; init;
    }
    public int Status { get; init; } = 200;
    public int Calls
    {
        get; private set;
    }
    public Task<Result<Guid>> Execute(IReadOnlyList<string> lockNames, Func<Task<Result<Guid>>> mutation, CancellationToken ct)
    {
        Calls++;
        return Task.FromResult(Status switch
        {
            200 => Result.Ok(ResultId),
            404 => Result.NotFound(detail: "Missing financial record.").ConvertError<Guid>(),
            409 => Result.Conflict(detail: "Revision changed.").ConvertError<Guid>(),
            _ => Result.Invalid(detail: "Rejected financial command.", field: "record", message: "Rejected financial command.").ConvertError<Guid>()
        });
    }
}
