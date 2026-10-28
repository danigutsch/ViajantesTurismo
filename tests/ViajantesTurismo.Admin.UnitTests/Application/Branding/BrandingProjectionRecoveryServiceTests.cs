using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Branding;

namespace ViajantesTurismo.Admin.UnitTests.Application.Branding;

[Trait(PandaBear.Testing.SharedKernelTestTraitNames.CategoryName, Infrastructure.TestTraits.CoreBehaviorCategory)]
public sealed class BrandingProjectionRecoveryServiceTests
{
    [Fact]
    public async Task Begin_commits_the_recovery_fence()
    {
        // Arrange
        var store = new FakeBrandingProjectionStore();
        var service = new BrandingProjectionRecoveryService(store, TimeProvider.System);

        // Act
        await service.Begin("source-restore", TestContext.Current.CancellationToken);

        // Assert
        var recovery = store.Recovery.ShouldNotBeNull();
        recovery.Status.ShouldBe(BrandingProjectionStatus.Recovering);
        recovery.Reason.ShouldBe("source-restore");
    }

    [Fact]
    public async Task Authorize_requires_an_active_recovery()
    {
        // Arrange
        var store = new FakeBrandingProjectionStore();
        var service = new BrandingProjectionRecoveryService(store, TimeProvider.System);

        // Act
        var result = await service.AuthorizeReseed(BrandingProjectionTestData.CreateReseedRequest(null, null, Guid.CreateVersion7(), 1), TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ResultStatus.Conflict);
        store.Recovery.ShouldBeNull();
    }

    [Fact]
    public async Task Authorize_refuses_a_stale_expected_watermark()
    {
        // Arrange
        var store = new FakeBrandingProjectionStore();
        await new BrandingProjectionApplier(store, TimeProvider.System)
            .Apply(BrandingProjectionTestData.CreateEvent(revision: 4), TestContext.Current.CancellationToken);
        var service = new BrandingProjectionRecoveryService(store, TimeProvider.System);
        await service.Begin("projection-corrupt", TestContext.Current.CancellationToken);

        // Act
        var result = await service.AuthorizeReseed(
            BrandingProjectionTestData.CreateReseedRequest(BrandingProjectionTestData.Epoch, 3, BrandingProjectionTestData.Epoch, 4),
            TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ResultStatus.Conflict);
        store.Recovery.ShouldNotBeNull().Authorization.ShouldBeNull();
    }

    [Fact]
    public async Task Authorize_refuses_a_same_epoch_rewind()
    {
        // Arrange
        var store = new FakeBrandingProjectionStore();
        await new BrandingProjectionApplier(store, TimeProvider.System)
            .Apply(BrandingProjectionTestData.CreateEvent(revision: 4), TestContext.Current.CancellationToken);
        var service = new BrandingProjectionRecoveryService(store, TimeProvider.System);
        await service.Begin("source-restore", TestContext.Current.CancellationToken);

        // Act
        var result = await service.AuthorizeReseed(
            BrandingProjectionTestData.CreateReseedRequest(BrandingProjectionTestData.Epoch, 4, BrandingProjectionTestData.Epoch, 2),
            TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ResultStatus.Invalid);
    }

    [Fact]
    public async Task Authorize_records_the_exact_reseed_identity()
    {
        // Arrange
        var store = new FakeBrandingProjectionStore();
        await new BrandingProjectionApplier(store, TimeProvider.System)
            .Apply(BrandingProjectionTestData.CreateEvent(revision: 4), TestContext.Current.CancellationToken);
        var service = new BrandingProjectionRecoveryService(store, TimeProvider.System);
        await service.Begin("source-restore", TestContext.Current.CancellationToken);
        var newEpoch = Guid.CreateVersion7();
        var request = BrandingProjectionTestData.CreateReseedRequest(BrandingProjectionTestData.Epoch, 4, newEpoch, 1);

        // Act
        var result = await service.AuthorizeReseed(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var authorization = store.Recovery.ShouldNotBeNull().Authorization.ShouldNotBeNull();
        authorization.EventId.ShouldBe(request.EventId);
        authorization.TargetEpoch.ShouldBe(newEpoch);
        authorization.TargetRevision.ShouldBe(1);
        authorization.AuthorizedBy.ShouldBe("operator@example.test");
    }
}
