using SharedKernel.Testing;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Infrastructure.Tests.Documents;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
[Trait(SharedKernelTestTraitNames.CapabilityName, DocumentTestTraits.GeneratedDocumentsCapability)]
public sealed class DocumentStorePostgreSqlTests : IAsyncLifetime
{
    private PostgreSqlDocumentStoreScenario? scenario;

    private PostgreSqlDocumentStoreScenario Scenario =>
        scenario ?? throw new InvalidOperationException("Test scenario is not initialized.");

    public async ValueTask InitializeAsync()
    {
        scenario = await PostgreSqlDocumentStoreScenario.Create(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        var currentScenario = scenario;
        scenario = null;

        if (currentScenario is not null)
        {
            await currentScenario.DisposeAsync();
        }
    }

    [Fact]
    public async Task PurgeExpiredDrafts_deletes_only_expired_unfinalized_documents()
    {
        // Arrange
        var now = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);
        var expired = DocumentDraftInfrastructureTestData.CreateDraft(now.AddDays(-DocumentLimits.DraftRetentionDays - 1));
        var boundaryExpired = DocumentDraftInfrastructureTestData.CreateDraft(now.AddDays(-DocumentLimits.DraftRetentionDays));
        var future = DocumentDraftInfrastructureTestData.CreateDraft(now.AddDays(-DocumentLimits.DraftRetentionDays).AddMinutes(1));
        var current = DocumentDraftInfrastructureTestData.CreateDraft(now);
        var finalized = DocumentDraftInfrastructureTestData.CreateFinalizedDraft(now.AddYears(-DocumentLimits.FinalizedRetentionYears - 1));
        await Scenario.Seed(expired, boundaryExpired, future, current, finalized);

        // Act
        var removedCount = await Scenario.PurgeExpiredDrafts(now, TestContext.Current.CancellationToken);

        // Assert
        removedCount.ShouldBe(2);
        var remaining = await Scenario.GetDocuments(TestContext.Current.CancellationToken);
        remaining.Select(document => document.Id).ShouldContain(future.Id);
        remaining.Select(document => document.Id).ShouldContain(current.Id);
        remaining.Select(document => document.Id).ShouldContain(finalized.Id);
        remaining.Select(document => document.Id).ShouldNotContain(expired.Id);
        remaining.Select(document => document.Id).ShouldNotContain(boundaryExpired.Id);
        remaining.Sum(document => document.Fields.Count).ShouldBe(6);
        var hasRetentionIndex = await Scenario.HasRetentionIndex(TestContext.Current.CancellationToken);
        hasRetentionIndex.ShouldBeTrue();
    }

    [Fact]
    public async Task Invalid_branding_logo_uri_materializes_as_missing_snapshot_logo()
    {
        // Arrange
        var document = DocumentDraftInfrastructureTestData.CreateDraft(DateTime.UtcNow);
        await Scenario.Seed(document);
        await Scenario.SetBrandingLogoUri(document.Id, "/\\evil.test/logo.svg", TestContext.Current.CancellationToken);

        // Act
        var documents = await Scenario.GetDocuments(TestContext.Current.CancellationToken);

        // Assert
        documents.ShouldHaveSingleItem().BrandingLogoUri.ShouldBeNull();
    }
}
