using JetBrains.Annotations;
using PandaBear.BuildingBlocks;
using PandaBear.Results;
using static ViajantesTurismo.Admin.Domain.Customers.CustomerErrors;
using PandaBear.InputNormalization;

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
    private EmergencyContact(string name, PhoneNumber mobile)
    {
        Name = name;
        Mobile = mobile;
    }

    /// <summary>
    /// DO NOT USE. This constructor is required by Entity Framework Core for materialization.
    /// </summary>
    [UsedImplicitly]
    private EmergencyContact()
    {
    }

    /// <summary>Emergency contact name.</summary>
    public string Name
    {
        get; private set;
    }

    /// <summary>Emergency contact mobile (with DDD): its digits, led by + when it was entered with one.</summary>
    public PhoneNumber Mobile
    {
        get; private set;
    }

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
        else if (sanitizedName.Length > AdminDomainLimits.MaxNameLength)
        {
            errors.Add(EmergencyContactNameTooLong());
        }

        var phoneNumber = CustomerPhoneNumbers.Create(
            errors,
            sanitizedMobile,
            EmptyEmergencyContactMobile,
            EmergencyContactMobileTooLong,
            InvalidEmergencyContactPhoneFormat);

        if (errors.HasErrors)
        {
            return errors.ToResult<EmergencyContact>();
        }

        return Result.Ok(new EmergencyContact(sanitizedName!, phoneNumber!));
    }
}
