using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PandaBear.Mediator;
using PandaBear.Mediator.EntityFrameworkCore;
using PandaBear.Results;

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
    : ResultTransactionBehavior<TRequest, TResponse>(dbContext, scopeFactory, logger)
    where TRequest : ICommand<TResponse>
    where TResponse : struct, IResultLike<TResponse>;
