using System.Text;

namespace ViajantesTurismo.Admin.Web.Services;

public sealed record CustomerImportField(string Name, string DisplayName, bool IsRequired);

public sealed record CustomerImportFieldMapping(CustomerImportField Field, string? MatchedCsvHeader)
{
    public bool IsAutoMatched => MatchedCsvHeader is not null;
}

public static class CustomerImportHeaderMatcher
{
    public static IReadOnlyList<CustomerImportField> Fields { get; } =
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

    public static IReadOnlyList<CustomerImportFieldMapping> AutoMatch(IReadOnlyList<string> csvHeaders)
    {
        return Fields
            .Select(field =>
            {
                var matched = csvHeaders.FirstOrDefault(h =>
                    h.Equals(field.Name, StringComparison.OrdinalIgnoreCase));
                return new CustomerImportFieldMapping(field, matched);
            })
            .ToList()
            .AsReadOnly();
    }

    public static byte[] ApplyMapping(
        byte[] fileBytes,
        IReadOnlyList<CustomerImportFieldMapping> autoMappings,
        IReadOnlyDictionary<string, string?> userMappings
    )
    {
        ArgumentNullException.ThrowIfNull(userMappings);
        var text = Encoding.UTF8.GetString(fileBytes);
        var newlineIndex = text.IndexOf('\n', StringComparison.Ordinal);
        var firstLine = newlineIndex >= 0
            ? text[..newlineIndex].TrimEnd('\r')
            : text;
        var rest = newlineIndex >= 0 ? text[(newlineIndex + 1)..] : "";

        var originalHeaders = firstLine.Split(',')
            .Select(h => h.Trim().Trim('"'))
            .ToArray();

        var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var mapping in autoMappings.Where(m => m.IsAutoMatched))
        {
            lookup[mapping.MatchedCsvHeader!] = mapping.Field.Name;
        }

        foreach (var (fieldName, csvHeader) in userMappings)
        {
            if (csvHeader is not null)
            {
                lookup[csvHeader] = fieldName;
            }
        }

        var newHeaders = originalHeaders.Select(h => lookup.GetValueOrDefault(h, h));

        return Encoding.UTF8.GetBytes(string.Join(",", newHeaders) + "\n" + rest);
    }
}
