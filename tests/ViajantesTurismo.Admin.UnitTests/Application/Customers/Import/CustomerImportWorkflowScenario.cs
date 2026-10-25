using ViajantesTurismo.Admin.Application.Customers.Import;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.UnitTests.Application.Customers.Import;

internal sealed class CustomerImportWorkflowScenario
{
    public const string CsvHeaders =
        "FirstName,LastName,Gender,BirthDate,Nationality,Occupation,NationalId,IdNationality," +
        "Email,Mobile,Street,Neighborhood,PostalCode,City,State,Country," +
        "WeightKg,HeightCentimeters,BikeType,RoomType,BedType,EmergencyContactName,EmergencyContactMobile";

    public CustomerImportWorkflowScenario()
    {
        Store = new FakeCustomerStore();
        var handler = new CustomerImportCommandHandler(Store, new FakeUnitOfWork(), TimeProvider.System);
        Service = new CustomerImportWorkflowService(Store, new CustomerImportConflictDetector(Store), handler, TimeProvider.System);
    }

    public FakeCustomerStore Store
    {
        get;
    }

    public CustomerImportWorkflowService Service
    {
        get;
    }

    public static string Csv(params string[] rows) => CsvHeaders + "\n" + string.Join('\n', rows);
}
