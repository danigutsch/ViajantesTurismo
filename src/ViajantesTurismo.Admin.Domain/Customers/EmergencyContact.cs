using JetBrains.Annotations;

namespace ViajantesTurismo.Admin.Domain.Customers;

/// <summary>
/// Represents an emergency contact.
/// </summary>
public sealed class EmergencyContact
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EmergencyContact"/> class.
    /// </summary>
    /// <param name="name">The emergency contact name.</param>
    /// <param name="mobile">The emergency contact mobile.</param>
    public EmergencyContact(string name, string mobile)
    {
        Name = name;
        Mobile = mobile;
    }

    /// <summary>Emergency contact name.</summary>
    public string Name { get; private set; }

    /// <summary>Emergency contact mobile (with DDD).</summary>
    public string Mobile { get; private set; }

    /// <summary>
    /// DO NOT USE. This constructor is required by Entity Framework Core for materialization.
    /// </summary>
#pragma warning disable CS8618
    [UsedImplicitly]
    private EmergencyContact()
    {
    }
}
