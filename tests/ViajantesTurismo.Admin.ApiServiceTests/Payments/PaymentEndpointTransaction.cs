using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Payments;

namespace ViajantesTurismo.Admin.ApiServiceTests.Payments;

/// <summary>Payment transaction and history store fake that returns a configured HTTP-like status without persisting.</summary>
internal sealed class PaymentEndpointTransaction : IPaymentTransaction, IPaymentHistoryStore
{
    /// <summary>Always throws because the HTTP boundary must not persist.</summary>
    public Task Save(Domain.Tours.Tour tour, Guid bookingId, CancellationToken ct) => throw new InvalidOperationException("The HTTP boundary must not execute persistence in this probe.");
    /// <summary>Gets the id returned when Status is 200.</summary>
    public Guid ResultId
    {
        get; init;
    }
    /// <summary>Gets the status to simulate: 200, 404, 409 or any other value for an invalid result.</summary>
    public int Status { get; init; } = 200;
    /// <summary>Gets how many times Execute ran.</summary>
    public int Calls
    {
        get; private set;
    }
    /// <summary>Counts the call and returns a result matching Status without running the mutation.</summary>
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
