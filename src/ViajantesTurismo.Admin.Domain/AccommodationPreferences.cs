using ViajantesTurismo.Common;

namespace ViajantesTurismo.Admin.Domain;

/// <summary>
/// Represents accommodation preferences for a customer.
/// </summary>
public sealed class AccommodationPreferences
{
    /// <summary>Room type.</summary>
    public required RoomType RoomType { get; init; }

    /// <summary>Bed type.</summary>
    public required string BedType { get; init; }

    /// <summary>Companion's first name.</summary>
    public string? CompanionFirstName { get; init; }

    /// <summary>Companion's last name.</summary>
    public string? CompanionLastName { get; init; }
}
