using SharedKernel.Mediator.EntityFrameworkCore;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Application.Documents;

namespace ViajantesTurismo.Admin.Infrastructure.Documents;

/// <summary>Persists document rejection evidence in the transaction behavior's fresh scope.</summary>
/// <typeparam name="TCommand">The document decision with deferred audit evidence.</typeparam>
/// <typeparam name="TResponse">The result returned by the document command.</typeparam>
/// <param name="writer">The audit writer for the fresh scope.</param>
internal sealed class DocumentRejectionRollbackAudit<TCommand, TResponse>(DocumentAuditWriter writer)
    : IAfterRollback<TCommand, TResponse>
    where TCommand : DocumentRejectionAuditedCommand<TResponse>
    where TResponse : struct, IResultLike<TResponse>
{
    /// <inheritdoc />
    public async ValueTask Handle(TCommand request, TResponse response, CancellationToken ct)
    {
        if (request.TakeRejectionAudit() is not { } rejection)
        {
            return;
        }

        var audit = await writer.AddRejected(request.AuditContext, request.AuditOperation, rejection, ct);
        if (audit.IsFailure)
        {
            throw new InvalidOperationException("The document rejection audit was invalid.");
        }
    }
}
