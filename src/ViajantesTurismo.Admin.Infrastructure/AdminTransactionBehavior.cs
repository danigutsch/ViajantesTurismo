using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PandaBear.Mediator;
using PandaBear.Mediator.EntityFrameworkCore;
using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Pipeline;

namespace ViajantesTurismo.Admin.Infrastructure;

/// <summary>Runs opted-in Admin commands in the write context's transaction.</summary>
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
    where TRequest : ICommand<TResponse>, ITransactionalAdminCommand
    where TResponse : struct, IResultLike<TResponse>;
