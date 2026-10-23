namespace ViajantesTurismo.Admin.Domain.Payments;

internal sealed record PaymentInstructionInstallmentState(Guid InstallmentId, DateOnly DueDate, decimal EuroAmount, decimal SettlementAmount, decimal RoundingAdjustment);
