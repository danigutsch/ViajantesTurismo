namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Requests the initial financial view or a window within a particular section.</summary>
public sealed record BookingFinancialPageRequestDto
{
    /// <summary>Gets the section to page. A missing section requests the initial windows.</summary>
    public BookingFinancialSection? Section
    {
        get; init;
    }

    /// <summary>Gets the requested page number, starting at one.</summary>
    public int? Page
    {
        get; init;
    }

    /// <summary>Gets the maximum number of records per requested window.</summary>
    public int? PageSize
    {
        get; init;
    }

    /// <summary>Gets the owning record for a nested section.</summary>
    public Guid? ParentId
    {
        get; init;
    }
}
