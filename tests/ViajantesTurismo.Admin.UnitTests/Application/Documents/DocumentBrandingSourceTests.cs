using PandaBear.Results;
using PandaBear.Testing;
using ViajantesTurismo.Admin.Application.Branding;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.UnitTests.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Application.Documents;

[Collection(AdminTelemetryGroup.Name)]
[Trait(SharedKernelTestTraitNames.CapabilityName, Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
public sealed class DocumentBrandingSourceTests
{
    [Fact]
    public async Task Http_capture_attributes_the_revision_when_the_projection_matches()
    {
        // Arrange
        var settings = DocumentDraftTestData.CreateBrandingSettings();
        var source = DocumentBrandingTestData.Http(settings, DocumentBrandingTestData.ProjectionMatching(settings, 5));
        using var measurements = new DocumentBrandingMeasurements();

        // Act
        var result = await source.Capture(CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.SourceRevision.ShouldBe(5);
        result.Value.BrandName.ShouldBe(settings.BrandName);
        measurements.MismatchReasons.ShouldBeEmpty();
    }

    [Fact]
    public async Task Http_capture_counts_a_missing_projection_and_keeps_the_revision_unknown()
    {
        // Arrange
        var settings = DocumentDraftTestData.CreateBrandingSettings();
        var source = DocumentBrandingTestData.Http(settings);
        using var measurements = new DocumentBrandingMeasurements();

        // Act
        var result = await source.Capture(CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.SourceRevision.ShouldBeNull();
        measurements.MismatchReasons.ShouldHaveSingleItem().ShouldBe(DocumentBrandingMetrics.ReasonMissing);
    }

    [Fact]
    public async Task Http_capture_counts_a_different_projection_and_uses_the_HTTP_values()
    {
        // Arrange
        var settings = DocumentDraftTestData.CreateBrandingSettings();
        var stale = DocumentBrandingTestData.ProjectionMatching(settings, 3) with
        {
            PrimaryColor = "#abcdef"
        };
        var source = DocumentBrandingTestData.Http(settings, stale);
        using var measurements = new DocumentBrandingMeasurements();

        // Act
        var result = await source.Capture(CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.PrimaryColor.ShouldBe(settings.PrimaryColor);
        result.Value.SourceRevision.ShouldBeNull();
        measurements.MismatchReasons.ShouldHaveSingleItem().ShouldBe(DocumentBrandingMetrics.ReasonDifferent);
    }

    [Fact]
    public async Task Projection_capture_reads_only_the_projection()
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
    public async Task Projection_capture_fails_closed_when_no_revision_was_applied()
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
    public async Task Projection_capture_fails_closed_while_the_projection_is_recovering()
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
}
