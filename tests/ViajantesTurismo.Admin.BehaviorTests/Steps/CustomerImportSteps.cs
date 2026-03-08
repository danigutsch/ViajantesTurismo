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

    [Given("an existing customer with email {string}")]
    public void GivenAnExistingCustomerWithEmail(string email)
    {
        context.CustomerStore.SeedEmail(email);
    }

    [Given("an existing customer record with email {string} and first name {string}")]
    public void GivenAnExistingCustomerRecordWithEmailAndFirstName(string email, string firstName)
    {
        context.SeedExistingCustomerRecord(email, firstName);
    }

    [Given("I have a canonical CSV with duplicate email {string}")]
    public void GivenIHaveACanonicalCsvWithDuplicateEmail(string email)
    {
        context.CsvContent = ImportContext.BuildCsvWithEmail(email);
        context.DryRun = false;
    }

    [Given("I have a canonical CSV with the following customer rows")]
    public void GivenIHaveACanonicalCsvWithTheFollowingCustomerRows(Table table)
    {
        var rows = table.Rows
            .Select(r => (FirstName: r["FirstName"], Email: r["Email"]))
            .ToList();

        context.CsvContent = ImportContext.BuildCsvWithCustomerRows(rows);
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

    [When("I run the import workflow pre-check")]
    public async Task WhenIRunTheImportWorkflowPreCheck()
    {
        var workflow = context.CreateWorkflowService();
        context.WorkflowResult = await workflow.Import(context.CsvContent, CancellationToken.None);
    }

    [When("I commit the import workflow with resolutions")]
    public async Task WhenICommitTheImportWorkflowWithResolutions(Table table)
    {
        var resolutions = table.Rows
            .ToDictionary(
                r => r["Email"],
                r => r["Decision"],
                StringComparer.OrdinalIgnoreCase);

        context.ConflictResolutions = resolutions;
        var workflow = context.CreateWorkflowService();
        context.WorkflowCommitResult = await workflow.Commit(context.CsvContent, resolutions, CancellationToken.None);
    }

    [When("I replace blank emails with generated valid emails and rerun the import")]
    public async Task WhenIReplaceBlankEmailsWithGeneratedValidEmailsAndRerunTheImport()
    {
        context.ReplaceBlankEmailsWithGeneratedValidEmails();
        var handler = context.CreateHandler();
        context.Result = await handler.Handle(
            new CustomerImportCommand(context.CsvContent, context.DryRun),
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

    [Then("{int} duplicate conflict should be surfaced")]
    [Then("{int} duplicate conflicts should be surfaced")]
    public void ThenDuplicateConflictsShouldBeSurfaced(int expectedCount)
    {
        Assert.NotNull(context.WorkflowResult);
        Assert.NotNull(context.WorkflowResult.Conflicts);
        Assert.Equal(expectedCount, context.WorkflowResult.Conflicts.Count);
    }

    [Then("the duplicate conflict should contain email {string}")]
    public void ThenTheDuplicateConflictShouldContainEmail(string expectedEmail)
    {
        Assert.NotNull(context.WorkflowResult);
        Assert.NotNull(context.WorkflowResult.Conflicts);
        var conflict = Assert.Single(context.WorkflowResult.Conflicts);
        Assert.Equal(expectedEmail, conflict.Email, ignoreCase: true);
    }

    [Then("import summary should report {int} created, {int} updated, {int} skipped, and {int} failed")]
    public void ThenImportSummaryShouldReportCreatedUpdatedSkippedAndFailed(
        int expectedCreated,
        int expectedUpdated,
        int expectedSkipped,
        int expectedFailed)
    {
        Assert.NotNull(context.WorkflowCommitResult);

        var updatedCount = context.ConflictResolutions.Values.Count(v =>
            v.Equals("overwrite", StringComparison.OrdinalIgnoreCase)
            || v.Equals("mixed", StringComparison.OrdinalIgnoreCase));
        var skippedCount = context.ConflictResolutions.Values.Count(v =>
            v.Equals("keep", StringComparison.OrdinalIgnoreCase));
        var createdCount = Math.Max(0, context.WorkflowCommitResult.SuccessCount - updatedCount);
        var failedCount = context.WorkflowCommitResult.ErrorCount;

        Assert.Equal(expectedCreated, createdCount);
        Assert.Equal(expectedUpdated, updatedCount);
        Assert.Equal(expectedSkipped, skippedCount);
        Assert.Equal(expectedFailed, failedCount);
    }

    [Then("customer with email {string} should have first name {string}")]
    public void ThenCustomerWithEmailShouldHaveFirstName(string email, string expectedFirstName)
    {
        var customer = context.GetCustomerByEmail(email);
        Assert.NotNull(customer);
        Assert.Equal(expectedFirstName, customer.PersonalInfo.FirstName);
    }

    [Then("customer with email {string} should exist in the store")]
    public void ThenCustomerWithEmailShouldExistInTheStore(string email)
    {
        var customer = context.GetCustomerByEmail(email);
        Assert.NotNull(customer);
    }

    [Then("imported customer with email {string} should have a stable identifier")]
    public void ThenImportedCustomerWithEmailShouldHaveAStableIdentifier(string email)
    {
        var customer = context.GetCustomerByEmail(email);
        Assert.NotNull(customer);
        Assert.NotEqual(Guid.Empty, customer.Id);
    }
}
