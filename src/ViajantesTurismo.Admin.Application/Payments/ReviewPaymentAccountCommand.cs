using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Requests an explicit ReviewPaymentAccount account operation.</summary>
public sealed record ReviewPaymentAccountCommand(Guid AccountId, ReviewPaymentAccountDto Input, string ActorId, string? IdempotencyKey)
{
    /// <inheritdoc />
    public override string ToString() => "ReviewPaymentAccountCommand [redacted]";
}
