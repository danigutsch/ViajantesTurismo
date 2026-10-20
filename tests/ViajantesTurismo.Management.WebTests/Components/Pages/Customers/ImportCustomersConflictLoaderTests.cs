using System.Text;
using SharedKernel.HttpClients;
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
            mappedFileBytes,
            Xunit.TestContext.Current.CancellationToken);

        // Assert
        var conflictState = (conflictStates).ShouldHaveSingleItem();
        (conflictState.Email).ShouldBe(conflictEmail);
        (conflictState.GetIncomingValue(CustomerImportFieldNames.FirstName)).ShouldBe(incomingFirstName);
        (conflictState.GetExistingValue(CustomerImportFieldNames.LastName)).ShouldBe(existingLastName);
        (conflictState.GetExistingValue(CustomerImportFieldNames.Email)).ShouldBe(conflictEmail);
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
            mappedFileBytes,
            Xunit.TestContext.Current.CancellationToken);

        // Assert
        var conflictState = (conflictStates).ShouldHaveSingleItem();
        (conflictState.GetIncomingValue(CustomerImportFieldNames.FirstName)).ShouldBe("IncomingFirst");
        (conflictState.GetExistingValue(CustomerImportFieldNames.FirstName)).ShouldBe(string.Empty);
        (conflictState.GetExistingValue(CustomerImportFieldNames.Email)).ShouldBe(string.Empty);
    }

    [Fact]
    public async Task LoadConflictStates_when_customer_list_load_fails_does_not_build_incomplete_conflict_state()
    {
        // Arrange
        const string conflictEmail = "existing@example.com";
        var customersApi = new FakeCustomersApiClient();
        customersApi.SetGetCustomersOutcome(ContractCommandOutcome.Status<GetCustomerDto[]>(
            ContractCommandOutcomeKind.Unauthorized,
            System.Net.HttpStatusCode.Unauthorized));
        var mappedFileBytes = Encoding.UTF8.GetBytes(
            CustomerImportCsvTestData.AllCanonicalHeaders
            + "\n"
            + CustomerImportCsvTestData.BuildCsvRow(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [CustomerImportFieldNames.FirstName] = "IncomingFirst",
                [CustomerImportFieldNames.Email] = conflictEmail,
            }));

        // Act
        Func<Task> action = () => ImportCustomersConflictLoader.LoadConflictStates(
            customersApi,
            [new ImportConflictDto(conflictEmail)],
            mappedFileBytes,
            Xunit.TestContext.Current.CancellationToken);

        // Assert
        await action.ShouldThrow<InvalidOperationException>();
    }
}
