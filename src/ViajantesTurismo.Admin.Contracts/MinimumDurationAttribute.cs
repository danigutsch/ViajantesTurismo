using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Admin.Contracts;

/// <summary>
/// Validates that a tour has a minimum duration between start and end dates.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class MinimumDurationAttribute : ValidationAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MinimumDurationAttribute"/> class.
    /// </summary>
    /// <param name="minimumDays">The minimum number of days required between start and end dates.</param>
    /// <param name="startDatePropertyName">The name of the property containing the start date.</param>
    /// <param name="endDatePropertyName">The name of the property containing the end date.</param>
    public MinimumDurationAttribute(int minimumDays, string startDatePropertyName = "StartDate", string endDatePropertyName = "EndDate")
    {
        MinimumDays = minimumDays;
        StartDatePropertyName = startDatePropertyName;
        EndDatePropertyName = endDatePropertyName;
        ErrorMessage = $"The tour must be at least {minimumDays} days long. End date must be more than {minimumDays} days after start date.";
    }

    /// <summary>
    /// Gets the minimum number of days required between start and end dates.
    /// </summary>
    public int MinimumDays { get; }

    /// <summary>
    /// Gets the name of the property containing the start date.
    /// </summary>
    public string StartDatePropertyName { get; }

    /// <summary>
    /// Gets the name of the property containing the end date.
    /// </summary>
    public string EndDatePropertyName { get; }

    /// <inheritdoc/>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        ArgumentNullException.ThrowIfNull(validationContext);

        if (value == null)
        {
            return ValidationResult.Success;
        }

        var startDateProperty = validationContext.ObjectType.GetProperty(StartDatePropertyName);
        var endDateProperty = validationContext.ObjectType.GetProperty(EndDatePropertyName);

        if (startDateProperty == null || endDateProperty == null)
        {
            return new ValidationResult($"Properties '{StartDatePropertyName}' or '{EndDatePropertyName}' not found.");
        }

        var startDateValue = startDateProperty.GetValue(validationContext.ObjectInstance);
        var endDateValue = endDateProperty.GetValue(validationContext.ObjectInstance);

        if (startDateValue is not DateTime startDate || endDateValue is not DateTime endDate)
        {
            return ValidationResult.Success;
        }

        var duration = (endDate - startDate).TotalDays;

        if (duration <= MinimumDays)
        {
            return new ValidationResult(
                ErrorMessage ?? $"The tour must be at least {MinimumDays} days long.",
                [StartDatePropertyName, EndDatePropertyName]);
        }

        return ValidationResult.Success;
    }
}