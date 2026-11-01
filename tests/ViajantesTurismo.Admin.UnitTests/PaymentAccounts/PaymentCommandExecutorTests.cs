using PandaBear.Idempotency;
using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Idempotency;
using ViajantesTurismo.Admin.Application.Payments;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Admin.UnitTests.Documents;

namespace ViajantesTurismo.Admin.UnitTests.PaymentAccounts;

/// <summary>Protects durable payment replay and lock identity while operation keys remain typed.</summary>
public sealed class PaymentCommandExecutorTests
{
    [Theory]
    [InlineData("approve", false)]
    [InlineData("approve", true)]
    [InlineData("finalize", false)]
    [InlineData("finalize", true)]
    [InlineData("review", false)]
    [InlineData("review", true)]
    [InlineData("void", false)]
    [InlineData("void", true)]
    [InlineData("withdraw", false)]
    [InlineData("withdraw", true)]
    public void Instruction_commands_hide_operation_keys_and_can_print_uninitialized_values(string operation, bool initialized)
    {
        // Arrange
        const string secretKey = "sensitive-operation-key";
        var key = initialized ? IdempotencyKey.From(secretKey) : default;
        var update = new UpdatePaymentInstructionsDto { ExpectedDocumentVersion = 1, Reason = FinancialReasonDto.InstructionApproved, Confirmed = true };
        var review = new ReviewPaymentInstructionsDto { ExpectedDocumentVersion = 1, ChangesRequested = true };
        object command = operation switch
        {
            "approve" => new ApprovePaymentInstructionsCommand(Guid.Empty, update, "actor", key),
            "finalize" => new FinalizePaymentInstructionsCommand(Guid.Empty, update, "actor", key),
            "review" => new ReviewPaymentInstructionsCommand(Guid.Empty, review, "actor", key),
            "void" => new VoidPaymentInstructionsCommand(Guid.Empty, update, "actor", key),
            "withdraw" => new WithdrawPaymentInstructionApprovalCommand(Guid.Empty, update, "actor", key),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };

        // Act
        var text = command.ToString();

        // Assert
        text.ShouldNotBeNull().ShouldContain("[redacted]", StringComparison.Ordinal);
        text.ShouldNotContain(secretKey, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_typed_key_preserves_lock_identity_and_replays_only_the_same_payload()
    {
        // Arrange
        var transaction = new ExecutingPaymentTransaction();
        var unitOfWork = new FakeUnitOfWork();
        var store = new CapturingNewIdempotencyStore(unitOfWork);
        var executor = new PaymentCommandExecutor(transaction, store, unitOfWork, TimeProvider.System);
        var key = IdempotencyKey.From("  payment-key  ");
        var resourceId = Guid.CreateVersion7();
        var fingerprint = new string('A', 64);
        // Act
        var created = await executor.Execute("payment:test", key, fingerprint, ["account:1"],
            () => Task.FromResult(Result.Ok(resourceId)), TestContext.Current.CancellationToken);
        var replay = await executor.Execute("payment:test", key, fingerprint, ["account:1"],
            () => throw new InvalidOperationException("A replay must not execute the mutation."), TestContext.Current.CancellationToken);
        var changed = await executor.Execute("payment:test", key, new string('B', 64), ["account:1"],
            () => throw new InvalidOperationException("Changed inputs must not execute the mutation."), TestContext.Current.CancellationToken);

        // Assert
        created.Value.ShouldBe(resourceId);
        replay.Value.ShouldBe(resourceId);
        changed.Status.ShouldBe(ResultStatus.Conflict);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
        store.StageCompletionCallCount.ShouldBe(1);
        store.WasCompletionStagedBeforeSave.ShouldBeTrue();
        store.StartedOperation.ShouldNotBeNull().Key.ShouldBe(key);
        store.StagedResultFingerprint.ShouldBe(fingerprint + ":" + resourceId.ToString("N"));
        transaction.LockNames.ShouldBe(["account:1", "admin:payment-operation:" + ApplicationRequestFingerprint.Create("payment:test", key.Value)]);
    }

    [Fact]
    public async Task An_uninitialized_key_is_rejected_before_locking_or_persistence()
    {
        // Arrange
        var transaction = new ExecutingPaymentTransaction();
        var unitOfWork = new FakeUnitOfWork();
        var executor = new PaymentCommandExecutor(transaction, new UnusedIdempotencyStore(), unitOfWork, TimeProvider.System);

        // Act
        var result = await executor.Execute("payment:test", default, new string('A', 64), [],
            () => throw new InvalidOperationException("An uninitialized key must not reach the mutation."), TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ResultStatus.Invalid);
        transaction.Calls.ShouldBe(0);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
        result.ErrorDetails.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull().ContainsKey("Idempotency-Key").ShouldBeTrue();
    }
}
