using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Infrastructure.Documents;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Admin.UnitTests.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure.Documents;

[Trait(SharedKernelTestTraitNames.CategoryName, DocumentTestTraits.CoreBehaviorCategory)]
[Trait(SharedKernelTestTraitNames.CapabilityName, DocumentTestTraits.GeneratedDocumentsCapability)]
public sealed class DocumentDraftRetentionHostedServiceTests
{
    [Fact]
    public async Task RunBatch_purges_expired_drafts_through_the_registered_handler()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 7, 11, 0, 0, 0, TimeSpan.Zero);
        var expired = DocumentDraftTestData.Create(now.UtcDateTime.AddDays(-DocumentLimits.DraftRetentionDays));
        var store = new FakeDocumentStore();
        store.Documents.Add(expired.Id, expired);
        var services = new ServiceCollection();
        services.AddSingleton<IDocumentStore>(store);
        services.AddSingleton<TimeProvider>(new FakeTimeProvider(now));
        services.AddScoped<PurgeExpiredDraftsCommandHandler>();
        await using var provider = services.BuildServiceProvider();
        using var hostedService = new DocumentDraftRetentionHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<DocumentDraftRetentionHostedService>.Instance);

        // Act
        var removedCount = await hostedService.RunBatch(CancellationToken.None);

        // Assert
        removedCount.ShouldBe(1);
        store.Documents.ContainsKey(expired.Id).ShouldBeFalse();
    }
}
