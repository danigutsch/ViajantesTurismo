using SharedKernel.BuildingBlocks;
using JetBrains.Annotations;
using SharedKernel.Results;
using static ViajantesTurismo.Admin.Domain.Customers.CustomerErrors;
using SharedKernel.InputNormalization;

namespace ViajantesTurismo.Admin.Domain.Customers;

/// <summary>
/// Represents contact information of a customer.
/// </summary>
public sealed class ContactInfo : ValueObject
{
    private ContactInfo(EmailAddress email, PhoneNumber mobile, string? instagram, string? facebook)
    {
        Email = email;
        Mobile = mobile;
        Instagram = instagram;
        Facebook = facebook;
    }

    /// <summary>
    /// DO NOT USE. This constructor is required by Entity Framework Core for materialization.
    /// </summary>
    [UsedImplicitly]
    private ContactInfo()
    {
    }

    /// <summary>Email address, stored in lower case.</summary>
    public EmailAddress Email
    {
        get; private set;
    }

    /// <summary>Mobile phone number: its digits, led by + when it was entered with one.</summary>
    public PhoneNumber Mobile
    {
        get; private set;
    }

    /// <summary>Instagram handle.</summary>
    public string? Instagram
    {
        get; private set;
    }

    /// <summary>Facebook profile.</summary>
    public string? Facebook
    {
        get; private set;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="ContactInfo"/> class. The email is validated by <see cref="EmailAddress"/>
    /// and kept within <see cref="AdminDomainLimits.MaxNameLength"/>; the mobile is validated by <see cref="PhoneNumber"/>
    /// and kept within <see cref="AdminDomainLimits.MaxDefaultLength"/> characters as typed.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <param name="mobile">The mobile phone number.</param>
    /// <param name="instagram">The Instagram handle.</param>
    /// <param name="facebook">The Facebook profile.</param>
    /// <returns>A Result containing the ContactInfo.</returns>
    public static Result<ContactInfo> Create(string email, string mobile, string? instagram, string? facebook)
    {
        var sanitizedEmail = StringSanitizer.Sanitize(email);
        var sanitizedMobile = StringSanitizer.Sanitize(mobile);
        instagram = StringSanitizer.Sanitize(instagram);
        instagram = string.IsNullOrWhiteSpace(instagram) ? null : instagram;
        facebook = StringSanitizer.Sanitize(facebook);
        facebook = string.IsNullOrWhiteSpace(facebook) ? null : facebook;

        var errors = new ValidationErrors();
        var emailAddress = CreateEmail(errors, sanitizedEmail);
        var phoneNumber = CustomerPhoneNumbers.Create(errors, sanitizedMobile, EmptyMobile, MobileTooLong, InvalidPhoneFormat);

        if (instagram?.Length > AdminDomainLimits.MaxDefaultLength)
        {
            errors.Add(InstagramTooLong());
        }

        if (facebook?.Length > AdminDomainLimits.MaxDefaultLength)
        {
            errors.Add(FacebookTooLong());
        }

        if (errors.HasErrors)
        {
            return errors.ToResult<ContactInfo>();
        }

        return Result.Ok(new ContactInfo(emailAddress!, phoneNumber!, instagram, facebook));
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Email;
        yield return Mobile;
        yield return Instagram;
        yield return Facebook;
    }

    private static EmailAddress? CreateEmail(ValidationErrors errors, string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            errors.Add(EmptyEmail());
            return null;
        }

        if (email.Length > AdminDomainLimits.MaxNameLength)
        {
            errors.Add(EmailTooLong());
            return null;
        }

        var address = EmailAddress.Create(email);
        if (address.IsFailure)
        {
            errors.Add(InvalidEmailFormat());
            return null;
        }

        return address.Value;
    }
}
