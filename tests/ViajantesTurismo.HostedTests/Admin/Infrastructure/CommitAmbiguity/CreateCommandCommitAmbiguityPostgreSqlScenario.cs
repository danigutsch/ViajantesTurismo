using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharedKernel.Mediator;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Customers.CreateCustomer;
using ViajantesTurismo.Admin.Application.Tours.CreateTour;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.CommitAmbiguity;

/// <summary>Exercises the production create handlers with the Admin transaction pipeline and a real commit.</summary>
internal static class CreateCommandCommitAmbiguityPostgreSqlScenario
{
    /// <summary>Creates a tour and reads the durable identifiers before and after the replay.</summary>
    internal static Task<(Result<Guid> Result, int Attempts, Guid[] CommittedIds, Guid[] FinalIds)> CreateTour(
        string connectionString, CreateTourCommand command, CancellationToken ct)
    {
        return Execute(connectionString, command, services => new CreateTourCommandHandler(
            services.GetRequiredService<ITourStore>(), services.GetRequiredService<IUnitOfWork>()),
            reader => reader.Tours.Select(tour => tour.Id), ct);
    }

    /// <summary>Creates a customer and reads the durable identifiers before and after the replay.</summary>
    internal static Task<(Result<Guid> Result, int Attempts, Guid[] CommittedIds, Guid[] FinalIds)> CreateCustomer(
        string connectionString, CreateCustomerCommand command, CancellationToken ct)
    {
        return Execute(connectionString, command, services => new CreateCustomerCommandHandler(
            services.GetRequiredService<ICustomerStore>(), services.GetRequiredService<IUnitOfWork>(),
            services.GetRequiredService<TimeProvider>()),
            reader => reader.Customers.Select(customer => customer.Id), ct);
    }

    private static async Task<(Result<Guid> Result, int Attempts, Guid[] CommittedIds, Guid[] FinalIds)> Execute<TCommand>(
        string connectionString,
        TCommand command,
        Func<IServiceProvider, ICommandHandler<TCommand, Result<Guid>>> createHandler,
        Func<AdminWriteDbContext, IQueryable<Guid>> selectIds,
        CancellationToken ct)
        where TCommand : ICommand<Result<Guid>>
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });
        builder.Configuration[$"ConnectionStrings:{ResourceNames.AdminDatabase}"] = connectionString;
        builder.Services.AddSingleton(TimeProvider.System);
        builder.AddApplication();
        builder.AddInfrastructure(addRuntimeBackgroundServices: false);
        var interceptor = new CommittedTimeoutInterceptor(connectionString, selectIds);
        builder.Services.ConfigureDbContext<AdminWriteDbContext>(options => options.AddInterceptors(interceptor));

        await using var provider = builder.Services.BuildServiceProvider();
        await using var writeScope = provider.CreateAsyncScope();
        var services = writeScope.ServiceProvider;
        var db = services.GetRequiredService<AdminWriteDbContext>();
        await db.Database.MigrateAsync(ct);

        var handler = createHandler(services);
        var behavior = new AdminTransactionBehavior<TCommand, Result<Guid>>(
            db, services.GetRequiredService<IServiceScopeFactory>(),
            services.GetRequiredService<ILogger<AdminTransactionBehavior<TCommand, Result<Guid>>>>());
        var attempts = 0;
        interceptor.Arm();
        var result = await behavior.Handle(command, () =>
        {
            attempts++;
            return handler.Handle(command, ct);
        }, ct);

        var finalIds = await interceptor.ReadIds(ct);
        return (result, attempts, interceptor.CommittedIds, finalIds);
    }
}
