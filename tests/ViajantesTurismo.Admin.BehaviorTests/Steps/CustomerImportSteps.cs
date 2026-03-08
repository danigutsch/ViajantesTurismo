using Reqnroll;
using ViajantesTurismo.Admin.Application.Customers.Import;
using ViajantesTurismo.Admin.BehaviorTests.Context;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
public sealed class CustomerImportSteps(ImportContext context)
{
    [Given("I have a valid canonical CSV with {int} customer row")]
    [Given("I have a valid canonical CSV with {int} customer rows")]
    public void GivenIHaveAValidCanonicalCsvWithCustomerRows(int rowCount)
    {
        context.CsvContent = ImportContext.BuildValidCsv(rowCount);
        context.DryRun = false;
    }

    [Given("I have a canonical CSV without the Email column header")]
    public void GivenIHaveACsvWithoutEmailColumn()
    {
        context.CsvContent = ImportContext.BuildCsvWithoutEmailColumn();
        context.DryRun = false;
    }

    [Given("I have a canonical CSV with a blank Email value")]
    public void GivenIHaveACsvWithBlankEmailValue()
    {
        context.CsvContent = ImportContext.BuildCsvWithBlankEmail();
        context.DryRun = false;
    }

    [When("I run the import")]
    public async Task WhenIRunTheImport()
    {
        var handler = context.CreateHandler();
        context.Result = await handler.Handle(
            new CustomerImportCommand(context.CsvContent, context.DryRun),
            CancellationToken.None);
    }

    [When("I run the import in dry-run mode")]
    public async Task WhenIRunTheImportInDryRunMode()
    {
        context.DryRun = true;
        var handler = context.CreateHandler();
        context.Result = await handler.Handle(
            new CustomerImportCommand(context.CsvContent, DryRun: true),
            CancellationToken.None);
    }

    [Then("{int} customer should be imported successfully")]
    [Then("{int} customers should be imported successfully")]
    public void ThenCustomersShouldBeImportedSuccessfully(int expectedCount)
    {
        Assert.NotNull(context.Result);
        Assert.Equal(expectedCount, context.Result.Value.SuccessCount);
    }

    [Then("{int} rows should have errors")]
    [Then("{int} row should have errors")]
    public void ThenRowsShouldHaveErrors(int expectedCount)
    {
        Assert.NotNull(context.Result);
        Assert.Equal(expectedCount, context.Result.Value.ErrorCount);
    }

    [Then("{int} customer success should be reported")]
    [Then("{int} customers success should be reported")]
    public void ThenCustomerSuccessShouldBeReported(int expectedCount)
    {
        Assert.NotNull(context.Result);
        Assert.Equal(expectedCount, context.Result.Value.SuccessCount);
    }

    [Then("no customers should exist in the store")]
    public void ThenNoCustomersShouldExistInTheStore()
    {
        Assert.Empty(context.CustomerStore.AllCustomers);
    }
}
