using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViajantesTurismo.Catalog.Contracts.Http;
using ViajantesTurismo.Catalog.Domain.PublicContent;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.HostedTests.Management.Browser;

namespace ViajantesTurismo.HostedTests.Management;

/// <summary>Seeds actual editable content and connects an authenticated Catalog API to its database.</summary>
internal static class PublicContentPagePostgreSqlScenario
{
    /// <summary>Migrates the database and creates entries with two substantial localized bodies each.</summary>
    internal static async Task Seed(string connectionString, int count, bool addPageKey, CancellationToken ct)
    {
        await using var database = new CatalogDbContextDesignTimeFactory().CreateDbContext([]);
        database.Database.SetConnectionString(connectionString);
        await database.Database.MigrateAsync(ct);
        var body = "Publication body " + new string('x', 3500);
        for (var index = 0; index < count; index++)
        {
            var english = PublicContentVariant.Create(PublicContentCultures.EnglishUnitedStates, "Public content",
                body, null, null, null, false).Value;
            var portuguese = PublicContentVariant.Create(PublicContentCultures.PortugueseBrazil, "Conteúdo público",
                body, null, null, null, index % 2 != 0).Value;
            var content = EditablePublicContent.Create($"catalog.entry.{index:D4}", PublicContentCultures.EnglishUnitedStates,
                [english, portuguese]).Value;
            content.PublishIfReady().IsSuccess.ShouldBeTrue();
            database.PublicContent.Add(content);
        }

        if (addPageKey)
        {
            var english = PublicContentVariant.Create(PublicContentCultures.EnglishUnitedStates, "Existing Page",
                body, null, null, null, false).Value;
            var portuguese = PublicContentVariant.Create(PublicContentCultures.PortugueseBrazil, "Página existente",
                body, null, null, null, false).Value;
            var entry = EditablePublicContent.Create("page", PublicContentCultures.EnglishUnitedStates, [english, portuguese]).Value;
            database.PublicContent.Add(entry);
        }

        await database.SaveChangesAsync(ct);
    }

    /// <summary>Configures the real Catalog API store to use the fixture's PostgreSQL database and query probe.</summary>
    internal static void Configure(IWebHostBuilder builder, string connectionString, PublicContentPageQueryProbe probe)
    {
        builder.ConfigureTestServices(services => services.Replace(ServiceDescriptor.Scoped(_ =>
        {
            var options = new DbContextOptionsBuilder<CatalogDbContext>()
                .UseNpgsql(connectionString)
                .AddInterceptors(probe)
                .Options;
            return new CatalogDbContext(options);
        })));
    }

    /// <summary>Starts the normal Management route with its typed client pointed at the Catalog API under test.</summary>
    internal static Task<ManagementWebBrowserHost> StartManagement(HttpClient catalogHttp, CancellationToken ct)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IPublicContentApiClient>(new PublicContentApiClient(catalogHttp));
        return ManagementWebBrowserHost.Start(services, ct);
    }
}
