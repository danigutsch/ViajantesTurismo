namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>A bounded page of independent reviews of one payment instruction revision.</summary>
public sealed record GetPaymentInstructionReviewPageDto
{
    /// <summary>Gets the independent reviews in recorded order.</summary>
    public required IReadOnlyList<GetFinancialReviewDto> Items
    {
        get; init;
    }

    /// <summary>Gets the one-based page number returned by the server.</summary>
    public required int Page
    {
        get; init;
    }

    /// <summary>Gets the maximum number of reviews per page.</summary>
    public required int PageSize
    {
        get; init;
    }

    /// <summary>Gets the total number of independent reviews for the instruction revision.</summary>
    public required int Total
    {
        get; init;
    }
}
