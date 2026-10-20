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
    private static readonly string RequestFingerprint =
        IdempotencyRequestFingerprint.Create("tour-service-contract", "1");

    [Fact]
    public async Task Canceled_lookup_preserves_the_caller_token_without_preparing_or_claiming()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        var unitOfWork = new FakeUnitOfWork();
        var auditStore = new FakeDocumentAuditStore();
        var store = new CapturingNewIdempotencyStore(unitOfWork);
        var coordinator = new DocumentCommandIdempotency(store, unitOfWork, TimeProvider.System);
        var command = new GenerateContractDraftCommand(
            Guid.CreateVersion7(), DocumentAuditTestData.CreateContext(),
            IdempotencyKey.From(Guid.CreateVersion7().ToString("N")));
        var preparationWasInvoked = false;
        await cancellation.CancelAsync();

        // Act
        Func<Task> action = async () => await coordinator.Execute(
            command, DocumentAuditTestData.CreateWriter(auditStore, unitOfWork), _ =>
            {
                preparationWasInvoked = true;
                return Task.FromResult(Result.Ok(Guid.CreateVersion7()));
            }, cancellation.Token);
        var exception = await action.ShouldThrow<OperationCanceledException>();

        // Assert
        exception.CancellationToken.ShouldBe(cancellation.Token);
        store.LastGetCancellationToken.ShouldBe(cancellation.Token);
        preparationWasInvoked.ShouldBeFalse();
        store.TryStartCallCount.ShouldBe(0);
        store.StageCompletionCallCount.ShouldBe(0);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
        auditStore.Records.ShouldBeEmpty();
    }

    [Fact]
    public async Task Cancellation_before_completion_preserves_the_caller_token_without_completing_or_saving()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        var unitOfWork = new FakeUnitOfWork();
        var auditStore = new FakeDocumentAuditStore();
        var store = new CapturingNewIdempotencyStore(unitOfWork);
        var coordinator = new DocumentCommandIdempotency(store, unitOfWork, TimeProvider.System);
        var command = new GenerateContractDraftCommand(
            Guid.CreateVersion7(), DocumentAuditTestData.CreateContext(),
            IdempotencyKey.From(Guid.CreateVersion7().ToString("N")));
        var mutationWasInvoked = false;

        // Act
        Func<Task> action = async () => await coordinator.Execute(
            command, DocumentAuditTestData.CreateWriter(auditStore, unitOfWork), commit => commit(async () =>
            {
                mutationWasInvoked = true;
                await cancellation.CancelAsync();
                return Result.Ok(Guid.CreateVersion7());
            }, null, command.BookingId, null), cancellation.Token);
        var exception = await action.ShouldThrow<OperationCanceledException>();

        // Assert
        exception.CancellationToken.ShouldBe(cancellation.Token);
        store.LastCompletionCancellationToken.ShouldBe(cancellation.Token);
        mutationWasInvoked.ShouldBeTrue();
        store.TryStartCallCount.ShouldBe(1);
        store.StageCompletionCallCount.ShouldBe(0);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
        auditStore.Records.ShouldBeEmpty();
    }

    [Fact]
    public async Task Preparation_failure_does_not_claim_a_key_or_persist_work()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var auditStore = new FakeDocumentAuditStore();
        var store = new CapturingNewIdempotencyStore(unitOfWork);
        var coordinator = new DocumentCommandIdempotency(store, unitOfWork, TimeProvider.System);
        var command = new GenerateContractDraftCommand(
            Guid.CreateVersion7(), DocumentAuditTestData.CreateContext(),
            IdempotencyKey.From(Guid.CreateVersion7().ToString("N")));
        var preparationWasInvoked = false;

        // Act
        var result = await coordinator.Execute(command, DocumentAuditTestData.CreateWriter(auditStore, unitOfWork), _ =>
        {
            preparationWasInvoked = true;
            return Task.FromResult(Result.Invalid<Guid>(detail: "Source validation failed.", field: "TemplateId", message: "Template is invalid."));
        }, CancellationToken.None);

        // Assert
        result.Status.ShouldBe(ResultStatus.Invalid);
        preparationWasInvoked.ShouldBeTrue();
        store.GetCallCount.ShouldBe(1);
        store.TryStartCallCount.ShouldBe(0);
        store.StageCompletionCallCount.ShouldBe(0);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
        auditStore.Records.ShouldBeEmpty();
    }

    [Fact]
    public async Task Prepared_mutation_claims_ownership_only_at_commit_and_stages_completion_before_save()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var auditStore = new FakeDocumentAuditStore();
        var store = new CapturingNewIdempotencyStore(unitOfWork);
        var coordinator = new DocumentCommandIdempotency(store, unitOfWork, TimeProvider.System);
        var command = new GenerateContractDraftCommand(
            Guid.CreateVersion7(), DocumentAuditTestData.CreateContext(),
            IdempotencyKey.From(Guid.CreateVersion7().ToString("N")));
        var documentId = Guid.CreateVersion7();
        var ownershipCountBeforePreparation = -1;
        var ownershipCountAtMutation = -1;

        // Act
        var result = await coordinator.Execute(command, DocumentAuditTestData.CreateWriter(auditStore, unitOfWork), commit =>
        {
            ownershipCountBeforePreparation = store.TryStartCallCount;
            return commit(() =>
            {
                ownershipCountAtMutation = store.TryStartCallCount;
                return Task.FromResult(Result.Ok(documentId));
            }, null, command.BookingId, null);
        }, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(documentId);
        ownershipCountBeforePreparation.ShouldBe(0);
        ownershipCountAtMutation.ShouldBe(1);
        var key = command.IdempotencyKey;
        store.StartedOperation.ShouldBe(new IdempotencyOperation(command.Scope, key));
        store.StagedResultFingerprint.ShouldBe($"{RequestFingerprint}:{documentId:N}");
        store.WasCompletionStagedBeforeSave.ShouldBeTrue();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
        auditStore.Records.ShouldBeEmpty();
    }

    [Fact]
    public void Commands_select_the_current_template_and_preserve_resource_operation_scopes()
    {
        // Arrange
        var bookingId = Guid.CreateVersion7();
        var documentId = Guid.CreateVersion7();
        var auditContext = DocumentAuditTestData.CreateContext();

        // Act
        var generate = new GenerateContractDraftCommand(bookingId, auditContext, IdempotencyKey.From(Guid.CreateVersion7().ToString("N")));
        var regenerate = new RegenerateDocumentDraftCommand(documentId, auditContext, IdempotencyKey.From(Guid.CreateVersion7().ToString("N")));
        var fingerprint = IdempotencyRequestFingerprint.Create(generate.TemplateId, generate.TemplateVersion);

        // Assert
        generate.TemplateId.ShouldBe("tour-service-contract");
        generate.TemplateVersion.ShouldBe("1");
        regenerate.TemplateId.ShouldBe(generate.TemplateId);
        regenerate.TemplateVersion.ShouldBe(generate.TemplateVersion);
        generate.Scope.Value.ShouldBe($"admin.documents.generate-contract-draft:{bookingId:N}");
        regenerate.Scope.Value.ShouldBe($"admin.documents.regenerate-draft:{documentId:N}");
        fingerprint.ShouldBe(RequestFingerprint);
    }

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
        var existingResult = await coordinator.GetExistingResult(
            scope,
            key,
            RequestFingerprint,
            CancellationToken.None);
        var result = await coordinator.Execute(
            scope,
            key,
            RequestFingerprint,
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
        store.StagedResultFingerprint.ShouldBe($"{RequestFingerprint}:{documentId:N}");
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
        var coordinator = DocumentIdempotencyTestData.CreateCompleted(
            scope,
            key,
            completedDocumentId,
            RequestFingerprint,
            unitOfWork);
        var operationWasInvoked = false;

        // Act
        var result = await coordinator.Execute(
            scope,
            key,
            RequestFingerprint,
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
            RequestFingerprint,
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
        var existingResult = await coordinator.GetExistingResult(
            scope,
            key,
            RequestFingerprint,
            CancellationToken.None);
        var executionResult = await coordinator.Execute(
            scope,
            key,
            RequestFingerprint,
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
    public async Task Legacy_bare_guid_entry_conflicts_when_template_inputs_change()
    {
        // Arrange
        var scope = IdempotencyScope.From($"admin.documents.generate-contract-draft:{Guid.CreateVersion7():N}");
        var key = IdempotencyKey.From(Guid.CreateVersion7().ToString("N"));
        var unitOfWork = new FakeUnitOfWork();
        var coordinator = DocumentIdempotencyTestData.CreateCompletedWithFingerprint(
            scope,
            key,
            Guid.CreateVersion7().ToString("N"),
            unitOfWork);
        var changedRequestFingerprint = IdempotencyRequestFingerprint.Create(
            "tour-service-contract",
            "2");

        // Act
        var result = await coordinator.GetExistingResult(
            scope,
            key,
            changedRequestFingerprint,
            CancellationToken.None);

        // Assert
        result.ShouldNotBeNull().Status.ShouldBe(ResultStatus.Conflict);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Uninitialized_key_is_invalid_before_lookup_or_mutation()
    {
        // Arrange
        var scope = IdempotencyScope.From($"admin.documents.generate-contract-draft:{Guid.CreateVersion7():N}");
        var unitOfWork = new FakeUnitOfWork();
        var coordinator = DocumentIdempotencyTestData.Create(unitOfWork);
        var operationCallCount = 0;

        // Act
        var existingResult = await coordinator.GetExistingResult(
            scope,
            default(IdempotencyKey),
            string.Empty,
            CancellationToken.None);
        var executionResult = await coordinator.Execute(
            scope,
            default(IdempotencyKey),
            string.Empty,
            () =>
            {
                operationCallCount++;
                return Task.FromResult(Result.Conflict<Guid>("Document work failed."));
            },
            CancellationToken.None);

        // Assert
        existingResult.ShouldNotBeNull().Status.ShouldBe(ResultStatus.Invalid);
        executionResult.Status.ShouldBe(ResultStatus.Invalid);
        operationCallCount.ShouldBe(0);
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
        var result = await coordinator.GetExistingResult(
            scope,
            key,
            RequestFingerprint,
            CancellationToken.None);

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
        var result = await coordinator.GetExistingResult(
            scope,
            key,
            RequestFingerprint,
            CancellationToken.None);

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
            RequestFingerprint,
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
