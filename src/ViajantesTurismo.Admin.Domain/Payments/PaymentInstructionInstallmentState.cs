namespace ViajantesTurismo.Admin.Domain.Payments;

/// <summary>Persisted installment selected by a frozen payment instruction.</summary>
/// <param name="InstallmentId">Identifier of the selected installment.</param>
/// <param name="DueDate">Civil calendar due date.</param>
/// <param name="EuroAmount">Outstanding EUR amount selected.</param>
/// <param name="SettlementAmount">Settlement-currency amount assigned to this installment.</param>
/// <param name="RoundingAdjustment">Difference from rounding this installment independently.</param>
internal sealed record PaymentInstructionInstallmentState(Guid InstallmentId, DateOnly DueDate, decimal EuroAmount, decimal SettlementAmount, decimal RoundingAdjustment);
