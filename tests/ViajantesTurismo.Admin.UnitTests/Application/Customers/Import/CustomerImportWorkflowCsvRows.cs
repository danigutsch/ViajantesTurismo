namespace ViajantesTurismo.Admin.UnitTests.Application.Customers.Import;

/// <summary>Builds customer import workflow CSV rows for tests.</summary>
internal static class CustomerImportWorkflowCsvRows
{
    /// <summary>Builds one valid CSV row with the given names and email.</summary>
    public static string Build(string firstName, string lastName, string email)
    {
        return $"{firstName},{lastName},Male,1990-01-01,Brazilian,Engineer,A12345678,BR," +
               $"{email},+5511999999999,Rua A,Centro,01000-000,São Paulo,SP,Brazil," +
               "75,175,Regular,DoubleOccupancy,SingleBed,Emergency Name,+5511888888888";
    }
}
