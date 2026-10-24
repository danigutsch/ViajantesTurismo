using JetBrains.Annotations;
using PandaBear.Results;
using ViajantesTurismo.Admin.Domain.Shared;

namespace ViajantesTurismo.Admin.Domain.Customers;

/// <summary>
/// Represents accommodation preferences for a customer.
/// </summary>
public sealed class AccommodationPreferences
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AccommodationPreferences"/> class.
    /// </summary>
    /// <param name="roomType">The room type.</param>
    /// <param name="bedType">The bed type.</param>
    /// <param name="companionId">The companion's ID.</param>
    private AccommodationPreferences(RoomType roomType, BedType bedType, Guid? companionId)
    {
        RoomType = roomType;
        BedType = bedType;
        CompanionId = companionId;
    }

    /// <summary>
    /// DO NOT USE. This constructor is required by Entity Framework Core for materialization.
    /// </summary>
    [UsedImplicitly]
    private AccommodationPreferences()
    {
    }

    /// <summary>Room type.</summary>
    public RoomType RoomType
    {
        get; private set;
    }

    /// <summary>Bed type.</summary>
    public BedType BedType
    {
        get; private set;
    }

    /// <summary>Companion's ID.</summary>
    public Guid? CompanionId
    {
        get; private set;
    }

    /// <summary>
    /// Creates a new instance of <see cref="AccommodationPreferences"/>.
    /// </summary>
    /// <param name="roomType">The room type.</param>
    /// <param name="bedType">The bed type.</param>
    /// <param name="companionId">The companion's ID.</param>
    /// <returns>The validated accommodation preferences or validation errors.</returns>
    public static Result<AccommodationPreferences> Create(RoomType roomType, BedType bedType, Guid? companionId)
    {
        var errors = new ValidationErrors();
        if (!Enum.IsDefined(roomType))
        {
            errors.Add(CustomerErrors.InvalidRoomType());
        }

        if (!Enum.IsDefined(bedType))
        {
            errors.Add(CustomerErrors.InvalidBedType());
        }

        if (errors.HasErrors)
        {
            return errors.ToResult<AccommodationPreferences>();
        }

        return Result.Ok(new AccommodationPreferences(roomType, bedType, companionId));
    }
}
