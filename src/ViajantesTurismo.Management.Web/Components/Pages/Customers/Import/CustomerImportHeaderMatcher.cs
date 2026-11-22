using PandaBear.Tabular;

namespace ViajantesTurismo.Management.Web.Components.Pages.Customers.Import;

/// <summary>Matches CSV headers to customer import fields and rewrites the header row to canonical field names.</summary>
internal static class CustomerImportHeaderMatcher
{
    /// <summary>All customer import fields with their display names and whether each is required.</summary>
    public static IReadOnlyList<CustomerImportField> Fields
    {
        get;
    } =
    [
        new(CustomerImportFieldNames.FirstName, "First Name", true),
        new(CustomerImportFieldNames.LastName, "Last Name", true),
        new(CustomerImportFieldNames.Gender, "Gender", true),
        new(CustomerImportFieldNames.BirthDate, "Birth Date", true),
        new(CustomerImportFieldNames.Nationality, "Nationality", true),
        new(CustomerImportFieldNames.Occupation, "Occupation", true),
        new(CustomerImportFieldNames.NationalId, "National ID", true),
        new(CustomerImportFieldNames.IdNationality, "ID Nationality", true),
        new(CustomerImportFieldNames.Email, "Email", true),
        new(CustomerImportFieldNames.Mobile, "Mobile", true),
        new(CustomerImportFieldNames.Instagram, "Instagram", false),
        new(CustomerImportFieldNames.Facebook, "Facebook", false),
        new(CustomerImportFieldNames.Street, "Street", true),
        new(CustomerImportFieldNames.Complement, "Complement", false),
        new(CustomerImportFieldNames.Neighborhood, "Neighborhood", false),
        new(CustomerImportFieldNames.PostalCode, "Postal Code", true),
        new(CustomerImportFieldNames.City, "City", true),
        new(CustomerImportFieldNames.State, "State", true),
        new(CustomerImportFieldNames.Country, "Country", true),
        new(CustomerImportFieldNames.WeightKg, "Weight (kg)", true),
        new(CustomerImportFieldNames.HeightCentimeters, "Height (cm)", true),
        new(CustomerImportFieldNames.BikeType, "Bike Type", true),
        new(CustomerImportFieldNames.RoomType, "Room Type", true),
        new(CustomerImportFieldNames.BedType, "Bed Type", true),
        new(CustomerImportFieldNames.CompanionId, "Companion ID", false),
        new(CustomerImportFieldNames.EmergencyContactName, "Emergency Contact Name", true),
        new(CustomerImportFieldNames.EmergencyContactMobile, "Emergency Contact Mobile", true),
        new(CustomerImportFieldNames.Allergies, "Allergies", false),
        new(CustomerImportFieldNames.AdditionalInfo, "Additional Info", false),
    ];

    private static readonly string[] CanonicalHeaders = [.. Fields.Select(field => field.Name)];
    private static readonly HashSet<string> FieldNames = CanonicalHeaders.ToHashSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>Matches each import field to a CSV header by case-insensitive name, leaving unmatched fields without a header.</summary>
    public static IReadOnlyList<CustomerImportFieldMapping> AutoMatch(IReadOnlyList<string> csvHeaders)
    {
        var matches = CsvHeaderMapping.Match(csvHeaders, CanonicalHeaders);
        return matches.Select((match, index) => new CustomerImportFieldMapping(Fields[index], match.SourceHeader)).ToList().AsReadOnly();
    }

    /// <summary>Rewrites the CSV header row using automatic and user mappings, with user mappings taking precedence.</summary>
    public static ReadOnlyMemory<byte> ApplyMapping(
        ReadOnlyMemory<byte> fileBytes,
        IReadOnlyList<CustomerImportFieldMapping> autoMappings,
        IReadOnlyDictionary<string, string?> userMappings
    )
    {
        ArgumentNullException.ThrowIfNull(userMappings);
        if (!ImportCustomersCsvProcessor.TryParse(fileBytes, out var document))
        {
            return fileBytes;
        }

        var headers = GetMappedHeaders(document.Headers, autoMappings, userMappings);
        return headers is null
            ? fileBytes
            : ImportCustomersCsvProcessor.WriteImportFile(headers, document.Rows);
    }

    /// <summary>Resolves the effective header names once, returning null when no rewrite is necessary.</summary>
    internal static IReadOnlyList<string>? GetMappedHeaders(
        IReadOnlyList<string> headers,
        IReadOnlyList<CustomerImportFieldMapping> autoMappings,
        IReadOnlyDictionary<string, string?> userMappings)
    {
        var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var mapping in autoMappings)
        {
            var fieldName = mapping.Field.Name;
            var csvHeader = userMappings.GetValueOrDefault(fieldName, mapping.MatchedCsvHeader);
            if (csvHeader is not null)
            {
                lookup[csvHeader] = fieldName;
            }
        }

        string[] newHeaders = [.. headers.Select(header => lookup.TryGetValue(header, out var field)
            ? field
            : FieldNames.Contains(header) ? $"{header} (unmapped)" : header)];

        return newHeaders.SequenceEqual(headers, StringComparer.Ordinal) ? null : newHeaders;
    }
}
