using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PandaBear.Mediator;
using PandaBear.Mediator.EntityFrameworkCore;
using PandaBear.Results;
using PortableLockDeclaration = PandaBear.Mediator.Transactions.ITransactionLockDeclaration;

namespace ViajantesTurismo.Admin.Infrastructure;

/// <summary>Runs Admin commands that return a result in a transaction on the Admin write context.</summary>
/// <remarks>Commands that insert rows must retain their client-generated identifiers across transaction attempts.</remarks>
/// <typeparam name="TRequest">The Admin command.</typeparam>
/// <typeparam name="TResponse">The command result.</typeparam>
/// <param name="dbContext">The Admin write database context.</param>
/// <param name="scopeFactory">Creates a scope for transaction outcome hooks.</param>
/// <param name="logger">Logs unhandled outcome hook failures.</param>
[PipelineOrder(PipelineStage.Transaction)]
public sealed class AdminTransactionBehavior<TRequest, TResponse>(
    DbContext dbContext,
    IServiceScopeFactory scopeFactory,
    ILogger<AdminTransactionBehavior<TRequest, TResponse>> logger)
    : ResultTransactionBehavior<TRequest, TResponse>(dbContext, scopeFactory, logger), IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
    where TResponse : struct, IResultLike<TResponse>
{
    private readonly DbContext context = dbContext;

    /// <inheritdoc />
    public new ValueTask<TResponse> Handle(TRequest request, RequestHandlerContinuation<TResponse> next, CancellationToken ct)
    {
        if ((request is PortableLockDeclaration or ITransactionLockDeclaration) && context.Database.CurrentTransaction is not null)
        {
            throw new InvalidOperationException("A command with declared transaction locks requires a new transaction.");
        }

        return base.Handle(request, next, ct);
    }
}
