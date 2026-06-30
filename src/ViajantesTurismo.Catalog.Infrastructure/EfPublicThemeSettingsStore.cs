using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Catalog.Application.PublicTheme;
using ViajantesTurismo.Catalog.Domain.PublicTheme;

namespace ViajantesTurismo.Catalog.Infrastructure;

internal sealed class EfPublicThemeSettingsStore(CatalogDbContext dbContext) : IPublicThemeSettingsStore
{
    public async Task<PublicThemeSettings?> GetTheme(CancellationToken ct)
    {
        return await dbContext.PublicThemeSettings
            .SingleOrDefaultAsync(theme => theme.Id == PublicThemeSettings.ThemeId, ct)
            .ConfigureAwait(false);
    }

    public async Task SaveTheme(PublicThemeSettings theme, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(theme);

        var existing = await dbContext.PublicThemeSettings
            .SingleOrDefaultAsync(current => current.Id == PublicThemeSettings.ThemeId, ct)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            existing.ReplaceWith(theme);
            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
            return;
        }

        dbContext.PublicThemeSettings.Add(theme);
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
