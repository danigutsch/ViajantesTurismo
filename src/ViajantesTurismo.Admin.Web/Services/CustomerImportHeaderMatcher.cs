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
        new("FirstName", "First Name", true),
        new("LastName", "Last Name", true),
        new("Gender", "Gender", true),
        new("BirthDate", "Birth Date", true),
        new("Nationality", "Nationality", true),
        new("Occupation", "Occupation", true),
        new("NationalId", "National ID", true),
        new("IdNationality", "ID Nationality", true),
        new("Email", "Email", true),
        new("Mobile", "Mobile", true),
        new("Instagram", "Instagram", false),
        new("Facebook", "Facebook", false),
        new("Street", "Street", true),
        new("Complement", "Complement", false),
        new("Neighborhood", "Neighborhood", false),
        new("PostalCode", "Postal Code", true),
        new("City", "City", true),
        new("State", "State", true),
        new("Country", "Country", true),
        new("WeightKg", "Weight (kg)", true),
        new("HeightCentimeters", "Height (cm)", true),
        new("BikeType", "Bike Type", true),
        new("RoomType", "Room Type", true),
        new("BedType", "Bed Type", true),
        new("CompanionId", "Companion ID", false),
        new("EmergencyContactName", "Emergency Contact Name", true),
        new("EmergencyContactMobile", "Emergency Contact Mobile", true),
        new("Allergies", "Allergies", false),
        new("AdditionalInfo", "Additional Info", false),
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
