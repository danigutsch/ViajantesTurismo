using System.Text;
using ImageMagick;
using PandaBear.Results;
using PandaBear.Testing;
using ViajantesTurismo.Admin.Application.Branding;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Testing.Builders;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Admin.UnitTests.Application.Branding;
using ViajantesTurismo.Admin.UnitTests.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Application.Documents;

[Trait(SharedKernelTestTraitNames.CapabilityName, Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
public sealed class DocumentBrandingSourceTests
{
    [Fact]
    public async Task Capture_reads_the_projection_and_records_its_revision()
    {
        // Arrange
        var settings = DocumentDraftTestData.CreateBrandingSettings();
        var source = DocumentBrandingTestData.Projection(DocumentBrandingTestData.ProjectionMatching(settings, 9));

        // Act
        var result = await source.Capture(CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.SourceRevision.ShouldBe(9);
        result.Value.BrandName.ShouldBe(settings.BrandName);
    }

    [Fact]
    public async Task Capture_fails_closed_when_no_revision_was_applied()
    {
        // Arrange
        var source = DocumentBrandingTestData.Projection(null);

        // Act
        var result = await source.Capture(CancellationToken.None);

        // Assert
        result.Status.ShouldBe(ResultStatus.Unavailable);
        result.ErrorDetails.ShouldNotBeNull().Detail.ShouldBe(DocumentBrandingSource.UnavailableDetail);
    }

    [Fact]
    public async Task Capture_fails_closed_while_the_projection_is_recovering()
    {
        // Arrange
        var settings = DocumentDraftTestData.CreateBrandingSettings();
        var source = DocumentBrandingTestData.Projection(
            DocumentBrandingTestData.ProjectionMatching(settings, 9),
            BrandingProjectionRecovery.Start("source-restore", DateTimeOffset.UnixEpoch));

        // Act
        var result = await source.Capture(CancellationToken.None);

        // Assert
        result.Status.ShouldBe(ResultStatus.Unavailable);
    }

    [Fact]
    public async Task Capture_embeds_the_projection_logo_as_a_webp_data_uri()
    {
        // Arrange
        var settings = DocumentDraftTestData.CreateBrandingSettings();
        var source = DocumentBrandingTestData.Projection(
            DocumentBrandingTestData.ProjectionMatching(settings, 4, DocumentLogoTestData.Logo));

        // Act
        var result = await source.Capture(CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var logoUri = result.Value.LogoUri.ShouldNotBeNull();
        logoUri.OriginalString.ShouldBe(DocumentLogoTestData.DataUri.OriginalString);
        logoUri.OriginalString.ShouldStartWith("data:image/webp;base64,", StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_transparent_webp_logo_stays_a_valid_data_uri_and_is_rendered_with_its_alpha()
    {
        // Arrange
        using var transparent = new MagickImage(MagickColors.Transparent, 32, 16);
        transparent.Format = MagickFormat.WebP;
        var webp = transparent.ToByteArray();
        var settings = DocumentDraftTestData.CreateBrandingSettings();
        var source = DocumentBrandingTestData.Projection(
            DocumentBrandingTestData.ProjectionMatching(settings, 4, DocumentLogoTestData.LogoFor(webp)));
        var capture = await source.Capture(CancellationToken.None);
        var logoUri = capture.Value.LogoUri.ShouldNotBeNull();
        var draft = DocumentDraftTestData.Create(TimeProvider.System.GetUtcNow().UtcDateTime, logoUri);

        // Act
        var html = Encoding.UTF8.GetString(DocumentArtifactRenderer.Render(draft));
        var embedded = Convert.FromBase64String(logoUri.OriginalString["data:image/webp;base64,".Length..]);
        using var decoded = new MagickImage(embedded);

        // Assert
        logoUri.OriginalString.ShouldStartWith("data:image/webp;base64,", StringComparison.Ordinal);
        html.ShouldContain(logoUri.OriginalString, StringComparison.Ordinal);
        decoded.HasAlpha.ShouldBeTrue();
    }

    [Fact]
    public async Task Capture_without_a_projection_logo_yields_no_logo_even_with_a_stored_route()
    {
        // Arrange
        var settings = DocumentDraftTestData.CreateBrandingSettings();
        settings.LogoUri = "/api/v1/public/branding/logo/" + DocumentLogoTestData.Sha256;
        var source = DocumentBrandingTestData.Projection(DocumentBrandingTestData.ProjectionMatching(settings, 4));

        // Act
        var result = await source.Capture(CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.LogoUri.ShouldBeNull();
    }

    [Fact]
    public async Task Capture_records_the_logo_hash_in_the_version_not_the_uri()
    {
        // Arrange
        var settings = DocumentDraftTestData.CreateBrandingSettings();
        var first = DocumentBrandingTestData.Projection(DocumentBrandingTestData.ProjectionMatching(settings, 1, DocumentLogoTestData.Logo));
        var same = DocumentBrandingTestData.Projection(DocumentBrandingTestData.ProjectionMatching(settings, 2, DocumentLogoTestData.Logo));
        var other = DocumentBrandingTestData.Projection(
            DocumentBrandingTestData.ProjectionMatching(settings, 3, DocumentLogoTestData.LogoFor(DocumentLogoTestData.OtherContent)));
        var none = DocumentBrandingTestData.Projection(DocumentBrandingTestData.ProjectionMatching(settings, 4));

        // Act
        var firstResult = await first.Capture(CancellationToken.None);
        var firstVersion = firstResult.Value.Version;
        var sameResult = await same.Capture(CancellationToken.None);
        var sameVersion = sameResult.Value.Version;
        var otherResult = await other.Capture(CancellationToken.None);
        var otherVersion = otherResult.Value.Version;
        var noneResult = await none.Capture(CancellationToken.None);
        var noneVersion = noneResult.Value.Version;

        // Assert
        sameVersion.ShouldBe(firstVersion);
        otherVersion.ShouldNotBe(firstVersion);
        noneVersion.ShouldNotBe(firstVersion);
    }

    [Fact]
    public async Task Replacing_then_deleting_the_projection_logo_does_not_change_a_finalized_artifact()
    {
        // Arrange
        var settings = DocumentDraftTestData.CreateBrandingSettings();
        var store = new FakeBrandingProjectionStore
        {
            Current = DocumentBrandingTestData.ProjectionMatching(settings, 1, DocumentLogoTestData.Logo)
        };
        var capture = await new DocumentBrandingSource(store).Capture(CancellationToken.None);
        var captured = capture.Value;
        var now = TimeProvider.System.GetUtcNow().UtcDateTime;
        var document = DocumentDraftTestData.Create(now, captured.LogoUri);
        document.BeginReview(now).IsSuccess.ShouldBeTrue();
        document.Approve(now).IsSuccess.ShouldBeTrue();
        var documents = new FakeDocumentStore();
        documents.Documents.Add(document.Id, document);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new FinalizeDocumentCommandHandler(documents, unitOfWork, TimeProvider.System, DocumentAuditTestData.CreateWriter(new FakeDocumentAuditStore(), unitOfWork), new FakeDocumentSourceVersionReader());
        var finalization = await handler.Handle(new FinalizeDocumentCommand(document.Id, DocumentAuditTestData.CreateContext()), CancellationToken.None);
        finalization.IsSuccess.ShouldBeTrue();
        var before = document.CopyFinalizedArtifactContent().ShouldNotBeNull().ToArray();

        // Act
        store.Current = DocumentBrandingTestData.ProjectionMatching(
            settings, 2, DocumentLogoTestData.LogoFor(DocumentLogoTestData.OtherContent));
        var replaced = document.CopyFinalizedArtifactContent().ShouldNotBeNull().ToArray();
        store.Current = DocumentBrandingTestData.ProjectionMatching(settings, 3);
        var deleted = document.CopyFinalizedArtifactContent().ShouldNotBeNull().ToArray();

        // Assert
        replaced.ShouldBe(before);
        deleted.ShouldBe(before);
        Encoding.UTF8.GetString(before).ShouldContain(DocumentLogoTestData.DataUri.OriginalString, StringComparison.Ordinal);
    }
}
