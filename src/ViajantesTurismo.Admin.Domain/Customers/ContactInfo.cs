using JetBrains.Annotations;
using ViajantesTurismo.AdminApi.Contracts;
using ViajantesTurismo.Common;
using ViajantesTurismo.Common.Results;
using static ViajantesTurismo.Admin.Domain.Customers.CustomerErrors;

namespace ViajantesTurismo.Admin.Domain.Customers;

/// <summary>
/// Represents contact information of a customer.
/// </summary>
public sealed class ContactInfo
{
    private ContactInfo(string email, string mobile, string? instagram, string? facebook)
    {
        Email = email;
        Mobile = mobile;
        Instagram = instagram;
        Facebook = facebook;
    }

    /// <summary>Email address.</summary>
    public string Email { get; private set; }

    /// <summary>Mobile phone number.</summary>
    public string Mobile { get; private set; }

    /// <summary>Instagram handle.</summary>
    public string? Instagram { get; private set; }

    /// <summary>Facebook profile.</summary>
    public string? Facebook { get; private set; }

    /// <summary>
    /// Creates a new instance of the <see cref="ContactInfo"/> class.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <param name="mobile">The mobile phone number.</param>
    /// <param name="instagram">The Instagram handle.</param>
    /// <param name="facebook">The Facebook profile.</param>
    /// <returns>A Result containing the ContactInfo.</returns>
    public static Result<ContactInfo> Create(string email, string mobile, string? instagram, string? facebook)
    {
        email = StringSanitizer.Sanitize(email);
        mobile = StringSanitizer.Sanitize(mobile);
        instagram = StringSanitizer.Sanitize(instagram);
        instagram = string.IsNullOrWhiteSpace(instagram) ? null : instagram;
        facebook = StringSanitizer.Sanitize(facebook);
        facebook = string.IsNullOrWhiteSpace(facebook) ? null : facebook;

        var errors = new ValidationErrors();

        if (string.IsNullOrWhiteSpace(email))
        {
            errors.Add(EmptyEmail());
        }
        else if (email.Length > ContractConstants.MaxNameLength)
        {
            errors.Add(EmailTooLong());
        }

        if (string.IsNullOrWhiteSpace(mobile))
        {
            errors.Add(EmptyMobile());
        }
        else if (mobile.Length > ContractConstants.MaxDefaultLength)
        {
            errors.Add(MobileTooLong());
        }

        if (instagram?.Length > ContractConstants.MaxDefaultLength)
        {
            errors.Add(InstagramTooLong());
        }

        if (facebook?.Length > ContractConstants.MaxDefaultLength)
        {
            errors.Add(FacebookTooLong());
        }

        if (errors.HasErrors)
        {
            return errors.ToResult<ContactInfo>();
        }

        return new ContactInfo(email, mobile, instagram, facebook);
    }

    /// <summary>
    /// DO NOT USE. This constructor is required by Entity Framework Core for materialization.
    /// </summary>
#pragma warning disable CS8618
    [UsedImplicitly]
    private ContactInfo()
    {
    }
}