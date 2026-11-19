using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PandaBear.Results;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Bookings.CreateBooking;
using ViajantesTurismo.Admin.Application.Tours;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Admin.Testing.Behavior;
using ViajantesTurismo.HostedTests.Admin.Infrastructure.CommitAmbiguity;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Tours;

/// <summary>Runs booking creation through the Admin transaction behavior using a test-owned PostgreSQL database.</summary>
internal static class BookingCommitAmbiguityPostgreSqlScenario
{
    /// <summary>Returns the command result, attempt count, and booking identifiers seen before and after the retry.</summary>
    internal static async Task<(Result<Guid> Result, int Attempts, Guid[] CommittedBookingIds, Guid[] FinalBookingIds)> CreateBooking(
        string connectionString,
        CancellationToken ct)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });
        builder.Configuration[$"ConnectionStrings:{ResourceNames.AdminDatabase}"] = connectionString;
        builder.Services.AddSingleton(TimeProvider.System);
        builder.AddApplication();
        builder.AddInfrastructure(addRuntimeBackgroundServices: false);
        var observerOptions = new DbContextOptionsBuilder<AdminWriteDbContext>().UseNpgsql(connectionString).Options;
        var interceptor = new CommittedTimeoutInterceptor(observerOptions,
            reader => reader.Set<Booking>().Select(booking => booking.Id));
        builder.Services.ConfigureDbContext<AdminWriteDbContext>(options => options.AddInterceptors(interceptor));

        await using var provider = builder.Services.BuildServiceProvider();
        await using var writeScope = provider.CreateAsyncScope();
        var services = writeScope.ServiceProvider;
        var db = services.GetRequiredService<AdminWriteDbContext>();
        await db.Database.MigrateAsync(ct);
        var tour = EntityBuilders.BuildTour();
        var customer = EntityBuilders.BuildCustomer(null);
        db.Tours.Add(tour);
        db.Customers.Add(customer);
        await db.SaveEntities(ct);

        var command = new CreateBookingCommand(tour.Id, customer.Id, BikeTypeDto.Regular,
            null, null, RoomTypeDto.DoubleOccupancy, DiscountTypeDto.None, 0m, null, null);
        var handler = new CreateBookingCommandHandler(services.GetRequiredService<ITourCommandStore>(),
            services.GetRequiredService<ICustomerStore>(), db, services.GetRequiredService<TimeProvider>());
        var behavior = new AdminTransactionBehavior<CreateBookingCommand, Result<Guid>>(
            db, services.GetRequiredService<IServiceScopeFactory>(),
            services.GetRequiredService<ILogger<AdminTransactionBehavior<CreateBookingCommand, Result<Guid>>>>());

        var attempts = 0;
        interceptor.Arm();
        var result = await behavior.Handle(command, () =>
        {
            attempts++;
            return handler.Handle(command, ct);
        }, ct);

        var finalBookingIds = await interceptor.ReadIds(ct);
        return (result, attempts, interceptor.CommittedIds, finalBookingIds);
    }
}
