using ViajantesTurismo.Catalog.Application.PublicTheme;
using ViajantesTurismo.Catalog.Domain.PublicTheme;

namespace ViajantesTurismo.Catalog.Testing.Infrastructure;

public sealed class TestPublicThemeSettingsStore : IPublicThemeSettingsStore
{
    private PublicThemeSettings? theme;

    public Task<PublicThemeSettings?> GetTheme(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(theme);
    }

    public Task SaveTheme(PublicThemeSettings theme, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        this.theme = theme;
        return Task.CompletedTask;
    }
}
