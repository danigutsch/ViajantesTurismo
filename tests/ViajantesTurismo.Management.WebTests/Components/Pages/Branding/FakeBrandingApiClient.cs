using System.Net;
using PandaBear.HttpClients;
using ViajantesTurismo.Branding.Contracts.Http;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Branding;

/// <summary>Configurable in-memory fake of the branding management client for page tests.</summary>
internal sealed class FakeBrandingApiClient : IBrandingManagementClient
{
    /// <summary>Source epoch reported in entity tags and returned settings.</summary>
    public Guid SourceEpoch { get; } = Guid.CreateVersion7();

    /// <summary>Source revision reported in entity tags; incremented after each successful save.</summary>
    public long SourceRevision { get; set; } = 1;

    /// <summary>When true, <see cref="SaveSettings"/> returns 412 Precondition Failed.</summary>
    public bool FailPrecondition
    {
        get; set;
    }

    /// <summary>Entity tag received by the latest save call.</summary>
    public string? SavedEntityTag
    {
        get; private set;
    }

    /// <summary>Number of successful settings reads.</summary>
    public int GetCount
    {
        get; private set;
    }

    /// <summary>Branding settings returned by reads and replaced by successful saves.</summary>
    public BrandingSettingsDto Branding { get; set; } = CreateDefaultBranding();

    /// <summary>When true, reads return an empty-body outcome.</summary>
    public bool ReturnEmptyGetResponse
    {
        get; set;
    }

    /// <summary>When true, reads after a successful save return an empty-body outcome.</summary>
    public bool ReturnEmptyGetResponseAfterSave
    {
        get; set;
    }

    /// <summary>When true, saves return an empty-body outcome.</summary>
    public bool ReturnEmptySaveResponse
    {
        get; set;
    }

    /// <summary>When true, saves return a malformed 400 body outcome.</summary>
    public bool ReturnMalformedValidationProblem
    {
        get; set;
    }

    /// <summary>When set, saves return a validation problem carrying these errors.</summary>
    public IReadOnlyDictionary<string, string[]>? ValidationErrors
    {
        get; set;
    }

    /// <summary>Settings captured by the latest successful save, or null if none succeeded.</summary>
    public BrandingSettingsDto? SavedBranding
    {
        get; private set;
    }

    /// <summary>Returns the current branding with an entity tag, or an empty-body outcome when configured.</summary>
    public Task<HttpOutcome<BrandingManagementDocument>> GetSettings(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (ReturnEmptyGetResponse || (ReturnEmptyGetResponseAfterSave && SavedBranding is not null))
        {
            return Task.FromResult(HttpOutcomeMapper.Status<BrandingManagementDocument>(HttpOutcomeKind.EmptyBody, HttpStatusCode.OK));
        }

        GetCount++;
        var document = new BrandingManagementDocument(ToManagement(Branding), BrandingEntityTags.Format(SourceEpoch, SourceRevision));
        return Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, document));
    }

    /// <summary>Records the entity tag, then applies the configured failure or stores the request as the new branding.</summary>
    public Task<HttpOutcome<BrandingManagementSettingsDto>> SaveSettings(BrandingSettingsDto request, string entityTag, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        SavedEntityTag = entityTag;

        if (FailPrecondition)
        {
            return Task.FromResult(HttpOutcomeMapper.Status<BrandingManagementSettingsDto>(
                HttpOutcomeKind.UnexpectedStatus,
                HttpStatusCode.PreconditionFailed));
        }

        if (ReturnMalformedValidationProblem)
        {
            return Task.FromResult(HttpOutcomeMapper.Status<BrandingManagementSettingsDto>(
                HttpOutcomeKind.MalformedBody,
                HttpStatusCode.BadRequest));
        }

        if (ValidationErrors is not null)
        {
            return Task.FromResult(new HttpOutcome<BrandingManagementSettingsDto>
            {
                Kind = HttpOutcomeKind.ValidationProblem,
                StatusCode = HttpStatusCode.BadRequest,
                ValidationErrors = ValidationErrors
            });
        }

        if (ReturnEmptySaveResponse)
        {
            return Task.FromResult(HttpOutcomeMapper.Status<BrandingManagementSettingsDto>(HttpOutcomeKind.EmptyBody, HttpStatusCode.OK));
        }

        SavedBranding = request;
        Branding = request;
        SourceRevision++;
        return Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, ToManagement(request)));
    }

    private BrandingManagementSettingsDto ToManagement(BrandingSettingsDto settings) => new()
    {
        BrandName = settings.BrandName,
        LogoUri = string.IsNullOrEmpty(settings.LogoUri) ? null : new Uri(settings.LogoUri, UriKind.RelativeOrAbsolute),
        PrimaryColor = settings.PrimaryColor,
        AccentColor = settings.AccentColor,
        BackgroundColor = settings.BackgroundColor,
        TextColor = settings.TextColor,
        HeadingFontFamily = settings.HeadingFontFamily,
        BodyFontFamily = settings.BodyFontFamily,
        SourceEpoch = SourceEpoch,
        SourceRevision = SourceRevision
    };

    private static BrandingSettingsDto CreateDefaultBranding()
    {
        return new BrandingSettingsDto
        {
            BrandName = "Viajantes Turismo",
            LogoUri = string.Empty,
            PrimaryColor = "#0F766E",
            AccentColor = "#F97316",
            BackgroundColor = "#FFFBF5",
            TextColor = "#1F2937",
            HeadingFontFamily = "Georgia",
            BodyFontFamily = "system-ui"
        };
    }
}
