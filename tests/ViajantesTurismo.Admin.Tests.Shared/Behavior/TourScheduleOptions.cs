namespace ViajantesTurismo.Admin.Tests.Shared.Behavior;

/// <summary>
/// Options for overriding tour schedule values in tests.
/// </summary>
/// <param name="StartDate">The start date override.</param>
/// <param name="EndDate">The end date override.</param>
public sealed record TourScheduleOptions(DateTime? StartDate = null, DateTime? EndDate = null);
