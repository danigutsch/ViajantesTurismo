using ViajantesTurismo.Common;

namespace ViajantesTurismo.ApiService;

internal sealed class Tour(string name, DateTime startDate, DateTime endDate) : Entity<int>
{
    public string Name { get; init; } = name;
    public DateTime StartDate { get; init; } = startDate;
    public DateTime EndDate { get; init; } = endDate;
}