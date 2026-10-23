using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Requests an unverified recipient draft; actor identity comes from the authenticated principal.</summary>
public sealed record CreatePaymentAccountCommand(CreatePaymentAccountDto Input, string ActorId, string? IdempotencyKey)
{
    /// <inheritdoc />
    public override string ToString() => "CreatePaymentAccountCommand [redacted]";
}
