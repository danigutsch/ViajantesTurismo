using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharedKernel.MalwareScanning.ClamAv;
using ViajantesTurismo.Catalog.Application.PublicContent;
using ViajantesTurismo.Catalog.Domain.PublicContent;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.HostedTests.Migrations;

/// <summary>Reads the restored snapshot through the production Catalog store registration.</summary>
internal sealed class CatalogMigrationRehearsalReader : IAsyncDisposable
{
    private readonly ServiceProvider services;

    /// <summary>Builds the Catalog infrastructure with the rehearsal runtime login.</summary>
    internal CatalogMigrationRehearsalReader(string connectionString)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { EnvironmentName = Environments.Development });
        builder.Logging.ClearProviders();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            [$"ConnectionStrings:{ResourceNames.CatalogDatabase}"] = connectionString,
            [ClamAvMalwareScannerConfigurationKeys.DisabledConfigurationKey] = bool.TrueString
        });
        builder.AddCatalogInfrastructure(addOutboxRelay: false);
        services = builder.Services.BuildServiceProvider();
    }

    /// <summary>Reads the public variant selected by the normal application culture resolver.</summary>
    internal async Task<(string Language, string Title, string Body)> ReadPortuguese(CancellationToken ct)
    {
        await using var scope = services.CreateAsyncScope();
        var content = await scope.ServiceProvider.GetRequiredService<IPublicContentStore>().GetContent("home.rehearsal", ct);
        if (content is null || !content.IsPubliclyVisible
            || !content.TryResolvePublicVariant(PublicContentCultures.PortugueseBrazil, out var variant))
        {
            throw new InvalidOperationException("The restored Catalog public content cannot be read.");
        }

        return (variant.Language.Value, variant.Title, variant.Body);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() => await services.DisposeAsync();
}
