using Microsoft.Extensions.DependencyInjection;
using ViajantesTurismo.Branding.Infrastructure;

namespace ViajantesTurismo.Admin.IntegrationTests.Branding;

/// <summary>One Branding settings store with its own PostgreSQL unit of work.</summary>
internal sealed class BrandingStoreLease(ServiceProvider provider, AsyncServiceScope scope, EfBrandingSettingsStore store)
    : IAsyncDisposable
{
    public EfBrandingSettingsStore Store { get; } = store;

    public async ValueTask DisposeAsync()
    {
        await scope.DisposeAsync();
        await provider.DisposeAsync();
    }
}
