using ViajantesTurismo.Management.Web.Components.Pages.Customers.Import;
using SharedKernel.Testing;
using System.Text;
using SharedKernel.HttpClients;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers.Import;

[TestScope(TestScope.Unit)]
public sealed class ImportCustomersConflictLoaderTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public async Task LoadConflictStates_when_existing_customer_is_found_populates_existing_and_incoming_values(int precedingCustomers)
    {
        // Arrange
        const string conflictEmail = "existing@example.com";
        const string incomingFirstName = "IncomingFirst";
        const string existingLastName = "ExistingLast";

        var customersApi = new FakeCustomersApiClient();
        var customerId = Guid.NewGuid();
        var existingCustomer = new GetCustomerDto
        {
            Id = customerId,
            FirstName = "ExistingFirst",
            LastName = existingLastName,
            Email = conflictEmail,
            Mobile = "+551111111111",
            Nationality = "Brazilian",
            BikeType = BikeTypeDto.Regular,
        };
        for (var index = 0; index < precedingCustomers; index++)
        {
            customersApi.AddCustomer(existingCustomer with
            {
                Id = Guid.NewGuid(),
                Email = $"unrelated-{index}@example.com",
            });
        }

        customersApi.AddCustomer(existingCustomer);
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
        var conflictState = conflictStates.ShouldHaveSingleItem();
        conflictState.Email.ShouldBe(conflictEmail);
        conflictState.GetIncomingValue(CustomerImportFieldNames.FirstName).ShouldBe(incomingFirstName);
        conflictState.GetExistingValue(CustomerImportFieldNames.LastName).ShouldBe(existingLastName);
        conflictState.GetExistingValue(CustomerImportFieldNames.Email).ShouldBe(conflictEmail);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LoadConflictStates_when_existing_customer_details_are_unavailable_leaves_existing_values_empty(bool customerExists)
    {
        // Arrange
        const string conflictEmail = "missing-details@example.com";

        var customersApi = new FakeCustomersApiClient();
        if (customerExists)
        {
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
        }

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
        var conflictState = conflictStates.ShouldHaveSingleItem();
        conflictState.GetIncomingValue(CustomerImportFieldNames.FirstName).ShouldBe("IncomingFirst");
        conflictState.GetExistingValue(CustomerImportFieldNames.FirstName).ShouldBe(string.Empty);
        conflictState.GetExistingValue(CustomerImportFieldNames.Email).ShouldBe(string.Empty);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LoadConflictStates_when_conflict_lookup_fails_does_not_build_incomplete_conflict_state(bool incompleteResponse)
    {
        // Arrange
        const string conflictEmail = "existing@example.com";
        var customersApi = new FakeCustomersApiClient();
        var outcome = incompleteResponse
            ? HttpOutcomeMapper.Succeeded(System.Net.HttpStatusCode.OK, Array.Empty<Guid?>())
            : HttpOutcomeMapper.Status<Guid?[]>(HttpOutcomeKind.Unauthorized, System.Net.HttpStatusCode.Unauthorized);
        customersApi.SetFindImportConflictIdsOutcome(outcome);
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
