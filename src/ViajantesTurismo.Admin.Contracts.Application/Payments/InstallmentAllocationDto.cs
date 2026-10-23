namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Selects an installment or existing allocation and an exact EUR credit.</summary>
public sealed record InstallmentAllocationDto
{
    /// <summary>Gets TargetId.</summary>
    public required Guid TargetId
    {
        get; init;
    }
    /// <summary>Gets EuroAmount.</summary>
    public required decimal EuroAmount
    {
        get; init;
    }
    /// <inheritdoc />
    public override string ToString() => "InstallmentAllocationDto [redacted]";
}
