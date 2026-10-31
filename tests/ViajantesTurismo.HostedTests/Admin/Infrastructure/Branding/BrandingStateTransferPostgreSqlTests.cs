using PandaBear.Branding;
using PandaBear.Testing;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Branding;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
[TestScope(TestScope.Integration)]
public sealed class BrandingStateTransferPostgreSqlTests(PostgreSqlTestServerFixture fixture) : IClassFixture<PostgreSqlTestServerFixture>
{
    [Fact]
    public async Task Branding_write_reaches_the_admin_projection_and_the_next_document_snapshot()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var catalogDatabase = await fixture.CreateDatabase(ct);
        await using var adminDatabase = await fixture.CreateDatabase(ct);
        await using var scenario = await BrandingStateTransferScenario.Start(catalogDatabase, adminDatabase, ct);
        var settings = BrandingSettings.Create(
            new BrandingSettingsDto
            {
                BrandName = "Viajantes ponta a ponta",
                PrimaryColor = "#112233",
                AccentColor = "#445566",
                BackgroundColor = "#FFFFFF",
                TextColor = "#000000",
                HeadingFontFamily = "Inter",
                BodyFontFamily = "Verdana"
            },
            BrandingFontFamilies.All).Value;

        // Act
        await scenario.Save(settings, ct);
        var projection = await scenario.WaitForProjectionRevision(1, TimeSpan.FromSeconds(60), ct);
        var snapshot = await scenario.CaptureDocumentBranding(ct);

        // Assert
        projection.ShouldNotBeNull().BrandName.ShouldBe("Viajantes ponta a ponta");
        snapshot.IsSuccess.ShouldBeTrue();
        snapshot.Value.SourceRevision.ShouldBe(1);
        snapshot.Value.PrimaryColor.ShouldBe("#112233");
        snapshot.Value.LogoUri.ShouldBeNull();
    }
}
