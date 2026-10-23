using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Diagnostics;

namespace ViajantesTurismo.Public.WebTests;

internal static class PublicWebEndpointTestsHelpers
{
    public static TourDetailsDto CreateTour(string slug, string title)
    {
        return new TourDetailsDto
        {
            Title = title,
            Slug = slug,
            Summary = $"Discover {title} by bicycle.",
            Description = $"A detailed description of {title}.",
            Itinerary = $"Day one: explore {title}.",
            Images = [],
            UpdatedAt = TimeProvider.System.GetUtcNow()
        };
    }

    public static WebApplicationFactory<IPublicWebAssemblyMarker> CreateFactory(
            IPublicCatalogApiClient? catalogApiClient = null,
            IBrandingApiClient? brandingApiClient = null,
            string? environment = null,
            string? canonicalOrigin = null,
            IReadOnlyDictionary<string, string?>? configuration = null)
    {
        var settings = configuration?.ToDictionary(static pair => pair.Key, static pair => pair.Value);
        if (canonicalOrigin is not null)
        {
            settings ??= [];
            settings[$"{PublicWebSitemapOptions.SectionName}:CanonicalOrigin"] = canonicalOrigin;
        }

        return WebApplicationTestHost.Create<IPublicWebAssemblyMarker>(
            environment,
            services =>
            {
                services.RemoveAll<IPublicCatalogApiClient>();
                services.RemoveAll<IBrandingApiClient>();
                services.AddSingleton(catalogApiClient ?? new FakePublicCatalogApiClient());
                services.AddSingleton(brandingApiClient ?? new FakeBrandingApiClient());
            },
            configuration: settings);
    }

    public static async Task<(int ExitCode, string Output)> RunProductionStartup(string canonicalOrigin, CancellationToken ct)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = AppContext.BaseDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add(typeof(IPublicWebAssemblyMarker).Assembly.Location);
        startInfo.ArgumentList.Add("--environment=Production");
        startInfo.ArgumentList.Add("--urls=http://127.0.0.1:0");
        startInfo.ArgumentList.Add("--PublicWeb:Sitemap:CanonicalOrigin=" + canonicalOrigin);

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            throw new InvalidOperationException("The production public web process did not start.");
        }

        var standardOutput = process.StandardOutput.ReadToEndAsync(ct);
        var standardError = process.StandardError.ReadToEndAsync(ct);
        try
        {
            await process.WaitForExitAsync(ct).WaitAsync(TimeSpan.FromSeconds(30), ct);
            return (process.ExitCode, await standardOutput + await standardError);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(CancellationToken.None);
            }
        }
    }
}
