using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using PandaBear.AspNetCore.Authentication;
using PandaBear.MalwareScanning.ClamAv;
using PandaBear.Testing.AspNetCore;
using ViajantesTurismo.Admin.Api;
using ViajantesTurismo.Admin.Domain.Shared;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Admin.Testing.Behavior;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.HostedTests.Management;

/// <summary>Arranges an upgraded Admin database and an authenticated customer read API.</summary>
internal static class CustomerOptionPostgreSqlScenario
{
    /// <summary>The synthetic Admin bearer token audience.</summary>
    internal const string Audience = "admin-api";

    /// <summary>Creates customers before upgrading the database to the current migration.</summary>
    internal static async Task<IReadOnlyList<Guid>> Seed(string connectionString, int total, int matching, CancellationToken ct)
    {
        await using var context = new AdminWriteDbContextDesignTimeFactory().CreateDbContext([]);
        context.Database.SetConnectionString(connectionString);
        await context.Database.MigrateAsync("20261119182941_RecordFinalizedArtifactProvenance", ct);

        var customers = Enumerable.Range(0, total).Select(index => EntityBuilders.BuildCustomer(new CustomerOptions(
            FirstName: index < total - matching ? $"FixtureCustomer{index:D3}" : "José",
            LastName: index < total - matching ? "Example" : "Ávila",
            Email: $"choice-{index:D3}@example.test",
            PreferredBike: BikeType.EBike))).ToArray();
        context.Customers.AddRange(customers);
        await context.SaveEntities(ct);
        await context.Database.MigrateAsync(ct);
        return [.. customers.TakeLast(matching).Select(customer => customer.Id).Order()];
    }

    /// <summary>Starts the real Admin API and instruments only its read context.</summary>
    internal static WebApplicationFactory<AdminApiHostEntryPoint> Host(string connectionString, IInterceptor query) =>
        WebApplicationTestHost.Create<AdminApiHostEntryPoint>(
            environment: Environments.Development,
            configuration: new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{ResourceNames.AdminDatabase}"] = connectionString,
                [ApiAuthenticationDefaults.AuthorityConfigurationKey] = ApiTestAuthentication.Authority,
                [ApiAuthenticationDefaults.IssuerConfigurationKey] = ApiTestAuthentication.Authority,
                [ClamAvMalwareScannerConfigurationKeys.DisabledConfigurationKey] = bool.TrueString,
            },
            configureTestServices: services =>
            {
                ApiTestAuthentication.ConfigureJwtBearer(services, Audience);
                services.RemoveAll<IHostedService>();
                services.ConfigureDbContext<AdminReadDbContext>(options => options.AddInterceptors(query));
            });
}
