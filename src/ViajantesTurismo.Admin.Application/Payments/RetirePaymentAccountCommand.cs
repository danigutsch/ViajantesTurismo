using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Requests an explicit RetirePaymentAccount account operation.</summary>
public sealed record RetirePaymentAccountCommand(Guid AccountId, RetirePaymentAccountDto Input, string ActorId, string? IdempotencyKey)
{
    /// <inheritdoc />
    public override string ToString() => "RetirePaymentAccountCommand [redacted]";
}
