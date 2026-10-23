namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Identifies an immutable schedule revision's lifecycle state.</summary>
public enum PaymentScheduleStatusDto
{
    /// <summary>Unapproved terms.</summary>
    Draft,
    /// <summary>Current explicitly approved terms.</summary>
    Approved,
    /// <summary>Historical approved terms replaced explicitly.</summary>
    Superseded
}
