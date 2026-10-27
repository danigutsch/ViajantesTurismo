using ViajantesTurismo.Admin.Application.Customers.Import;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.UnitTests.Application.Customers.Import;

/// <summary>Wires an import workflow service over a fake customer store for tests.</summary>
internal sealed class CustomerImportWorkflowScenario
{
    /// <summary>The CSV header row of the customer import format.</summary>
    public const string CsvHeaders =
        "FirstName,LastName,Gender,BirthDate,Nationality,Occupation,NationalId,IdNationality," +
        "Email,Mobile,Street,Neighborhood,PostalCode,City,State,Country," +
        "WeightKg,HeightCentimeters,BikeType,RoomType,BedType,EmergencyContactName,EmergencyContactMobile";

    /// <summary>Creates the fake store and the import workflow service that uses it.</summary>
    public CustomerImportWorkflowScenario()
    {
        Store = new FakeCustomerStore();
        var handler = new CustomerImportCommandHandler(Store, new FakeUnitOfWork(), TimeProvider.System);
        Service = new CustomerImportWorkflowService(Store, new CustomerImportConflictDetector(Store), handler, TimeProvider.System);
    }

    /// <summary>Gets the fake customer store used by the workflow.</summary>
    public FakeCustomerStore Store
    {
        get;
    }

    /// <summary>Gets the import workflow service under test.</summary>
    public CustomerImportWorkflowService Service
    {
        get;
    }

    /// <summary>Builds CSV content from the header row followed by the given rows.</summary>
    public static string Csv(params string[] rows) => CsvHeaders + "\n" + string.Join('\n', rows);
}
