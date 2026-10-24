using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi;
using ViajantesTurismo.Branding.Contracts.Http;
using PandaBear.ApiVersioning.AspNetCore;
using PandaBear.Branding;
using PandaBear.BuildingBlocks;
using PandaBear.HttpCaching.AspNetCore;
using PandaBear.Results;
using ViajantesTurismo.Branding.Infrastructure;

namespace ViajantesTurismo.Branding.ApiService;

internal static class BrandingEndpoints
{
    public static IEndpointRouteBuilder MapBrandingEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var versionedApi = app.MapApiVersionGroup(BrandingOpenApiDocuments.CurrentApiVersion);
        var publicBranding = versionedApi.MapGroup($"/{BrandingRoutes.PublicSettingsPath}")
            .AllowAnonymous();
        var managementBranding = versionedApi.MapGroup($"/{BrandingRoutes.ManagementSettingsPath}")
            .WithNoStoreResponses();

        publicBranding.MapGet("/", GetPublicSettings)
            .CacheOutput(policy => policy.Expire(BrandingHttpCache.PublicFreshness).Tag(BrandingHttpCache.PublicBrandingTag))
            .RequireRateLimiting(BrandingSecurityBaseline.PublicReadRateLimitPolicy);
        managementBranding.MapGet("/", GetManagementSettings)
            .Produces<BrandingManagementSettingsDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .AddOpenApiOperationTransformer((operation, _, _) =>
            {
                AddEntityTagResponseHeader(operation);
                return Task.CompletedTask;
            })
            .RequireRateLimiting(BrandingSecurityBaseline.MutationRateLimitPolicy)
            .RequireAuthorization(BrandingAuthorization.BrandingRead);
        managementBranding.MapPut("/", SaveSettings)
            .Produces<BrandingManagementSettingsDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .ProducesProblem(StatusCodes.Status428PreconditionRequired)
            .AddOpenApiOperationTransformer((operation, _, _) =>
            {
                AddIfMatchRequestHeader(operation);
                return Task.CompletedTask;
            })
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
        IBrandingSettingsRevisionStore store,
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

        httpContext.Response.Headers.ETag = BrandingEntityTags.Format(current.SourceEpoch, current.SourceRevision);
        return TypedResults.Ok(ToManagementDto(current));
    }

    private static async Task<Results<Ok<BrandingManagementSettingsDto>, ProblemHttpResult>> SaveSettings(
        BrandingSettingsDto request,
        IBrandingSettingsRevisionStore store,
        IOutputCacheStore outputCacheStore,
        ILogger<BrandingApiHostEntryPoint> logger,
        HttpContext httpContext,
        CancellationToken ct)
    {
        if (!httpContext.Request.Headers.TryGetValue(HeaderNames.IfMatch, out var ifMatchValues))
        {
            return PreconditionProblem(
                StatusCodes.Status428PreconditionRequired,
                BrandingManagementProblemCodes.PreconditionRequired,
                "Send If-Match with the entity tag from the latest Branding read.");
        }

        var ifMatch = BrandingIfMatch.Parse(ifMatchValues);
        if (ifMatch is null)
        {
            return PreconditionProblem(
                StatusCodes.Status400BadRequest,
                BrandingManagementProblemCodes.InvalidIfMatch,
                "If-Match must be \"*\" or a list of entity tags.");
        }

        var settings = BrandingSettings.Create(request, BrandingDefaults.AllowedFonts);
        if (settings.IsFailure)
        {
            return ToValidationProblem(settings.ErrorDetails);
        }

        BrandingSettingsSnapshot? saved;
        try
        {
            saved = await store.SaveIfMatch(settings.Value, ifMatch.Matches, ct).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            saved = null;
        }

        if (saved is null)
        {
            return PreconditionProblem(
                StatusCodes.Status412PreconditionFailed,
                BrandingManagementProblemCodes.PreconditionFailed,
                "Branding settings changed after they were loaded. Reload them and reapply your changes.");
        }

        await InvalidatePublicBrandingCache(outputCacheStore, logger).ConfigureAwait(false);
        return TypedResults.Ok(ToManagementDto(saved));
    }

    private static ProblemHttpResult PreconditionProblem(int statusCode, string code, string detail) =>
        TypedResults.Problem(
            detail,
            statusCode: statusCode,
            extensions: new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                [BrandingManagementProblemCodes.ExtensionName] = code
            });

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

    private static void AddEntityTagResponseHeader(OpenApiOperation operation)
    {
        if (operation.Responses is null
            || !operation.Responses.TryGetValue("200", out var okResponse)
            || okResponse is not OpenApiResponse response)
        {
            return;
        }

        response.Headers ??= new Dictionary<string, IOpenApiHeader>(StringComparer.Ordinal);
        response.Headers[HeaderNames.ETag] = new OpenApiHeader
        {
            Description = "Strong validator for the returned revision; send it as If-Match on the next PUT.",
            Required = true,
            Schema = new OpenApiSchema { Type = JsonSchemaType.String }
        };
    }

    private static void AddIfMatchRequestHeader(OpenApiOperation operation)
    {
        operation.Parameters ??= [];
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = HeaderNames.IfMatch,
            In = ParameterLocation.Header,
            Required = true,
            Description = "Entity tag from the latest management GET, or \"*\".",
            Schema = new OpenApiSchema { Type = JsonSchemaType.String }
        });
    }

    private static async Task<BrandingSettings> GetSettingsOrDefault(IBrandingSettingsStore store, CancellationToken ct)
    {
        return await store.GetSettings(ct).ConfigureAwait(false) ?? BrandingDefaults.CreateSettings();
    }

    private static async Task InvalidatePublicBrandingCache(IOutputCacheStore outputCacheStore, ILogger logger)
    {
        try
        {
            await outputCacheStore
                .EvictByTagAsync(BrandingHttpCache.PublicBrandingTag, CancellationToken.None)
                .ConfigureAwait(false);
            logger.PublicCacheAreaInvalidated(BrandingHttpCache.PublicBrandingArea);
        }
        catch (Exception exception) when (exception.ShouldHandleAsFailure(CancellationToken.None))
        {
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

    private static ProblemHttpResult ToValidationProblem(ResultError error)
    {
        // ProblemHttpResult avoids adding a new published 400 response while preserving the validation payload.
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
