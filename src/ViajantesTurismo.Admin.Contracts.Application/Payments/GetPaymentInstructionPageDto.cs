namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>A bounded page of masked payment instruction revisions, newest first.</summary>
public sealed record GetPaymentInstructionPageDto
{
    /// <summary>Gets the masked instruction revisions and their existing financial reviews.</summary>
    public required IReadOnlyList<GetPaymentInstructionDto> Items
    {
        get; init;
    }

    /// <summary>Gets the one-based page number returned by the server.</summary>
    public required int Page
    {
        get; init;
    }

    /// <summary>Gets the maximum number of revisions per page.</summary>
    public required int PageSize
    {
        get; init;
    }

    /// <summary>Gets the total number of retained payment instruction revisions for this booking.</summary>
    public required int Total
    {
        get; init;
    }
}
