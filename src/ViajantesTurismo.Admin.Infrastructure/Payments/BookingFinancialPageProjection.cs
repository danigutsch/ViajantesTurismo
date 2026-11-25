using SharedKernel.BuildingBlocks;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Infrastructure.Payments;

/// <summary>Selects bounded windows from the decoded booking snapshot before mapping records.</summary>
/// <param name="request">The section, parent record and page to read.</param>
internal sealed class BookingFinancialPageProjection(BookingFinancialPageRequestDto request)
{
    private const int NestedPreviewSize = 5;
    private readonly List<BookingFinancialPageWindowDto> _windows = [];

    /// <summary>Gets the windows supplied in the response.</summary>
    public IReadOnlyList<BookingFinancialPageWindowDto> Windows => _windows;

    /// <summary>Gets whether the requested nested record is absent from this booking.</summary>
    public bool ParentMissing => request.ParentId.HasValue && !_windows.Any(window => window.Section == request.Section && window.ParentId == request.ParentId);

    /// <summary>Returns whether the view includes the section.</summary>
    public bool Includes(BookingFinancialSection section) => request.Section is null || request.Section == section;

    /// <summary>Returns whether only the specified section was requested.</summary>
    public bool Requests(BookingFinancialSection section) => request.Section == section;

    /// <summary>Selects the requested window of a top-level section.</summary>
    public IEnumerable<T> Items<T>(IReadOnlyList<T> items, BookingFinancialSection section) =>
        Includes(section) ? Slice(items, section, null) : [];

    /// <summary>Selects the requested records or the parent targeted by a child page.</summary>
    public IEnumerable<T> Parents<T>(IReadOnlyList<T> items, BookingFinancialSection section, BookingFinancialSection childSection, Func<T, Guid> id) =>
        Requests(childSection) ? items.Where(item => id(item) == request.ParentId) : Items(items, section);

    /// <summary>Selects the nested preview or the requested window of a record's children.</summary>
    public IEnumerable<T> Children<T>(IReadOnlyList<T> items, BookingFinancialSection section, Guid parentId) =>
        Slice(items, section, parentId);

    /// <summary>Selects indices in the combined reversal and correction history.</summary>
    public IEnumerable<int> Reversals(int count) =>
        Includes(BookingFinancialSection.Reversals)
            ? Indices(Window(count, BookingFinancialSection.Reversals, null))
            : [];

    /// <summary>Gets the requested owning record identifier.</summary>
    public Guid? ParentId => request.ParentId;

    private IEnumerable<T> Slice<T>(IReadOnlyList<T> items, BookingFinancialSection section, Guid? parentId)
    {
        var window = Window(items.Count, section, parentId);
        return Indices(window).Select(index => items[index]);
    }

    private PageWindow Window(int count, BookingFinancialSection section, Guid? parentId)
    {
        var requested = Requests(section);
        var size = parentId.HasValue ? NestedPreviewSize : ContractConstants.DefaultBookingFinancialPageSize;
        if (requested || !parentId.HasValue)
        {
            size = request.PageSize ?? size;
        }
        var window = PageWindow.Create(requested ? request.Page : null, size, count);
        _windows.Add(new BookingFinancialPageWindowDto(section, parentId, window.Page, window.PageSize, window.Total, window.PageCount));
        return window;
    }

    private static IEnumerable<int> Indices(PageWindow window) => Enumerable.Range(window.Skip, Math.Min(window.Take, window.Total - window.Skip));
}
