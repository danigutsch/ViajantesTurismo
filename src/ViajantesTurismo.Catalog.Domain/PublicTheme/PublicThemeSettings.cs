using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using SharedKernel.Domain;
using SharedKernel.Results;
using ViajantesTurismo.Catalog.Contracts;
using ViajantesTurismo.Common.Sanitizers;

namespace ViajantesTurismo.Catalog.Domain.PublicTheme;

/// <summary>
/// Business-editable public website theme settings.
/// </summary>
[GenerateModelSupport(Identity = true)]
public sealed partial class PublicThemeSettings : IAggregateRoot<Guid>
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Gets the single theme row identifier.
    /// </summary>
    public static readonly Guid ThemeId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    /// <summary>
    /// DO NOT USE. This constructor is required by Entity Framework Core for materialisation.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [UsedImplicitly]
    private PublicThemeSettings()
    {
        PrimaryColor = string.Empty;
        AccentColor = string.Empty;
        BackgroundColor = string.Empty;
        TextColor = string.Empty;
        HeadingFontFamily = string.Empty;
        BodyFontFamily = string.Empty;
    }

    private PublicThemeSettings(
        string primaryColor,
        string accentColor,
        string backgroundColor,
        string textColor,
        string headingFontFamily,
        string bodyFontFamily)
    {
        Id = ThemeId;
        PrimaryColor = primaryColor;
        AccentColor = accentColor;
        BackgroundColor = backgroundColor;
        TextColor = textColor;
        HeadingFontFamily = headingFontFamily;
        BodyFontFamily = bodyFontFamily;
    }

    /// <summary>
    /// Gets the singleton theme identifier.
    /// </summary>
    public Guid Id { get; private init; }

    /// <summary>
    /// Gets the primary brand color.
    /// </summary>
    public string PrimaryColor { get; private set; }

    /// <summary>
    /// Gets the accent color.
    /// </summary>
    public string AccentColor { get; private set; }

    /// <summary>
    /// Gets the page background color.
    /// </summary>
    public string BackgroundColor { get; private set; }

    /// <summary>
    /// Gets the body text color.
    /// </summary>
    public string TextColor { get; private set; }

    /// <summary>
    /// Gets the heading font family.
    /// </summary>
    public string HeadingFontFamily { get; private set; }

    /// <summary>
    /// Gets the body font family.
    /// </summary>
    public string BodyFontFamily { get; private set; }

    /// <inheritdoc />
    public IReadOnlyCollection<IDomainEvent> GetDomainEvents() => _domainEvents.AsReadOnly();

    /// <inheritdoc />
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// Creates the default public website theme.
    /// </summary>
    /// <returns>The default public website theme.</returns>
    public static PublicThemeSettings Default()
    {
        return new PublicThemeSettings(
            PublicThemeSettingsDefaults.PrimaryColor,
            PublicThemeSettingsDefaults.AccentColor,
            PublicThemeSettingsDefaults.BackgroundColor,
            PublicThemeSettingsDefaults.TextColor,
            PublicThemeSettingsDefaults.HeadingFontFamily,
            PublicThemeSettingsDefaults.BodyFontFamily);
    }

    /// <summary>
    /// Creates public website theme settings.
    /// </summary>
    /// <returns>A valid theme settings instance, or validation errors.</returns>
    public static Result<PublicThemeSettings> Create(
        string? primaryColor,
        string? accentColor,
        string? backgroundColor,
        string? textColor,
        string? headingFontFamily,
        string? bodyFontFamily)
    {
        var errors = new ValidationErrors();
        var primary = NormalizeColor(primaryColor);
        var accent = NormalizeColor(accentColor);
        var background = NormalizeColor(backgroundColor);
        var text = NormalizeColor(textColor);
        var headingFont = NormalizeFontFamily(headingFontFamily);
        var bodyFont = NormalizeFontFamily(bodyFontFamily);

        ValidateColor(errors, nameof(PrimaryColor), primary);
        ValidateColor(errors, nameof(AccentColor), accent);
        ValidateColor(errors, nameof(BackgroundColor), background);
        ValidateColor(errors, nameof(TextColor), text);
        ValidateFontFamily(errors, nameof(HeadingFontFamily), headingFont);
        ValidateFontFamily(errors, nameof(BodyFontFamily), bodyFont);

        return errors.HasErrors
            ? errors.ToResult<PublicThemeSettings>()
            : Result.Ok(new PublicThemeSettings(primary, accent, background, text, headingFont, bodyFont));
    }

    /// <summary>
    /// Replaces the stored theme values.
    /// </summary>
    /// <returns>A result indicating whether replacement was allowed.</returns>
    public Result ReplaceWith(PublicThemeSettings theme)
    {
        ArgumentNullException.ThrowIfNull(theme);

        PrimaryColor = theme.PrimaryColor;
        AccentColor = theme.AccentColor;
        BackgroundColor = theme.BackgroundColor;
        TextColor = theme.TextColor;
        HeadingFontFamily = theme.HeadingFontFamily;
        BodyFontFamily = theme.BodyFontFamily;

        return Result.Ok();
    }

    private static string NormalizeColor(string? color)
    {
        return color is null ? string.Empty : StringSanitizer.NormalizeKey(color);
    }

    private static string NormalizeFontFamily(string? fontFamily)
    {
        var value = StringSanitizer.Sanitize(fontFamily);
        return string.IsNullOrEmpty(value)
            ? string.Empty
            : PublicThemeSettingsDefaults.AllowedFontFamilies.FirstOrDefault(
                allowed => string.Equals(allowed, value, StringComparison.OrdinalIgnoreCase)) ?? value;
    }

    private static void ValidateColor(ValidationErrors errors, string field, string value)
    {
        if (value.Length != ContractConstants.MaxCssColorLength || value[0] != '#' || !value[1..].All(Uri.IsHexDigit))
        {
            errors.Add(Result.Invalid($"{field} must be a #RRGGBB color.", field, "Use a safe #RRGGBB hex color."));
        }
    }

    private static void ValidateFontFamily(ValidationErrors errors, string field, string value)
    {
        if (!PublicThemeSettingsDefaults.AllowedFontFamilies.Contains(value, StringComparer.Ordinal))
        {
            errors.Add(Result.Invalid(
                $"{field} is not an allowed font family.",
                field,
                $"Use one of: {string.Join(", ", PublicThemeSettingsDefaults.AllowedFontFamilies)}."));
        }
    }
}
