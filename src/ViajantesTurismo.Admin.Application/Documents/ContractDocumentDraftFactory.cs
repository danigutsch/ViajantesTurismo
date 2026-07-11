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

        var fields = CreateFields(booking, tour);
        return DocumentDraft.Create(
            booking.Id,
            DocumentType.BookingConfirmationContract,
            DocumentAudience.Customer,
            templateId,
            templateVersion,
            CreateSourceVersion(booking, tour),
            fields,
            branding.Version,
            branding.BrandName,
            branding.LogoUri,
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

        return current.CreateRevision(
            templateId,
            templateVersion,
            CreateSourceVersion(booking, tour),
            CreateFields(booking, tour),
            branding.Version,
            branding.BrandName,
            branding.LogoUri,
            now);
    }

    private static IReadOnlyList<DocumentField> CreateFields(GetBookingDto booking, GetTourDto tour) =>
    [
        CreateField("booking-reference", "Booking reference", booking.Id.ToString("N"), DocumentPrivacyClassification.Operational, false),
        CreateField("customer-name", "Customer", booking.CustomerName, DocumentPrivacyClassification.PersonalData, false),
        CreateField("companion-name", "Companion", booking.CompanionName ?? string.Empty, DocumentPrivacyClassification.PersonalData, false),
        CreateField("tour-name", "Tour", tour.Name, DocumentPrivacyClassification.Public, false),
        CreateField("tour-dates", "Travel dates", $"{tour.StartDate:yyyy-MM-dd} to {tour.EndDate:yyyy-MM-dd}", DocumentPrivacyClassification.Public, false),
        CreateField("included-services", "Included services", string.Join(", ", tour.IncludedServices), DocumentPrivacyClassification.Public, false),
        CreateField("total-price", "Total price", $"{booking.Currency} {booking.TotalPrice.ToString("0.00", CultureInfo.InvariantCulture)}", DocumentPrivacyClassification.Operational, false),
        CreateField("payment-status", "Payment status", booking.PaymentStatus.ToString(), DocumentPrivacyClassification.Operational, false),
        CreateField("greeting", "Greeting", $"Dear {booking.CustomerName},", DocumentPrivacyClassification.PersonalData, true),
        CreateField("trip-note", "Trip note", string.Empty, DocumentPrivacyClassification.PersonalData, true),
        CreateField("support-contact", "Support contact", "Contact Viajantes Turismo support for assistance.", DocumentPrivacyClassification.Public, true)
    ];

    private static DocumentField CreateField(
        string fieldId,
        string label,
        string value,
        DocumentPrivacyClassification classification,
        bool isEditable) => DocumentField.Create(fieldId, label, value, classification, isEditable).Value;

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
