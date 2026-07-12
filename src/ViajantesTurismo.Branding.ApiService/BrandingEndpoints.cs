using Microsoft.AspNetCore.OutputCaching;
using SharedKernel.ApiVersioning.AspNetCore;
using SharedKernel.Branding;
using SharedKernel.Results;

namespace ViajantesTurismo.Branding.ApiService;

internal static class BrandingEndpoints
{
    public static IEndpointRouteBuilder MapBrandingEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var versionedApi = app.MapApiVersionGroup(BrandingOpenApiDocuments.CurrentApiVersion);
        var publicBranding = versionedApi.MapGroup($"/{BrandingRoutes.PublicSettingsPath}")
            .AllowAnonymous();
        var managementBranding = versionedApi.MapGroup($"/{BrandingRoutes.ManagementSettingsPath}");

        publicBranding.MapGet("/", GetPublicSettings)
            .CacheOutput(policy => policy.Expire(BrandingHttpCache.PublicFreshness).Tag(BrandingHttpCache.PublicBrandingTag))
            .RequireRateLimiting(BrandingSecurityBaseline.PublicReadRateLimitPolicy);
        managementBranding.MapGet("/", GetManagementSettings)
            .RequireRateLimiting(BrandingSecurityBaseline.MutationRateLimitPolicy)
            .RequireAuthorization(BrandingAuthorization.BrandingRead);
        managementBranding.MapPut("/", SaveSettings)
            .RequireRateLimiting(BrandingSecurityBaseline.MutationRateLimitPolicy)
            .RequireAuthorization(BrandingAuthorization.BrandingWrite);

        return app;
    }

    private static async Task<IResult> GetPublicSettings(
        IBrandingSettingsStore store,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var settings = await GetSettingsOrDefault(store, ct).ConfigureAwait(false);
        BrandingHttpCache.SetPublicHeaders(httpContext);
        return Results.Ok(ToDto(settings));
    }

    private static async Task<IResult> GetManagementSettings(
        IBrandingSettingsStore store,
        HttpContext httpContext,
        CancellationToken ct)
    {
        BrandingHttpCache.SetNoStore(httpContext);
        var settings = await GetSettingsOrDefault(store, ct).ConfigureAwait(false);
        return Results.Ok(ToDto(settings));
    }

    private static async Task<IResult> SaveSettings(
        BrandingSettingsDto request,
        IBrandingSettingsStore store,
        IOutputCacheStore outputCacheStore,
        ILogger<BrandingApiHostEntryPoint> logger,
        HttpContext httpContext,
        CancellationToken ct)
    {
        BrandingHttpCache.SetNoStore(httpContext);

        var settings = BrandingSettings.Create(request, BrandingDefaults.AllowedFonts);
        if (settings.IsFailure)
        {
            return ToValidationProblem(settings.ErrorDetails);
        }

        await store.SaveSettings(settings.Value, ct).ConfigureAwait(false);
        await InvalidatePublicBrandingCache(outputCacheStore, logger, ct).ConfigureAwait(false);
        return Results.Ok(ToDto(settings.Value));
    }

    private static async Task<BrandingSettings> GetSettingsOrDefault(IBrandingSettingsStore store, CancellationToken ct)
    {
        return await store.GetSettings(ct).ConfigureAwait(false) ?? BrandingDefaults.CreateSettings();
    }

    private static async Task InvalidatePublicBrandingCache(IOutputCacheStore outputCacheStore, ILogger logger, CancellationToken ct)
    {
        await outputCacheStore.EvictByTagAsync(BrandingHttpCache.PublicBrandingTag, ct).ConfigureAwait(false);
        logger.PublicCacheAreaInvalidated(BrandingHttpCache.PublicBrandingArea);
    }

    private static BrandingSettingsDto ToDto(BrandingSettings settings)
    {
        return new BrandingSettingsDto
        {
            BrandName = settings.BrandName,
            PrimaryColor = settings.PrimaryColor,
            AccentColor = settings.AccentColor,
            BackgroundColor = settings.BackgroundColor,
            TextColor = settings.TextColor,
            HeadingFontFamily = settings.HeadingFontFamily,
            BodyFontFamily = settings.BodyFontFamily,
            LogoUri = settings.LogoUri
        };
    }

    private static IResult ToValidationProblem(ResultError error)
    {
        return Results.ValidationProblem(ToValidationProblemDictionary(error.ValidationErrors), detail: error.Detail);
    }

    private static Dictionary<string, string[]> ToValidationProblemDictionary(IReadOnlyDictionary<string, IReadOnlyList<string>>? validationErrors)
    {
        if (validationErrors is null)
        {
            return [];
        }

        var result = new Dictionary<string, string[]>(validationErrors.Count, StringComparer.Ordinal);
        foreach (var (field, messages) in validationErrors)
        {
            result[field] = [.. messages];
        }

        return result;
    }
}
