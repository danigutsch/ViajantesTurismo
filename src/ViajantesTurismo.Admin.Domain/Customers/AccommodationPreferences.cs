using JetBrains.Annotations;
using ViajantesTurismo.Common.Results;

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
    private AccommodationPreferences(RoomType roomType, BedType bedType, int? companionId)
    {
        RoomType = roomType;
        BedType = bedType;
        CompanionId = companionId;
    }

    /// <summary>Room type.</summary>
    public RoomType RoomType { get; private set; }

    /// <summary>Bed type.</summary>
    public BedType BedType { get; private set; }

    /// <summary>Companion's ID.</summary>
    public int? CompanionId { get; private set; }

    /// <summary>
    /// Creates a new instance of <see cref="AccommodationPreferences"/>.
    /// </summary>
    /// <param name="roomType">The room type.</param>
    /// <param name="bedType">The bed type.</param>
    /// <param name="companionId">The companion's ID.</param>
    /// <returns>A <see cref="Result{AccommodationPreferences}"/> containing the accommodation preferences.</returns>
    public static Result<AccommodationPreferences> Create(RoomType roomType, BedType bedType, int? companionId)
    {
        return new AccommodationPreferences(roomType, bedType, companionId);
    }

    /// <summary>
    /// DO NOT USE. This constructor is required by Entity Framework Core for materialization.
    /// </summary>
#pragma warning disable CS8618
    [UsedImplicitly]
    private AccommodationPreferences()
    {
    }
}