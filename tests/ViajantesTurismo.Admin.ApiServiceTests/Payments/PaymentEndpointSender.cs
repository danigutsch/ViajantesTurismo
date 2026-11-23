using PandaBear.Mediator;
using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Payments;

namespace ViajantesTurismo.Admin.ApiServiceTests.Payments;

/// <summary>Records payment dispatches and provides the outcomes needed for HTTP mapping tests.</summary>
internal sealed class PaymentEndpointSender : ISender
{
    /// <summary>Gets the ID returned for a successful command.</summary>
    public Guid ResultId
    {
        get;
        init;
    }

    /// <summary>Gets the outcome to simulate.</summary>
    public int Status { get; init; } = 200;

    /// <summary>Gets the number of payment commands received.</summary>
    public int Calls
    {
        get;
        private set;
    }

    /// <inheritdoc />
    public ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct)
    {
        if (request is not IPaymentCommand)
        {
            throw new InvalidOperationException("The payment endpoint sent an unexpected command.");
        }

        Calls++;
        var result = Status switch
        {
            200 => Result.Ok(ResultId),
            404 => Result.NotFound(detail: "Missing financial record.").ConvertError<Guid>(),
            409 => Result.Conflict(detail: "Revision changed.").ConvertError<Guid>(),
            503 => Result.Unavailable(detail: "Instruction generation is unavailable.").ConvertError<Guid>(),
            _ => Result.Invalid(detail: "Rejected financial command.", field: "record", message: "Rejected financial command.").ConvertError<Guid>()
        };
        return ValueTask.FromResult((TResponse)(object)result);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<TResponse> Send<TResponse>(IStreamRequest<TResponse> request, CancellationToken ct) =>
        throw new NotSupportedException();
}
