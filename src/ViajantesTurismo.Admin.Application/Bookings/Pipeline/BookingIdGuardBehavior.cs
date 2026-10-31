using PandaBear.Mediator;
using PandaBear.Results;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Application.Bookings.Pipeline;

/// <summary>
/// Short-circuits a booking command that targets <see cref="Guid.Empty"/> with the not-found result
/// the handler would return, so no store lookup or capacity lease is attempted for an identifier that cannot exist.
/// </summary>
/// <typeparam name="TRequest">The booking-scoped command type.</typeparam>
/// <typeparam name="TResponse">The command response type.</typeparam>
[PipelineOrder(PipelineStage.Validation, Order = 1)]
public sealed class BookingIdGuardBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>, IBookingScopedCommand
    where TResponse : struct, IResultLike<TResponse>
{
    /// <inheritdoc />
    public ValueTask<TResponse> Handle(TRequest request, RequestHandlerContinuation<TResponse> next, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(next);

        if (request.BookingId != Guid.Empty)
        {
            return next();
        }

        var notFound = BookingErrors.BookingNotFound().ErrorDetails;
        ArgumentNullException.ThrowIfNull(notFound);

        return ValueTask.FromResult(TResponse.FromError(notFound));
    }
}
