namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>
/// Contains constant values used in the contract definitions.
/// </summary>
public static class ContractConstants
{
    /// <summary>The required HTTP header identifying an idempotent financial or document command.</summary>
    public const string IdempotencyKeyHeaderName = "Idempotency-Key";

    /// <summary>
    /// The maximum length for default string fields such as identifiers.
    /// </summary>
    public const int MaxDefaultLength = 64;

    /// <summary>
    /// The maximum length for names such as customer names and tour names.
    /// </summary>
    public const int MaxNameLength = 128;

    /// <summary>
    /// The maximum length for service names in the included services list.
    /// </summary>
    public const int MaxServiceDescriptionLength = 256;

    /// <summary>
    /// The maximum price value for any tour-related pricing.
    /// </summary>
    public const int MaxPrice = 100_000;

    /// <summary>
    /// The maximum length for booking notes.
    /// </summary>
    public const int MaxBookingNotesLength = 2000;

    /// <summary>
    /// The maximum length for an editable generated-document field value.
    /// </summary>
    public const int MaxDocumentFieldValueLength = 4000;

    /// <summary>Maximum length of an opaque disposition hold code: purpose, authority reference or release reason.</summary>
    public const int MaxDocumentHoldCodeLength = 128;

    /// <summary>Characters an opaque disposition hold code may use: ASCII letters, digits and <c>. _ : -</c>.</summary>
    public const string DocumentHoldCodePattern = "^[A-Za-z0-9._:-]+$";

    /// <summary>Hold basis: the establishment, exercise or defence of a legal claim.</summary>
    public const string DocumentHoldBasisLegalClaim = "LegalClaim";

    /// <summary>Hold basis: a legal or regulatory obligation to keep the data.</summary>
    public const string DocumentHoldBasisLegalObligation = "LegalObligation";

    /// <summary>Hold basis: a restriction of processing requested by, or owed to, a data subject.</summary>
    public const string DocumentHoldBasisDataSubjectRestriction = "DataSubjectRestriction";

    /// <summary>Hold basis: an inquiry by a regulator or other authority.</summary>
    public const string DocumentHoldBasisRegulatoryInquiry = "RegulatoryInquiry";

    /// <summary>Hold basis: an internal or external investigation.</summary>
    public const string DocumentHoldBasisInvestigation = "Investigation";

    /// <summary>
    /// The maximum length for medical information fields (allergies and additional info).
    /// </summary>
    public const int MaxMedicalInfoLength = 500;

    /// <summary>
    /// The minimum duration in days for a tour.
    /// </summary>
    public const int MinimumTourDurationDays = 5;

    /// <summary>
    /// The minimum weight value in kilograms for physical information (inclusive).
    /// </summary>
    public const int MinWeightKg = 1;

    /// <summary>
    /// The maximum weight value in kilograms for physical information (inclusive).
    /// </summary>
    public const int MaxWeightKg = 500;

    /// <summary>
    /// The minimum height value in centimeters for physical information (inclusive).
    /// </summary>
    public const int MinHeightCm = 50;

    /// <summary>
    /// The maximum height value in centimeters for physical information (inclusive).
    /// </summary>
    public const int MaxHeightCm = 300;

    /// <summary>
    /// The maximum percentage value for discount (100%).
    /// </summary>
    public const int MaxDiscountPercentage = 100;

    /// <summary>
    /// The maximum length for discount reason.
    /// </summary>
    public const int MaxDiscountReasonLength = 500;

    /// <summary>
    /// The minimum length for discount reason when discount is applied.
    /// </summary>
    public const int MinDiscountReasonLength = 10;

    /// <summary>
    /// The maximum length for payment reference numbers.
    /// </summary>
    public const int MaxReferenceNumberLength = 128;

    /// <summary>
    /// The maximum length for payment notes.
    /// </summary>
    public const int MaxPaymentNotesLength = 500;

    /// <summary>
    /// The minimum number of customers allowed for a tour.
    /// </summary>
    public const int MinTourCustomers = 1;

    /// <summary>
    /// The maximum number of customers allowed for a tour.
    /// </summary>
    public const int MaxTourCustomers = 20;

    /// <summary>
    /// Standard CSV media type accepted by customer import uploads.
    /// </summary>
    public const string CustomerImportTextCsvContentType = "text/csv";

    /// <summary>
    /// Alternate CSV media type accepted by customer import uploads.
    /// </summary>
    public const string CustomerImportApplicationCsvContentType = "application/csv";

    /// <summary>
    /// Spreadsheet media type that browsers commonly send for CSV uploads.
    /// </summary>
    public const string CustomerImportExcelCsvContentType = "application/vnd.ms-excel";

    /// <summary>
    /// Maximum accepted customer import file size in bytes.
    /// </summary>
    public const long CustomerImportMaxFileBytes = 1_048_576;

    /// <summary>
    /// Maximum budget for the serialized customer import conflict-resolution form field in bytes.
    /// </summary>
    public const long CustomerImportConflictResolutionsMaxBytes = 16_384;

    /// <summary>
    /// Additional multipart envelope budget for customer import requests in bytes.
    /// </summary>
    public const long CustomerImportMultipartEnvelopeBytes = 16_384;

    /// <summary>
    /// Maximum accepted customer import multipart request size in bytes.
    /// </summary>
    public const long CustomerImportMaxRequestBytes =
        CustomerImportMaxFileBytes
        + CustomerImportConflictResolutionsMaxBytes
        + CustomerImportMultipartEnvelopeBytes;

    /// <summary>
    /// Content types accepted by customer import uploads.
    /// </summary>
    public static IReadOnlyCollection<string> CustomerImportAllowedContentTypes
    {
        get;
    } = Array.AsReadOnly([
        CustomerImportTextCsvContentType,
        CustomerImportApplicationCsvContentType,
        CustomerImportExcelCsvContentType
    ]);
}
