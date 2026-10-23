namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Defines payment account status labels.</summary>
public enum PaymentAccountStatusDto
{
    /// <summary>An unverified recipient revision.</summary>
    Draft = 0,
    /// <summary>An explicitly attested recipient revision.</summary>
    Verified = 1,
    /// <summary>A permanently retired account.</summary>
    Inactive = 2,
}
