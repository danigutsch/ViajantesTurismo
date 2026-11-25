using Microsoft.EntityFrameworkCore;
using SharedKernel.Idempotency;
using SharedKernel.Results;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Application.Payments;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Infrastructure.Payments;
using ViajantesTurismo.Admin.UnitTests.Infrastructure;

namespace ViajantesTurismo.Admin.UnitTests.PaymentAccounts;

[Trait(TestTraitNames.CategoryName, TestTraits.CoreBehaviorCategory)]
[Trait(TestTraitNames.CapabilityName, Testing.AdminTestTraitValues.PaymentsCapability)]
public sealed class PaymentConcurrencyBehaviorTests
{
    [Fact]
    public async Task An_optimistic_concurrency_failure_returns_the_payment_revision_conflict()
    {
        // Arrange
        var command = new RetirePaymentAccountCommand(Guid.CreateVersion7(),
            new RetirePaymentAccountDto { ExpectedVersion = 2, Confirmed = true, Reason = PaymentAccountReasonDto.NoLongerUsed },
            "actor", IdempotencyKey.From("retire"));
        var behavior = new PaymentConcurrencyBehavior<RetirePaymentAccountCommand, Result<Guid>>();

        // Act
        var result = await behavior.Handle(command, () => ValueTask.FromException<Result<Guid>>(new DbUpdateConcurrencyException()),
            TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ResultStatus.Conflict);
        result.ErrorDetails.ShouldNotBeNull().Detail.ShouldBe("The payment record changed. Reload and confirm the current revision.");
    }
}
