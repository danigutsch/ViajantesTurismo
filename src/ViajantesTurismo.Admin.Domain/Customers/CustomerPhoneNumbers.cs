using PandaBear.BuildingBlocks;
using PandaBear.Results;

namespace ViajantesTurismo.Admin.Domain.Customers;

/// <summary>
/// Validates a customer phone number with <see cref="PhoneNumber"/> and reports failures as the customer error of the
/// field being validated, so each form field keeps its own message.
/// </summary>
internal static class CustomerPhoneNumbers
{
    /// <summary>Creates the phone number or adds the matching field error.</summary>
    /// <param name="errors">The collected validation errors.</param>
    /// <param name="sanitized">The sanitized input.</param>
    /// <param name="empty">Creates the error for a missing number.</param>
    /// <param name="tooLong">Creates the error for input longer than <see cref="AdminDomainLimits.MaxDefaultLength"/>.</param>
    /// <param name="invalid">Creates the error for input that is not a phone number.</param>
    /// <returns>The phone number, or <see langword="null"/> when an error was added.</returns>
    public static PhoneNumber? Create(
        ValidationErrors errors,
        string? sanitized,
        Func<Result> empty,
        Func<Result> tooLong,
        Func<Result> invalid)
    {
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            errors.Add(empty());
            return null;
        }

        if (sanitized.Length > AdminDomainLimits.MaxDefaultLength)
        {
            errors.Add(tooLong());
            return null;
        }

        var phoneNumber = PhoneNumber.Create(sanitized);
        if (phoneNumber.IsFailure)
        {
            errors.Add(invalid());
            return null;
        }

        return phoneNumber.Value;
    }
}
