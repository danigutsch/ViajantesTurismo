using JetBrains.Annotations;
using ViajantesTurismo.Common;

namespace ViajantesTurismo.Admin.Domain.Customers;

/// <summary>
/// Represents contact information of a customer.
/// </summary>
public sealed class ContactInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContactInfo"/> class.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <param name="mobile">The mobile phone number.</param>
    /// <param name="instagram">The Instagram handle.</param>
    /// <param name="facebook">The Facebook profile.</param>
    public ContactInfo(string email, string mobile, string? instagram, string? facebook)
    {
        Email = StringSanitizer.Sanitize(email);
        Mobile = StringSanitizer.Sanitize(mobile);
        Instagram = StringSanitizer.Sanitize(instagram);
        Facebook = StringSanitizer.Sanitize(facebook);
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
    /// DO NOT USE. This constructor is required by Entity Framework Core for materialization.
    /// </summary>
#pragma warning disable CS8618
    [UsedImplicitly]
    private ContactInfo()
    {
    }
}
