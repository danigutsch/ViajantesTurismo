using JetBrains.Annotations;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Common.Results;
using ViajantesTurismo.Common.Sanitizers;
using static ViajantesTurismo.Admin.Domain.Customers.CustomerErrors;

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
    private EmergencyContact(string name, string mobile)
    {
        Name = name;
        Mobile = mobile;
    }

    /// <summary>Emergency contact name.</summary>
    public string Name { get; private set; }

    /// <summary>Emergency contact mobile (with DDD).</summary>
    public string Mobile { get; private set; }

    /// <summary>
    /// Creates a new instance of <see cref="EmergencyContact"/> with validation.
    /// </summary>
    /// <param name="name">The emergency contact name.</param>
    /// <param name="mobile">The emergency contact mobile.</param>
    /// <returns>A <see cref="Result{EmergencyContact}"/> containing the emergency contact or validation errors.</returns>
    public static Result<EmergencyContact> Create(string? name, string? mobile)
    {
        var sanitizedName = StringSanitizer.Sanitize(name);
        var sanitizedMobile = StringSanitizer.Sanitize(mobile);

        var errors = new ValidationErrors();

        if (string.IsNullOrWhiteSpace(sanitizedName))
        {
            errors.Add(EmptyEmergencyContactName());
        }
        else if (sanitizedName.Length > ContractConstants.MaxNameLength)
        {
            errors.Add(EmergencyContactNameTooLong());
        }

        if (string.IsNullOrWhiteSpace(sanitizedMobile))
        {
            errors.Add(EmptyEmergencyContactMobile());
        }
        else if (sanitizedMobile.Length > ContractConstants.MaxDefaultLength)
        {
            errors.Add(EmergencyContactMobileTooLong());
        }

        if (errors.HasErrors)
        {
            return errors.ToResult<EmergencyContact>();
        }

        return new EmergencyContact(sanitizedName!, sanitizedMobile!);
    }

    /// <summary>
    /// DO NOT USE. This constructor is required by Entity Framework Core for materialization.
    /// </summary>
#pragma warning disable CS8618
    [UsedImplicitly]
    private EmergencyContact()
    {
    }
}