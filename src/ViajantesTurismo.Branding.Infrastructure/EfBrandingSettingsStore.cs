using Microsoft.EntityFrameworkCore;
using SharedKernel.Branding;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Branding.Infrastructure;

internal sealed class EfBrandingSettingsStore(BrandingDbContext dbContext) : IBrandingSettingsStore
{
    public async Task<BrandingSettings?> GetSettings(CancellationToken ct)
    {
        var entity = await dbContext.BrandingSettings
            .SingleOrDefaultAsync(settings => settings.Id == BrandingSettingsRecord.SettingsId, ct)
            .ConfigureAwait(false);

        return entity?.ToSettings(BrandingFontFamilies.All);
    }

    public async Task SaveSettings(BrandingSettings settings, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var existing = await dbContext.BrandingSettings
            .SingleOrDefaultAsync(current => current.Id == BrandingSettingsRecord.SettingsId, ct)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            existing.ReplaceWith(settings);
            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
            return;
        }

        dbContext.BrandingSettings.Add(BrandingSettingsRecord.FromSettings(settings));
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
