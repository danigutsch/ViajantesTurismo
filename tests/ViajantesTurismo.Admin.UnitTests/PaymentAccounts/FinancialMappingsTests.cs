using ViajantesTurismo.Admin.Application.Mappings;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.UnitTests.PaymentAccounts;

public sealed class FinancialMappingsTests
{
    [Fact]
    public void Valid_allocations_are_kept_in_the_order_given()
    {
        // Arrange
        var first = Guid.CreateVersion7();
        var second = Guid.CreateVersion7();

        // Act
        var result = FinancialMappings.Allocations(
        [
            new InstallmentAllocationDto { TargetId = first, EuroAmount = 10m },
            new InstallmentAllocationDto { TargetId = second, EuroAmount = 20m },
        ]);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Select(allocation => allocation.TargetId).ShouldBe([first, second]);
    }

    [Fact]
    public void Valid_installments_are_kept_in_the_order_given()
    {
        // Act
        var result = FinancialMappings.Installments(
        [
            new PaymentInstallmentDto { EuroAmount = 10m, DueDate = new DateOnly(2030, 1, 5) },
            new PaymentInstallmentDto { EuroAmount = 20m, DueDate = new DateOnly(2030, 2, 5) },
        ]);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Select(installment => installment.DueDate).ShouldBe([new DateOnly(2030, 1, 5), new DateOnly(2030, 2, 5)]);
    }

    [Fact]
    public void One_missing_entry_among_valid_ones_rejects_the_whole_list()
    {
        // Act
        var allocations = FinancialMappings.Allocations([new InstallmentAllocationDto { TargetId = Guid.CreateVersion7(), EuroAmount = 10m }, null!]);
        var installments = FinancialMappings.Installments([new PaymentInstallmentDto { EuroAmount = 10m, DueDate = new DateOnly(2030, 1, 5) }, null!]);

        // Assert
        allocations.IsFailure.ShouldBeTrue();
        installments.IsFailure.ShouldBeTrue();
    }
}
