namespace ViajantesTurismo.Admin.Infrastructure.Documents;

/// <summary>Terminal outcomes in the document observability contract.</summary>
public enum DocumentOperationOutcome
{
    /// <summary>Completed normally.</summary>
    Succeeded,

    /// <summary>Returned a business or input rejection.</summary>
    Rejected,

    /// <summary>Failed for operational reasons.</summary>
    Failed,

    /// <summary>Stopped due to caller or host cancellation.</summary>
    Canceled,
}
