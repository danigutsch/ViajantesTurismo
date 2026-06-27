using System.Text;
using ViajantesTurismo.Management.Web.Components.Pages.Customers;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers;

public sealed class ImportCustomersConflictLoaderTests
{
    [Fact]
    public async Task LoadConflictStates_when_existing_customer_is_found_populates_existing_and_incoming_values()
    {
        // Arrange
        const string conflictEmail = "existing@example.com";
        const string incomingFirstName = "IncomingFirst";
        const string existingLastName = "ExistingLast";

        var customersApi = new FakeCustomersApiClient();
        var customerId = Guid.NewGuid();
        customersApi.AddCustomer(new GetCustomerDto
        {
            Id = customerId,
            FirstName = "ExistingFirst",
            LastName = existingLastName,
            Email = conflictEmail,
            Mobile = "+551111111111",
            Nationality = "Brazilian",
            BikeType = BikeTypeDto.Regular,
        });
        customersApi.AddCustomerDetails(ImportCustomersConflictLoaderTestHelper.BuildCustomerDetails(customerId, conflictEmail, existingLastName));

        var mappedFileBytes = Encoding.UTF8.GetBytes(CustomerImportCsvTestData.AllCanonicalHeaders + "\n" + CustomerImportCsvTestData.BuildCsvRow(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [CustomerImportFieldNames.FirstName] = incomingFirstName,
            [CustomerImportFieldNames.LastName] = "IncomingLast",
            [CustomerImportFieldNames.Email] = conflictEmail,
        }));

        // Act
        var conflictStates = await ImportCustomersConflictLoader.LoadConflictStates(
            customersApi,
            [new ImportConflictDto(conflictEmail)],
            mappedFileBytes);

        // Assert
        var conflictState = Assert.Single(conflictStates);
        Assert.Equal(conflictEmail, conflictState.Email);
        Assert.Equal(incomingFirstName, conflictState.GetIncomingValue(CustomerImportFieldNames.FirstName));
        Assert.Equal(existingLastName, conflictState.GetExistingValue(CustomerImportFieldNames.LastName));
        Assert.Equal(conflictEmail, conflictState.GetExistingValue(CustomerImportFieldNames.Email));
    }

    [Fact]
    public async Task LoadConflictStates_when_existing_customer_details_are_missing_leaves_existing_values_empty()
    {
        // Arrange
        const string conflictEmail = "missing-details@example.com";

        var customersApi = new FakeCustomersApiClient();
        customersApi.AddCustomer(new GetCustomerDto
        {
            Id = Guid.NewGuid(),
            FirstName = "ExistingFirst",
            LastName = "ExistingLast",
            Email = conflictEmail,
            Mobile = "+551111111111",
            Nationality = "Brazilian",
            BikeType = BikeTypeDto.Regular,
        });

        var mappedFileBytes = Encoding.UTF8.GetBytes(CustomerImportCsvTestData.AllCanonicalHeaders + "\n" + CustomerImportCsvTestData.BuildCsvRow(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [CustomerImportFieldNames.FirstName] = "IncomingFirst",
            [CustomerImportFieldNames.Email] = conflictEmail,
        }));

        // Act
        var conflictStates = await ImportCustomersConflictLoader.LoadConflictStates(
            customersApi,
            [new ImportConflictDto(conflictEmail)],
            mappedFileBytes);

        // Assert
        var conflictState = Assert.Single(conflictStates);
        Assert.Equal("IncomingFirst", conflictState.GetIncomingValue(CustomerImportFieldNames.FirstName));
        Assert.Equal(string.Empty, conflictState.GetExistingValue(CustomerImportFieldNames.FirstName));
        Assert.Equal(string.Empty, conflictState.GetExistingValue(CustomerImportFieldNames.Email));
    }
}
