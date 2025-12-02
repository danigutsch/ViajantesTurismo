using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Admin.Contracts;

/// <summary>
/// Provides centralised tour validation for DTO validation.
/// </summary>
public static class TourValidation
{
    /// <summary>
    /// Validates that the tour duration meets the minimum requirement.
    /// </summary>
    /// <param name="startDate">The tour start date.</param>
    /// <param name="endDate">The tour end date.</param>
    /// <param name="minimumDays">The minimum required duration in days.</param>
    /// <param name="startDateMemberName">The name of the start date property.</param>
    /// <param name="endDateMemberName">The name of the end date property.</param>
    /// <returns>A validation result if invalid, or null if valid.</returns>
    public static ValidationResult? ValidateDuration(
        DateTime startDate,
        DateTime endDate,
        int minimumDays,
        string startDateMemberName,
        string endDateMemberName)
    {
        var duration = (endDate - startDate).TotalDays;
        return duration <= minimumDays
            ? new ValidationResult(
                $"The tour must be at least {minimumDays} days long. End date must be more than {minimumDays} days after start date.",
                [startDateMemberName, endDateMemberName])
            : null;
    }
}
