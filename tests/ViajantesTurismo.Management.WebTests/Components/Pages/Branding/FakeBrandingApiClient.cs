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

    public bool ThrowOnGetSettings
    {
        get; set;
    }

    public bool ReturnEmptyGetResponse
    {
        get; set;
    }

    public bool ReturnEmptySaveResponse
    {
        get; set;
    }

    public ContractValidationException? ValidationException
    {
        get; set;
    }

    public BrandingSettingsDto? SavedBranding
    {
        get; private set;
    }

    public Task<BrandingManagementDocument> GetSettings(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ThrowIfGetShouldFail();
        GetCount++;
        return Task.FromResult(new BrandingManagementDocument(ToManagement(Branding), BrandingEntityTags.Format(SourceEpoch, SourceRevision)));
    }

    public Task<BrandingManagementSettingsDto> SaveSettings(BrandingSettingsDto request, string entityTag, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        SavedEntityTag = entityTag;

        if (FailPrecondition)
        {
            throw new BrandingPreconditionFailedException();
        }

        if (ValidationException is not null)
        {
            throw ValidationException;
        }

        if (ReturnEmptySaveResponse)
        {
            throw new InvalidOperationException("Branding API returned an empty settings response.");
        }

        SavedBranding = request;
        Branding = request;
        SourceRevision++;
        return Task.FromResult(ToManagement(request));
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

    private void ThrowIfGetShouldFail()
    {
        if (ThrowOnGetSettings)
        {
            throw new HttpRequestException("Branding settings unavailable.");
        }

        if (ReturnEmptyGetResponse)
        {
            throw new InvalidOperationException("Branding API returned an empty settings response.");
        }
    }

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
