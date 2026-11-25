using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Net.Http.Headers;
using ViajantesTurismo.Branding.Contracts.Http;
using SharedKernel.ApiVersioning.AspNetCore;
using SharedKernel.Branding;
using SharedKernel.BuildingBlocks;
using SharedKernel.HttpCaching.AspNetCore;
using SharedKernel.OpenApi;
using SharedKernel.Results;
using ViajantesTurismo.Branding.Infrastructure;

namespace ViajantesTurismo.Branding.AspNetCore;

/// <summary>Maps the public and management Branding HTTP endpoints.</summary>
internal static class BrandingEndpoints
{
    /// <summary>Maps versioned public and management Branding routes with caching, rate limiting and authorization.</summary>
    public static IEndpointRouteBuilder MapBrandingEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var versionedApi = app.MapApiVersionGroup(BrandingOpenApiDocuments.CurrentApiVersion);
        versionedApi.RequireCors(BrandingSecurityBaseline.CorsPolicyName);
        var publicBranding = versionedApi.MapGroup($"/{BrandingRoutes.PublicSettingsPath}")
            .AllowAnonymous();
        var managementBranding = versionedApi.MapGroup($"/{BrandingRoutes.ManagementSettingsPath}")
            .WithNoStoreResponses();

        publicBranding.MapGet("/", GetPublicSettings)
            .WithName("GetPublicBrandingSettings")
            .CacheOutput(policy => policy.AddPolicy<BrandingCacheTelemetryPolicy>().Expire(BrandingHttpCache.PublicFreshness).Tag(BrandingHttpCache.PublicBrandingTag))
            .RequireRateLimiting(BrandingSecurityBaseline.PublicReadRateLimitPolicy);
        publicBranding.MapGet(BrandingLogoPath.Template, GetLogo)
            .WithName("GetPublicBrandingLogo")
            .CacheOutput(policy => policy
                .With(context => !context.HttpContext.Request.Headers.ContainsKey(HeaderNames.IfMatch))
                .Expire(BrandingHttpCache.PublicFreshness)
                .SetVaryByQuery([])
                // Keep the exact address even when the output cache normalizes the request path.
                .SetVaryByRouteValue(BrandingLogoPath.HashParameter)
                .SetVaryByHeader(HeaderNames.Origin))
            .Produces(StatusCodes.Status200OK, contentType: BrandingLogo.EncodedContentType)
            .Produces(StatusCodes.Status304NotModified)
            .Produces(StatusCodes.Status404NotFound)
            .RequireRateLimiting(BrandingSecurityBaseline.PublicReadRateLimitPolicy);
        managementBranding.MapGet("/", GetManagementSettings)
            .WithName("GetManagementBrandingSettings")
            .Produces<BrandingManagementSettingsDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .WithEntityTagResponseHeader()
            .RequireRateLimiting(BrandingSecurityBaseline.MutationRateLimitPolicy)
            .RequireAuthorization(BrandingAuthorization.BrandingRead);
        managementBranding.MapPut("/", SaveSettings)
            .WithName("SaveBrandingSettings")
            .Produces<BrandingManagementSettingsDto>(StatusCodes.Status200OK)
            .WithIfMatchPrecondition()
            .RequireRateLimiting(BrandingSecurityBaseline.MutationRateLimitPolicy)
            .RequireAuthorization(BrandingAuthorization.BrandingWrite);
        managementBranding.MapPut(BrandingLogoPath.ManagementTemplate, UploadLogo)
            .WithName("UploadBrandingLogo")
            .Accepts<byte[]>("image/png", "image/jpeg", "image/webp")
            .Produces<BrandingManagementSettingsDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .Produces(StatusCodes.Status504GatewayTimeout)
            .WithMetadata(new RequestSizeLimitAttribute(BrandingLogoLimits.MaxUploadBytes))
            .WithRequestTimeout(BrandingModule.LogoUploadTimeoutPolicy)
            .WithIfMatchPrecondition()
            .RequireRateLimiting(BrandingSecurityBaseline.MutationRateLimitPolicy)
            .RequireAuthorization(BrandingAuthorization.BrandingWrite);

        managementBranding.MapDelete(BrandingLogoPath.ManagementTemplate, RemoveLogo)
            .WithName("RemoveBrandingLogo")
            .Produces<BrandingManagementSettingsDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .WithIfMatchPrecondition()
            .RequireRateLimiting(BrandingSecurityBaseline.MutationRateLimitPolicy)
            .RequireAuthorization(BrandingAuthorization.BrandingWrite);

        return app;
    }

    private static async Task<IResult> GetPublicSettings(
        IBrandingSettingsStore store,
        BrandingTelemetry telemetry,
        HttpContext httpContext,
        CancellationToken ct)
    {
        telemetry.CacheMiss();
        var settings = await GetSettingsOrDefault(store, ct).ConfigureAwait(false);
        BrandingHttpCache.SetPublicHeaders(httpContext);
        return Results.Ok(ToDto(settings));
    }

    private static async Task<IResult> GetLogo(
        string sha256,
        IBrandingSettingsRevisionStore store,
        HttpContext httpContext,
        CancellationToken ct)
    {
        if (!BrandingLogoPath.IsContentAddress(sha256) || await store.GetLogo(sha256, ct).ConfigureAwait(false) is not { } logo)
        {
            return TypedResults.NotFound();
        }

        HttpCacheHeaders.SetImmutable(httpContext);
        httpContext.Response.Headers.XContentTypeOptions = "nosniff";
        return TypedResults.Bytes(logo.Content, logo.ContentType, entityTag: new EntityTagHeaderValue($"\"{logo.Sha256}\""));
    }

    private static async Task<IResult> GetManagementSettings(
        IBrandingSettingsRevisionStore store,
        BrandingTelemetry telemetry,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var current = await store.GetCurrent(ct).ConfigureAwait(false);
        if (current is null)
        {
            return TypedResults.Problem(
                "Branding revisions have not started yet.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        telemetry.ObserveRevision(current.SourceRevision);
        httpContext.Response.Headers.ETag = BrandingEntityTags.Format(current.SourceEpoch, current.SourceRevision);
        return TypedResults.Ok(ToManagementDto(current));
    }

    private static async Task<Results<Ok<BrandingManagementSettingsDto>, ProblemHttpResult>> SaveSettings(
        BrandingSettingsDto request,
        IBrandingSettingsRevisionStore store,
        IOutputCacheStore outputCacheStore,
        BrandingTelemetry telemetry,
        ILogger<BrandingAspNetCoreMarker> logger,
        HttpContext httpContext,
        CancellationToken ct)
    {
        if (!TryReadIfMatch(httpContext, out var ifMatch, out var problem))
        {
            return problem;
        }

        var settings = BrandingSettings.Create(request, BrandingDefaults.AllowedFonts);
        if (settings.IsFailure)
        {
            return ToValidationProblem(settings.ErrorDetails);
        }

        var theme = BrandingSiteTheme.Create(request);
        if (theme.IsFailure)
        {
            return ToValidationProblem(theme.ErrorDetails);
        }

        var foreignLogo = false;
        BrandingSettingsSnapshot? saved;
        try
        {
            saved = await store.SaveIfMatch(
                settings.Value,
                current =>
                {
                    foreignLogo = !KeepsOrClearsLogo(current, settings.Value);
                    return !foreignLogo && Matches(ifMatch, current);
                },
                ct).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            saved = null;
        }

        if (foreignLogo)
        {
            return ForeignLogoProblem();
        }

        if (saved is null)
        {
            return StalePreconditionProblem();
        }

        telemetry.ObserveRevision(saved.SourceRevision);
        await InvalidatePublicBrandingCache(outputCacheStore, telemetry, logger).ConfigureAwait(false);
        return TypedResults.Ok(ToManagementDto(saved));
    }

    private static async Task<Results<Ok<BrandingManagementSettingsDto>, ProblemHttpResult>> UploadLogo(
        BrandingLogoIntake intake,
        IBrandingSettingsRevisionStore store,
        IOutputCacheStore outputCacheStore,
        BrandingTelemetry telemetry,
        ILogger<BrandingAspNetCoreMarker> logger,
        HttpContext httpContext,
        CancellationToken ct)
    {
        if (!TryReadIfMatch(httpContext, out var ifMatch, out var problem))
        {
            return problem;
        }

        if (await HasStaleSnapshot(store, ifMatch, ct).ConfigureAwait(false))
        {
            return StalePreconditionProblem();
        }

        var logo = await intake.Process(httpContext.Request.Body, httpContext.Request.ContentType, ct).ConfigureAwait(false);
        if (logo.IsFailure)
        {
            return logo.Status is ResultStatus.Invalid
                ? ToValidationProblem(logo.ErrorDetails)
                : TypedResults.Problem(logo.ErrorDetails.Detail, statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        return await ReplaceLogo(logo.Value, ifMatch, store, outputCacheStore, telemetry, logger, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Cheap fail-fast read so a stale tag skips the scan and re-encode. The store re-checks the precondition when it writes,
    /// and a missing snapshot is left to the store to report.
    /// </summary>
    private static async Task<bool> HasStaleSnapshot(IBrandingSettingsRevisionStore store, IfMatchCondition ifMatch, CancellationToken ct) =>
        await store.GetCurrent(ct).ConfigureAwait(false) is { } current && !Matches(ifMatch, current);

    private static ProblemHttpResult StalePreconditionProblem() =>
        PreconditionProblems.Failed(
            "Branding settings changed after they were loaded. Reload them and reapply your changes.",
            ProblemCode(BrandingManagementProblemCodes.PreconditionFailed));

    private static async Task<Results<Ok<BrandingManagementSettingsDto>, ProblemHttpResult>> RemoveLogo(
        IBrandingSettingsRevisionStore store,
        IOutputCacheStore outputCacheStore,
        BrandingTelemetry telemetry,
        ILogger<BrandingAspNetCoreMarker> logger,
        HttpContext httpContext,
        CancellationToken ct)
    {
        if (!TryReadIfMatch(httpContext, out var ifMatch, out var problem))
        {
            return problem;
        }

        return await ReplaceLogo(null, ifMatch, store, outputCacheStore, telemetry, logger, ct).ConfigureAwait(false);
    }

    private static async Task<Results<Ok<BrandingManagementSettingsDto>, ProblemHttpResult>> ReplaceLogo(
        BrandingLogo? logo,
        IfMatchCondition ifMatch,
        IBrandingSettingsRevisionStore store,
        IOutputCacheStore outputCacheStore,
        BrandingTelemetry telemetry,
        ILogger<BrandingAspNetCoreMarker> logger,
        CancellationToken ct)
    {
        var unavailable = false;
        BrandingSettingsSnapshot? saved;
        try
        {
            saved = await store.ReplaceLogoIfMatch(
                logo,
                current =>
                {
                    unavailable = current is null;
                    return !unavailable && Matches(ifMatch, current);
                },
                ct).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            saved = null;
        }

        if (unavailable)
        {
            return TypedResults.Problem(
                "Branding revisions have not started yet.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        if (saved is null)
        {
            return StalePreconditionProblem();
        }

        telemetry.ObserveRevision(saved.SourceRevision);
        await InvalidatePublicBrandingCache(outputCacheStore, telemetry, logger).ConfigureAwait(false);
        return TypedResults.Ok(ToManagementDto(saved));
    }

    private static bool TryReadIfMatch(
        HttpContext httpContext,
        [NotNullWhen(true)] out IfMatchCondition? ifMatch,
        [NotNullWhen(false)] out ProblemHttpResult? problem)
    {
        ifMatch = null;
        problem = null;
        if (!httpContext.Request.Headers.TryGetValue(HeaderNames.IfMatch, out var ifMatchValues))
        {
            problem = PreconditionProblems.Required(
                "Send If-Match with the entity tag from the latest Branding read.",
                ProblemCode(BrandingManagementProblemCodes.PreconditionRequired));
            return false;
        }

        if (!IfMatchCondition.TryParse(ifMatchValues, out ifMatch))
        {
            problem = PreconditionProblems.InvalidIfMatch(extensions: ProblemCode(BrandingManagementProblemCodes.InvalidIfMatch));
            return false;
        }

        return true;
    }

    private static bool Matches(IfMatchCondition ifMatch, BrandingSettingsSnapshot? current) =>
        current is not null && ifMatch.Matches(BrandingEntityTags.Format(current.SourceEpoch, current.SourceRevision));

    private static KeyValuePair<string, object?>[] ProblemCode(string code) =>
        [new(BrandingManagementProblemCodes.ExtensionName, code)];

    private static bool KeepsOrClearsLogo(BrandingSettingsSnapshot? current, BrandingSettings requested) =>
        requested.LogoUri is null || (current is not null && string.Equals(requested.LogoUri, current.Settings.LogoUri, StringComparison.Ordinal));

    private static ProblemHttpResult ForeignLogoProblem() =>
        TypedResults.Problem(TypedResults.ValidationProblem(
            new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                [nameof(BrandingSettingsDto.LogoUri)] =
                    ["The logo can only be replaced by uploading an image, or removed by leaving the logo URI empty."]
            },
            detail: "Branding settings are invalid.").ProblemDetails);

    private static BrandingManagementSettingsDto ToManagementDto(BrandingSettingsSnapshot snapshot) => new()
    {
        BrandName = snapshot.Settings.BrandName,
        PrimaryColor = snapshot.Settings.PrimaryColor,
        AccentColor = snapshot.Settings.AccentColor,
        BackgroundColor = snapshot.Settings.BackgroundColor,
        TextColor = snapshot.Settings.TextColor,
        HeadingFontFamily = snapshot.Settings.HeadingFontFamily,
        BodyFontFamily = snapshot.Settings.BodyFontFamily,
        LogoUri = snapshot.Settings.LogoUri is null ? null : new Uri(snapshot.Settings.LogoUri, UriKind.RelativeOrAbsolute),
        SourceEpoch = snapshot.SourceEpoch,
        SourceRevision = snapshot.SourceRevision
    };

    private static async Task<BrandingSettings> GetSettingsOrDefault(IBrandingSettingsStore store, CancellationToken ct)
    {
        return await store.GetSettings(ct).ConfigureAwait(false) ?? BrandingDefaults.CreateSettings();
    }

    private static async Task InvalidatePublicBrandingCache(
        IOutputCacheStore outputCacheStore,
        BrandingTelemetry telemetry,
        ILogger logger)
    {
        try
        {
            await outputCacheStore
                .EvictByTagAsync(BrandingHttpCache.PublicBrandingTag, CancellationToken.None)
                .ConfigureAwait(false);
            telemetry.Invalidation(succeeded: true);
            logger.PublicCacheAreaInvalidated(BrandingHttpCache.PublicBrandingArea);
        }
        catch (Exception exception) when (exception.ShouldHandleAsFailure(CancellationToken.None))
        {
            telemetry.Invalidation(succeeded: false);
            logger.PublicCacheAreaInvalidationFailed(
                BrandingHttpCache.PublicBrandingArea,
                exception.GetType().Name);
        }
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

    /// <summary>ProblemHttpResult avoids adding a new published 400 response while preserving the validation payload.</summary>
    private static ProblemHttpResult ToValidationProblem(ResultError error)
    {
        var validationProblem = TypedResults.ValidationProblem(ToValidationProblemDictionary(error.ValidationErrors), detail: error.Detail);
        return TypedResults.Problem(validationProblem.ProblemDetails);
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
