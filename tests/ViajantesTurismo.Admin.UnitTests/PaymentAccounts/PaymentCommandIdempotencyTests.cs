using PandaBear.Idempotency;
using PandaBear.Results;
using PandaBear.Testing;
using ViajantesTurismo.Admin.Application.Idempotency;
using ViajantesTurismo.Admin.Application.Payments;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Admin.UnitTests.Documents;
using ViajantesTurismo.Admin.UnitTests.Infrastructure;

namespace ViajantesTurismo.Admin.UnitTests.PaymentAccounts;

/// <summary>Protects durable payment replay and lock identity while operation keys remain typed.</summary>
[Trait(TestTraitNames.CategoryName, TestTraits.CoreBehaviorCategory)]
[Trait(SharedKernelTestTraitNames.CapabilityName, TestTraits.PaymentsCapability)]
public sealed class PaymentCommandIdempotencyTests
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
        var target = new PaymentInstructionTarget(Guid.Empty, Guid.Empty, Guid.Empty, 1);
        object command = operation switch
        {
            "approve" => new ApprovePaymentInstructionsCommand(target, update, "actor", key),
            "finalize" => new FinalizePaymentInstructionsCommand(target, update, "actor", key),
            "review" => new ReviewPaymentInstructionsCommand(target, review, "actor", key),
            "void" => new VoidPaymentInstructionsCommand(target, update, "actor", key),
            "withdraw" => new WithdrawPaymentInstructionApprovalCommand(target, update, "actor", key),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };

        // Act
        var text = command.ToString();

        // Assert
        text.ShouldNotBeNull().ShouldContain("[redacted]", StringComparison.Ordinal);
        text.ShouldNotContain(secretKey, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_typed_key_replays_only_the_same_payload()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var store = new CapturingNewIdempotencyStore(unitOfWork);
        var idempotency = new PaymentCommandIdempotency(store, unitOfWork, TimeProvider.System);
        var key = IdempotencyKey.From("  payment-key  ");
        var resourceId = Guid.CreateVersion7();
        var fingerprint = new string('A', 64);
        // Act
        var created = await idempotency.Execute("payment:test", key, fingerprint,
            () => Task.FromResult(Result.Ok(resourceId)), TestContext.Current.CancellationToken);
        var replay = await idempotency.Execute("payment:test", key, fingerprint,
            () => throw new InvalidOperationException("A replay must not execute the mutation."), TestContext.Current.CancellationToken);
        var changed = await idempotency.Execute("payment:test", key, new string('B', 64),
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
    }

    [Fact]
    public async Task An_uninitialized_key_is_rejected_before_locking_or_persistence()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var idempotency = new PaymentCommandIdempotency(new UnusedIdempotencyStore(), unitOfWork, TimeProvider.System);

        // Act
        var result = await idempotency.Execute("payment:test", default, new string('A', 64),
            () => throw new InvalidOperationException("An uninitialized key must not reach the mutation."), TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ResultStatus.Invalid);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
        result.ErrorDetails.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull().ContainsKey("Idempotency-Key").ShouldBeTrue();
    }

    [Fact]
    public void Instruction_commands_declare_stable_operation_and_resource_lock_names()
    {
        // Arrange
        var target = new PaymentInstructionTarget(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), 1);
        var input = new UpdatePaymentInstructionsDto { ExpectedDocumentVersion = 1, Reason = FinancialReasonDto.InstructionApproved, Confirmed = true };
        var key = IdempotencyKey.From("  payment-key  ");
        var command = new ApprovePaymentInstructionsCommand(target, input, "actor", key);
        var scope = "payment-instruction:Approved:" + target.DocumentId.ToString("N");

        // Act
        var names = command.TransactionLocks;
        var commandWithoutKey = command with
        {
            IdempotencyKey = default
        };
        var withoutKey = commandWithoutKey.TransactionLocks;

        // Assert
        names.ShouldBe([
            "admin:booking-finance:" + target.BookingId.ToString("N"),
            "admin:payment-account:" + target.AccountId.ToString("N"),
            "admin:payment-operation:" + ApplicationRequestFingerprint.Create(scope, key.Value)
        ]);
        withoutKey.ShouldBeEmpty();
    }
}
