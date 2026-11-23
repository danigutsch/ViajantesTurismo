namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>A bounded page of masked payment account summaries in stable identifier order.</summary>
public sealed record GetPaymentAccountPageDto
{
    /// <summary>Gets the masked account summaries for this page.</summary>
    public required IReadOnlyList<GetPaymentAccountDto> Items
    {
        get; init;
    }

    /// <summary>Gets the one-based page number returned by the server.</summary>
    public required int Page
    {
        get; init;
    }

    /// <summary>Gets the maximum number of accounts per page.</summary>
    public required int PageSize
    {
        get; init;
    }

    /// <summary>Gets the number of accounts matching the requested filter.</summary>
    public required int Total
    {
        get; init;
    }
}
