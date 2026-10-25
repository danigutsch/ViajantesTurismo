using PandaBear.Results;
using PandaBear.Testing;
using ViajantesTurismo.Admin.Application.Branding;
using ViajantesTurismo.Admin.Application.Documents;
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
}
