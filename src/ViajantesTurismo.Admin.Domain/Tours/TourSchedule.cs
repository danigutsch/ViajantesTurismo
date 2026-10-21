using PandaBear.BuildingBlocks;
using PandaBear.Results;

namespace ViajantesTurismo.Admin.Domain.Tours;

/// <summary>Represents the calendar dates of an Admin tour.</summary>
public sealed class TourSchedule : ValueObject
{
    private TourSchedule(DateOnly startDate, DateOnly endDate)
    {
        StartDate = startDate;
        EndDate = endDate;
    }

    /// <summary>Gets the first tour date.</summary>
    public DateOnly StartDate { get; }

    /// <summary>Gets the last tour date.</summary>
    public DateOnly EndDate { get; }

    /// <summary>Gets the number of days between the start and end dates.</summary>
    public int DurationDays => EndDate.DayNumber - StartDate.DayNumber;

    /// <summary>Creates a schedule whose end date follows its start date.</summary>
    /// <param name="startDate">The first tour date.</param>
    /// <param name="endDate">The last tour date.</param>
    /// <returns>The schedule or a validation error.</returns>
    public static Result<TourSchedule> Create(DateOnly startDate, DateOnly endDate)
    {
        if (endDate <= startDate)
        {
            return Result.Invalid<TourSchedule>("End date must be after start date.", "schedule", "End date must be after start date.");
        }

        return Result.Ok(new TourSchedule(startDate, endDate));
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return StartDate;
        yield return EndDate;
    }
}
