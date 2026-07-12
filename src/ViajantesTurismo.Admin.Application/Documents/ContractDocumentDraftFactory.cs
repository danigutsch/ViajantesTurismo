using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>
/// Builds customer-facing booking confirmation contract drafts from read-side source data.
/// </summary>
internal static class ContractDocumentDraftFactory
{
    public static Result<DocumentDraft> Create(
        GetBookingDto booking,
        GetTourDto tour,
        string templateId,
        string templateVersion,
        DocumentBrandingSnapshotValues branding,
        DateTime now)
    {
        ArgumentNullException.ThrowIfNull(booking);
        ArgumentNullException.ThrowIfNull(tour);
        ArgumentNullException.ThrowIfNull(branding);

        var fieldsResult = CreateFields(booking, tour);
        if (fieldsResult.IsFailure)
        {
            return fieldsResult.ConvertError<IReadOnlyList<DocumentField>, DocumentDraft>();
        }

        return DocumentDraft.Create(
            booking.Id,
            DocumentType.BookingConfirmationContract,
            DocumentAudience.Customer,
            templateId,
            templateVersion,
            CreateSourceVersion(booking, tour),
            fieldsResult.Value,
            branding.Version,
            branding.BrandName,
            branding.LogoUri,
            branding.PrimaryColor,
            branding.AccentColor,
            branding.BackgroundColor,
            branding.TextColor,
            branding.HeadingFontFamily,
            branding.BodyFontFamily,
            branding.FooterText,
            now);
    }

    public static Result<DocumentDraft> CreateRevision(
        DocumentDraft current,
        GetBookingDto booking,
        GetTourDto tour,
        string templateId,
        string templateVersion,
        DocumentBrandingSnapshotValues branding,
        DateTime now)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(booking);
        ArgumentNullException.ThrowIfNull(tour);
        ArgumentNullException.ThrowIfNull(branding);

        var fieldsResult = CreateFields(booking, tour);
        if (fieldsResult.IsFailure)
        {
            return fieldsResult.ConvertError<IReadOnlyList<DocumentField>, DocumentDraft>();
        }

        return current.CreateRevision(
            templateId,
            templateVersion,
            CreateSourceVersion(booking, tour),
            fieldsResult.Value,
            branding.Version,
            branding.BrandName,
            branding.LogoUri,
            branding.PrimaryColor,
            branding.AccentColor,
            branding.BackgroundColor,
            branding.TextColor,
            branding.HeadingFontFamily,
            branding.BodyFontFamily,
            branding.FooterText,
            now);
    }

    private static Result<IReadOnlyList<DocumentField>> CreateFields(GetBookingDto booking, GetTourDto tour)
    {
        var definitions = new (string FieldId, string Label, string Value, DocumentPrivacyClassification Classification, bool IsEditable)[]
        {
            ("booking-reference", "Booking reference", booking.Id.ToString("N"), DocumentPrivacyClassification.Operational, false),
            ("customer-name", "Customer", booking.CustomerName, DocumentPrivacyClassification.PersonalData, false),
            ("companion-name", "Companion", booking.CompanionName ?? string.Empty, DocumentPrivacyClassification.PersonalData, false),
            ("tour-name", "Tour", tour.Name, DocumentPrivacyClassification.Public, false),
            ("tour-dates", "Travel dates", $"{tour.StartDate:yyyy-MM-dd} to {tour.EndDate:yyyy-MM-dd}", DocumentPrivacyClassification.Public, false),
            ("included-services", "Included services", string.Join(", ", tour.IncludedServices), DocumentPrivacyClassification.Public, false),
            ("total-price", "Total price", $"{booking.Currency} {booking.TotalPrice.ToString("0.00", CultureInfo.InvariantCulture)}", DocumentPrivacyClassification.PersonalData, false),
            ("payment-status", "Payment status", booking.PaymentStatus.ToString(), DocumentPrivacyClassification.Operational, false),
            ("greeting", "Greeting", $"Dear {booking.CustomerName},", DocumentPrivacyClassification.PersonalData, true),
            ("trip-note", "Trip note", string.Empty, DocumentPrivacyClassification.PersonalData, true),
            ("support-contact", "Support contact", "Contact Viajantes Turismo support for assistance.", DocumentPrivacyClassification.Public, true),
        };
        var fields = new List<DocumentField>(definitions.Length);
        foreach (var definition in definitions)
        {
            var fieldResult = DocumentField.Create(definition.FieldId, definition.Label, definition.Value, definition.Classification, definition.IsEditable);
            if (fieldResult.IsFailure)
            {
                return fieldResult.ConvertError<DocumentField, IReadOnlyList<DocumentField>>();
            }

            fields.Add(fieldResult.Value);
        }

        return Result.Ok<IReadOnlyList<DocumentField>>(fields);
    }

    private static string CreateSourceVersion(GetBookingDto booking, GetTourDto tour)
    {
        var source = string.Join("\n",
            booking.Id.ToString("N"),
            booking.CustomerId.ToString("N"),
            booking.CustomerName,
            booking.CompanionId?.ToString("N") ?? string.Empty,
            booking.CompanionName ?? string.Empty,
            booking.BookingDate.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            booking.Status,
            booking.PaymentStatus,
            booking.TotalPrice.ToString(CultureInfo.InvariantCulture),
            booking.Currency,
            tour.Id.ToString("N"),
            tour.Identifier,
            tour.Name,
            tour.StartDate.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            tour.EndDate.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            string.Join("\u001f", tour.IncludedServices));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(source));
        return Convert.ToHexString(hash);
    }
}
