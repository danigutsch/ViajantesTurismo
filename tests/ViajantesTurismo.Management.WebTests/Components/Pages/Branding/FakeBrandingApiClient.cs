using System.Net;
using PandaBear.HttpClients;
using ViajantesTurismo.Branding.Contracts.Http;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Branding;

internal sealed class FakeBrandingApiClient : IBrandingManagementClient
{
    public Guid SourceEpoch { get; } = Guid.CreateVersion7();

    public long SourceRevision { get; set; } = 1;

    public bool FailPrecondition
    {
        get; set;
    }

    public string? SavedEntityTag
    {
        get; private set;
    }

    public int GetCount
    {
        get; private set;
    }

    public BrandingSettingsDto Branding { get; set; } = CreateDefaultBranding();

    public bool ReturnEmptyGetResponse
    {
        get; set;
    }

    public bool ReturnEmptySaveResponse
    {
        get; set;
    }

    public bool ReturnMalformedValidationProblem
    {
        get; set;
    }

    public IReadOnlyDictionary<string, string[]>? ValidationErrors
    {
        get; set;
    }

    public BrandingSettingsDto? SavedBranding
    {
        get; private set;
    }

    public Task<HttpOutcome<BrandingManagementDocument>> GetSettings(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (ReturnEmptyGetResponse)
        {
            return Task.FromResult(HttpOutcomeMapper.Status<BrandingManagementDocument>(HttpOutcomeKind.EmptyBody, HttpStatusCode.OK));
        }

        GetCount++;
        var document = new BrandingManagementDocument(ToManagement(Branding), BrandingEntityTags.Format(SourceEpoch, SourceRevision));
        return Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, document));
    }

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
