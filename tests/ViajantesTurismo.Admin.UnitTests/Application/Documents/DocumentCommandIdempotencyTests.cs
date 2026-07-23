using SharedKernel.Idempotency;
using SharedKernel.Results;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Admin.UnitTests.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Application.Documents;

[Trait(SharedKernelTestTraitNames.CapabilityName, Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
public sealed class DocumentCommandIdempotencyTests
{
    [Fact]
    public async Task GetExistingResult_returns_conflict_immediately_before_the_processing_lock_expires()
    {
        // Arrange
        var scope = IdempotencyScope.From($"admin.documents.generate-contract-draft:{Guid.CreateVersion7():N}");
        var key = IdempotencyKey.From(Guid.CreateVersion7().ToString("N"));
        var elapsed = TimeSpan.FromMinutes(5) - TimeSpan.FromTicks(1);
        var coordinator = DocumentIdempotencyTestData.CreateStarted(scope, key, new FakeUnitOfWork(), elapsed);

        // Act
        var result = await coordinator.GetExistingResult(scope, key, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull().Status.ShouldBe(ResultStatus.Conflict);
    }

    [Fact]
    public async Task GetExistingResult_allows_ownership_exactly_when_the_processing_lock_expires()
    {
        // Arrange
        var scope = IdempotencyScope.From($"admin.documents.generate-contract-draft:{Guid.CreateVersion7():N}");
        var key = IdempotencyKey.From(Guid.CreateVersion7().ToString("N"));
        var coordinator = DocumentIdempotencyTestData.CreateStarted(
            scope,
            key,
            new FakeUnitOfWork(),
            TimeSpan.FromMinutes(5));

        // Act
        var result = await coordinator.GetExistingResult(scope, key, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task Execute_returns_canonical_conflict_without_running_an_operation_owned_by_another_request()
    {
        // Arrange
        var scope = IdempotencyScope.From($"admin.documents.generate-contract-draft:{Guid.CreateVersion7():N}");
        var key = IdempotencyKey.From(Guid.CreateVersion7().ToString("N"));
        var coordinator = DocumentIdempotencyTestData.CreateStarted(scope, key, new FakeUnitOfWork());
        var operationWasInvoked = false;

        // Act
        var result = await coordinator.Execute(
            scope,
            key,
            () =>
            {
                operationWasInvoked = true;
                return Task.FromResult(Result.Ok(Guid.CreateVersion7()));
            },
            CancellationToken.None);

        // Assert
        result.Status.ShouldBe(ResultStatus.Conflict);
        result.ErrorDetails.ShouldNotBeNull().Detail.ShouldBe(
            "A document revision already exists for this booking. Reload and retry.");
        operationWasInvoked.ShouldBeFalse();
    }
}
