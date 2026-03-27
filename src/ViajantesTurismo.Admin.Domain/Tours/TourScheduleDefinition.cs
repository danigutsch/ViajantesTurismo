namespace ViajantesTurismo.Admin.Domain.Tours;

/// <summary>
/// Defines the schedule requested for a tour.
/// </summary>
/// <param name="StartDate">The scheduled start date.</param>
/// <param name="EndDate">The scheduled end date.</param>
public sealed record TourScheduleDefinition(DateTime StartDate, DateTime EndDate);
