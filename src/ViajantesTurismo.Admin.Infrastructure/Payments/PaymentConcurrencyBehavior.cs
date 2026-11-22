using Microsoft.EntityFrameworkCore;
using PandaBear.Mediator;
using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Payments;

namespace ViajantesTurismo.Admin.Infrastructure.Payments;

/// <summary>Translates optimistic concurrency failures before the payment transaction decides whether to roll back.</summary>
/// <typeparam name="TRequest">The payment command type.</typeparam>
/// <typeparam name="TResponse">The payment result type.</typeparam>
[PipelineOrder(PipelineStage.Transaction, Order = 1)]
public sealed class PaymentConcurrencyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>, IPaymentCommand
    where TResponse : struct, IResultLike<TResponse>
{
    /// <inheritdoc />
    public async ValueTask<TResponse> Handle(TRequest request, RequestHandlerContinuation<TResponse> next, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(next);

        try
        {
            return await next();
        }
        catch (DbUpdateConcurrencyException)
        {
            var conflict = Result.Conflict(detail: "The payment record changed. Reload and confirm the current revision.").ErrorDetails;
            ArgumentNullException.ThrowIfNull(conflict);
            return TResponse.FromError(conflict);
        }
    }
}
