namespace ViajantesTurismo.Admin.Domain;

/// <summary>
/// Represents an emergency contact.
/// </summary>
public sealed class EmergencyContact
{
    /// <summary>Emergency contact name.</summary>
    public required string Name { get; init; }

    /// <summary>Emergency contact mobile (with DDD).</summary>
    public required string Mobile { get; init; }
}