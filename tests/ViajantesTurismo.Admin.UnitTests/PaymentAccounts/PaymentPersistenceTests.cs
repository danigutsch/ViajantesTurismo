using TestTraits = ViajantesTurismo.Admin.UnitTests.Infrastructure.TestTraits;
using ViajantesTurismo.Admin.Domain.Payments;

namespace ViajantesTurismo.Admin.UnitTests.PaymentAccounts;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.PersistenceCategory)]
[Trait(PandaBear.Testing.TestTraitNames.ScopeName, TestTraits.UnitScope)]
[Trait(PandaBear.Testing.SharedKernelTestTraitNames.CapabilityName, TestTraits.PaymentsCapability)]
public sealed class PaymentPersistenceTests
{
    [Fact]
    public void Corrupt_nested_installments_are_rejected_without_materializing_invalid_history()
    {
        // Arrange
        var scenario = new PaymentScenario();
        scenario.ApproveSchedule();
        var original = scenario.History.ExportState();
        var corrupt = original with
        {
            Schedules = [original.Schedules[0] with { Installments = [null!] }]
        };

        // Act
        var restored = BookingFinancialHistory.Restore(corrupt);

        // Assert
        restored.IsFailure.ShouldBeTrue();
    }
}
