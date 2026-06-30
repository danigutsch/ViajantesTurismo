using ViajantesTurismo.Catalog.Domain.PublicTheme;

namespace ViajantesTurismo.Catalog.Application.PublicTheme;

/// <summary>
/// Stores public website theme settings.
/// </summary>
public interface IPublicThemeSettingsStore
{
    /// <summary>
    /// Gets the configured public website theme settings.
    /// </summary>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The configured theme settings, or <see langword="null" /> when defaults should be used.</returns>
    Task<PublicThemeSettings?> GetTheme(CancellationToken ct);

    /// <summary>
    /// Saves public website theme settings.
    /// </summary>
    /// <param name="theme">The theme settings to save.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A task that completes when theme settings have been saved.</returns>
    Task SaveTheme(PublicThemeSettings theme, CancellationToken ct);
}
