namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Requests a confirmed change to one allowlisted document maintenance job.</summary>
public sealed record CreateDocumentMaintenanceControlDto
{
    /// <summary>Gets the identifier reused when retrying the same request.</summary>
    public Guid RequestId
    {
        get;
        init;
    }

    /// <summary>Gets whether the operator confirmed the change.</summary>
    public bool Confirmed
    {
        get;
        init;
    }

    /// <summary>Gets the reason to retain in the operations audit.</summary>
    public string? Reason
    {
        get;
        init;
    }

    /// <summary>Gets the failed execution targeted by a retry.</summary>
    public Guid? FailedRunId
    {
        get;
        init;
    }

    /// <inheritdoc />
    public override string ToString() => "CreateDocumentMaintenanceControlDto [redacted]";
}
