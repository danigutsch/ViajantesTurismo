using SharedKernel.Idempotency;
using SharedKernel.Results;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Admin.UnitTests.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Application.Documents;

[Trait(SharedKernelTestTraitNames.CapabilityName, Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
public sealed class DocumentCommandIdempotencyTests
{
    [Fact]
    public async Task Fresh_key_acquires_ownership_stages_completion_before_save_and_returns_the_document_id()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 7, 24, 12, 0, 0, TimeSpan.Zero);
        var scope = IdempotencyScope.From($"admin.documents.generate-contract-draft:{Guid.CreateVersion7():N}");
        var key = IdempotencyKey.From(Guid.CreateVersion7().ToString("N"));
        var operation = new IdempotencyOperation(scope, key);
        var documentId = Guid.CreateVersion7();
        var unitOfWork = new FakeUnitOfWork();
        var store = new CapturingNewIdempotencyStore(unitOfWork);
        var coordinator = new DocumentCommandIdempotency(store, unitOfWork, new FakeTimeProvider(now));
        var operationCallCount = 0;

        // Act
        var existingResult = await coordinator.GetExistingResult(scope, key, CancellationToken.None);
        var result = await coordinator.Execute(
            scope,
            key,
            () =>
            {
                operationCallCount++;
                return Task.FromResult(Result.Ok(documentId));
            },
            CancellationToken.None);

        // Assert
        existingResult.ShouldBeNull();
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(documentId);
        operationCallCount.ShouldBe(1);
        store.GetCallCount.ShouldBe(1);
        store.TryStartCallCount.ShouldBe(1);
        store.StartedOperation.ShouldBe(operation);
        store.StartedAt.ShouldBe(now);
        store.LockDuration.ShouldBe(TimeSpan.FromMinutes(5));
        store.StageCompletionCallCount.ShouldBe(1);
        store.StagedOperation.ShouldBe(operation);
        store.CompletedAt.ShouldBe(now);
        store.StagedResultFingerprint.ShouldBe(documentId.ToString("N"));
        store.WasCompletionStagedBeforeSave.ShouldBeTrue();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Execute_replays_the_completed_result_when_try_start_loses_the_race()
    {
        // Arrange
        var scope = IdempotencyScope.From($"admin.documents.generate-contract-draft:{Guid.CreateVersion7():N}");
        var key = IdempotencyKey.From(Guid.CreateVersion7().ToString("N"));
        var completedDocumentId = Guid.CreateVersion7();
        var unitOfWork = new FakeUnitOfWork();
        var coordinator = DocumentIdempotencyTestData.CreateCompleted(scope, key, completedDocumentId, unitOfWork);
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
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(completedDocumentId);
        operationWasInvoked.ShouldBeFalse();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Execute_does_not_complete_or_save_an_owned_failed_operation()
    {
        // Arrange
        var scope = IdempotencyScope.From($"admin.documents.generate-contract-draft:{Guid.CreateVersion7():N}");
        var key = IdempotencyKey.From(Guid.CreateVersion7().ToString("N"));
        var unitOfWork = new FakeUnitOfWork();
        var store = new CapturingNewIdempotencyStore(unitOfWork);
        var coordinator = new DocumentCommandIdempotency(store, unitOfWork, TimeProvider.System);
        var operationCallCount = 0;

        // Act
        var result = await coordinator.Execute(
            scope,
            key,
            () =>
            {
                operationCallCount++;
                return Task.FromResult(Result.Conflict<Guid>("Document work failed."));
            },
            CancellationToken.None);

        // Assert
        result.Status.ShouldBe(ResultStatus.Conflict);
        result.ErrorDetails.ShouldNotBeNull().Detail.ShouldBe("Document work failed.");
        operationCallCount.ShouldBe(1);
        store.TryStartCallCount.ShouldBe(1);
        store.StageCompletionCallCount.ShouldBe(0);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("11111111-1111-1111-1111-111111111111")]
    public async Task Completed_entry_without_a_valid_n_fingerprint_fails_closed(string? resultFingerprint)
    {
        // Arrange
        var scope = IdempotencyScope.From($"admin.documents.generate-contract-draft:{Guid.CreateVersion7():N}");
        var key = IdempotencyKey.From(Guid.CreateVersion7().ToString("N"));
        var unitOfWork = new FakeUnitOfWork();
        var coordinator = DocumentIdempotencyTestData.CreateCompletedWithFingerprint(
            scope,
            key,
            resultFingerprint,
            unitOfWork);
        var operationWasInvoked = false;

        // Act
        var existingResult = await coordinator.GetExistingResult(scope, key, CancellationToken.None);
        var executionResult = await coordinator.Execute(
            scope,
            key,
            () =>
            {
                operationWasInvoked = true;
                return Task.FromResult(Result.Ok(Guid.CreateVersion7()));
            },
            CancellationToken.None);

        // Assert
        existingResult.ShouldNotBeNull().Status.ShouldBe(ResultStatus.Conflict);
        executionResult.Status.ShouldBe(ResultStatus.Conflict);
        operationWasInvoked.ShouldBeFalse();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Null_key_bypasses_idempotency_and_does_not_save_a_failed_operation()
    {
        // Arrange
        var scope = IdempotencyScope.From($"admin.documents.generate-contract-draft:{Guid.CreateVersion7():N}");
        var unitOfWork = new FakeUnitOfWork();
        var coordinator = DocumentIdempotencyTestData.Create(unitOfWork);
        var operationCallCount = 0;

        // Act
        var existingResult = await coordinator.GetExistingResult(scope, null, CancellationToken.None);
        var executionResult = await coordinator.Execute(
            scope,
            null,
            () =>
            {
                operationCallCount++;
                return Task.FromResult(Result.Conflict<Guid>("Document work failed."));
            },
            CancellationToken.None);

        // Assert
        existingResult.ShouldBeNull();
        executionResult.Status.ShouldBe(ResultStatus.Conflict);
        operationCallCount.ShouldBe(1);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }

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
