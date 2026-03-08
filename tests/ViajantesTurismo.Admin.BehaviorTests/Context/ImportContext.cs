using JetBrains.Annotations;
using ViajantesTurismo.Admin.Application.Customers.Import;
using ViajantesTurismo.Admin.Application.Import;
using ViajantesTurismo.Admin.Contracts;
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
}
