namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Identifies the immutable result of a financial command without retaining response payloads in idempotency storage.</summary>
public sealed record GetPaymentCommandResultDto
{
    /// <summary>Gets the booking identifier.</summary>
    public required Guid BookingId
    {
        get; init;
    }
    /// <summary>Gets the created or approved immutable financial resource.</summary>
    public required Guid ResourceId
    {
        get; init;
    }
}
