using JetBrains.Annotations;
using ViajantesTurismo.Admin.Application.Customers.Import;
using ViajantesTurismo.Admin.Application.Import;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Tests.Shared.Fakes;

namespace ViajantesTurismo.Admin.BehaviorTests.Context;

[UsedImplicitly]
public sealed class ImportContext
{
    private const string RequiredHeaders =
        "FirstName,LastName,Gender,BirthDate,Nationality,Occupation," +
        "NationalId,IdNationality,Email,Mobile,Street,Neighborhood," +
        "PostalCode,City,State,Country,WeightKg,HeightCentimeters," +
        "BikeType,RoomType,BedType,EmergencyContactName,EmergencyContactMobile";

    private const string ValidRow =
        "John,Doe,Male,1990-01-01,Brazilian,Engineer,A12345,BR," +
        "john.import@example.com,+1234567890,123 Main St,Downtown," +
        "10001,New York,NY,USA,75,175,Regular,DoubleOccupancy,SingleBed," +
        "Jane Doe,+0987654321";

    public string CsvContent { get; set; } = BuildValidCsv(1);

    public bool DryRun { get; set; }

    public ImportResult? Result { get; set; }

    public ImportResultDto? WorkflowResult { get; set; }

    public ImportResultDto? WorkflowCommitResult { get; set; }

    public IReadOnlyDictionary<string, string> ConflictResolutions { get; set; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public FakeCustomerStore CustomerStore { get; } = new();

    public FakeUnitOfWork UnitOfWork { get; } = new();

    public CustomerImportCommandHandler CreateHandler() =>
        new(CustomerStore, UnitOfWork, TimeProvider.System);

    public CustomerImportWorkflowService CreateWorkflowService() =>
        new(CustomerStore, CreateHandler());

    public static string BuildValidCsv(int rowCount)
    {
        var rows = Enumerable.Range(1, rowCount)
            .Select(i => ValidRow.Replace("john.import@example.com", $"john.import.{i}@example.com", StringComparison.Ordinal));
        return RequiredHeaders + "\n" + string.Join("\n", rows);
    }

    public static string BuildCsvWithBlankEmail() =>
        RequiredHeaders + "\n" +
        ValidRow.Replace("john.import@example.com", "", StringComparison.Ordinal);

    public static string BuildCsvWithEmail(string email) =>
        RequiredHeaders + "\n" +
        ValidRow.Replace("john.import@example.com", email, StringComparison.Ordinal);

    public static string BuildCsvWithCustomerRows(IReadOnlyList<(string FirstName, string Email)> rows)
    {
        var csvRows = rows.Select(r =>
            ValidRow
                .Replace("John", r.FirstName, StringComparison.Ordinal)
                .Replace("john.import@example.com", r.Email, StringComparison.Ordinal));

        return RequiredHeaders + "\n" + string.Join("\n", csvRows);
    }

    public void SeedExistingCustomerRecord(string email, string firstName)
    {
        var customer = CreateCustomer(firstName, email);
        CustomerStore.Seed(customer);
    }

    public Customer? GetCustomerByEmail(string email) =>
        CustomerStore.AllCustomers.FirstOrDefault(c =>
            c.ContactInfo.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

    public void ReplaceBlankEmailsWithGeneratedValidEmails()
    {
        var lines = CsvContent.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).ToList();
        if (lines.Count < 2)
        {
            return;
        }

        var headers = lines[0].Split(',').Select(h => h.Trim()).ToArray();
        var emailIndex = Array.FindIndex(headers, h => h.Equals("Email", StringComparison.OrdinalIgnoreCase));
        if (emailIndex < 0)
        {
            return;
        }

        for (var i = 1; i < lines.Count; i++)
        {
            var values = lines[i].Split(',');
            if (emailIndex >= values.Length)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(values[emailIndex]))
            {
                values[emailIndex] = $"retry.import.{i}@example.com";
                lines[i] = string.Join(",", values);
            }
        }

        CsvContent = string.Join("\n", lines);
    }

    public static string BuildCsvWithoutEmailColumn()
    {
        const string headersWithoutEmail =
            "FirstName,LastName,Gender,BirthDate,Nationality,Occupation," +
            "NationalId,IdNationality,Mobile,Street,Neighborhood," +
            "PostalCode,City,State,Country,WeightKg,HeightCentimeters," +
            "BikeType,RoomType,BedType,EmergencyContactName,EmergencyContactMobile";

        const string rowWithoutEmail =
            "John,Doe,Male,1990-01-01,Brazilian,Engineer,A12345,BR," +
            "+1234567890,123 Main St,Downtown," +
            "10001,New York,NY,USA,75,175,Regular,DoubleOccupancy,SingleBed," +
            "Jane Doe,+0987654321";

        return headersWithoutEmail + "\n" + rowWithoutEmail;
    }

    private static Customer CreateCustomer(string firstName, string email)
    {
        var personalInfo = PersonalInfo.Create(
            firstName,
            "Doe",
            "Male",
            DateTime.UtcNow.AddYears(-30),
            "Brazilian",
            "Engineer",
            TimeProvider.System).Value;

        var identificationInfo = IdentificationInfo.Create(
            nationalId: "A12345678",
            idNationality: "BR").Value;

        var contactInfo = ContactInfo.Create(
            email,
            "+1234567890",
            null,
            null).Value;

        var address = Address.Create(
            "123 Main St",
            null,
            "Downtown",
            "10001",
            "New York",
            "NY",
            "USA").Value;

        var physicalInfo = PhysicalInfo.Create(
            75,
            175,
            BikeType.Regular).Value;

        var accommodationPreferences = AccommodationPreferences.Create(
            RoomType.DoubleOccupancy,
            BedType.SingleBed,
            null).Value;

        var emergencyContact = EmergencyContact.Create(
            "Emergency Contact",
            "+0987654321").Value;

        var medicalInfo = MedicalInfo.Create("None", "None").Value;

        return new Customer(
            personalInfo,
            identificationInfo,
            contactInfo,
            address,
            physicalInfo,
            accommodationPreferences,
            emergencyContact,
            medicalInfo);
    }
}
