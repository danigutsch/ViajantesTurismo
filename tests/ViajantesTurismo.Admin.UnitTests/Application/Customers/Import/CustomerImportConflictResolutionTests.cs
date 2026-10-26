namespace ViajantesTurismo.Admin.UnitTests.Application.Customers.Import;

public sealed class CustomerImportConflictResolutionTests
{
    private const string Email = "existing@example.com";

    [Theory]
    [InlineData("overwrite")]
    [InlineData("OVERWRITE")]
    [InlineData("mixed")]
    public async Task Commit_overwrites_an_existing_customer_when_the_resolution_asks_for_it(string resolution)
    {
        // Arrange
        var scenario = new CustomerImportWorkflowScenario();
        await scenario.Service.Import(CustomerImportWorkflowScenario.Csv(CustomerImportWorkflowCsvRows.Build("Ana", "Silva", Email)), TestContext.Current.CancellationToken);
        var csv = CustomerImportWorkflowScenario.Csv(CustomerImportWorkflowCsvRows.Build("Beatriz", "Silva", Email));

        // Act
        var result = await scenario.Service.Commit(csv, new Dictionary<string, string> { [Email.ToUpperInvariant()] = resolution }, TestContext.Current.CancellationToken);

        // Assert
        result.SuccessCount.ShouldBe(1);
        result.ErrorCount.ShouldBe(0);
        var row = result.SuccessRows.ShouldNotBeNull().ShouldHaveSingleItem();
        row.Email.ShouldBe(Email);
        row.Outcome.ShouldBe("updated");
        var customer = scenario.Store.AllCustomers.ShouldHaveSingleItem();
        row.CustomerId.ShouldBe(customer.Id);
        customer.PersonalInfo.FirstName.ShouldBe("Beatriz");
    }

    [Fact]
    public async Task Commit_leaves_an_existing_customer_untouched_when_the_resolution_keeps_it()
    {
        // Arrange
        var scenario = new CustomerImportWorkflowScenario();
        await scenario.Service.Import(CustomerImportWorkflowScenario.Csv(CustomerImportWorkflowCsvRows.Build("Ana", "Silva", Email)), TestContext.Current.CancellationToken);
        var csv = CustomerImportWorkflowScenario.Csv(CustomerImportWorkflowCsvRows.Build("Beatriz", "Silva", Email));

        // Act
        var result = await scenario.Service.Commit(csv, new Dictionary<string, string> { [Email] = "keep" }, TestContext.Current.CancellationToken);

        // Assert
        result.SuccessCount.ShouldBe(0);
        result.ErrorCount.ShouldBe(0);
        result.SuccessRows.ShouldNotBeNull().ShouldBeEmpty();
        result.ErrorRows.ShouldNotBeNull().ShouldBeEmpty();
        scenario.Store.AllCustomers.ShouldHaveSingleItem().PersonalInfo.FirstName.ShouldBe("Ana");
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    public async Task Commit_reports_a_row_error_for_an_existing_email_without_a_usable_resolution(string? key)
    {
        // Arrange
        var scenario = new CustomerImportWorkflowScenario();
        await scenario.Service.Import(CustomerImportWorkflowScenario.Csv(CustomerImportWorkflowCsvRows.Build("Ana", "Silva", Email)), TestContext.Current.CancellationToken);
        var csv = CustomerImportWorkflowScenario.Csv(CustomerImportWorkflowCsvRows.Build("Beatriz", "Silva", Email));
        var resolutions = key is null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string> { [key] = "overwrite" };

        // Act
        var result = await scenario.Service.Commit(csv, resolutions, TestContext.Current.CancellationToken);

        // Assert
        result.SuccessCount.ShouldBe(0);
        result.ErrorCount.ShouldBe(1);
        var error = result.ErrorRows.ShouldNotBeNull().ShouldHaveSingleItem();
        error.LineNumber.ShouldBe(2);
        error.Field.ShouldBe("Email");
        error.Message.ShouldBe("Conflict requires resolution.");
        error.Email.ShouldBe(Email);
        scenario.Store.AllCustomers.ShouldHaveSingleItem().PersonalInfo.FirstName.ShouldBe("Ana");
    }

    [Fact]
    public async Task Commit_rejects_missing_conflict_resolutions()
    {
        // Arrange
        var scenario = new CustomerImportWorkflowScenario();
        Func<Task> commit = async () => await scenario.Service.Commit(CustomerImportWorkflowScenario.Csv(), null!, TestContext.Current.CancellationToken);

        // Act
        var exception = await commit.ShouldThrow<ArgumentNullException>();

        // Assert
        exception.ParamName.ShouldBe("conflictResolutions");
    }

    [Fact]
    public async Task Commit_reports_a_row_that_repeats_an_email_from_earlier_in_the_file()
    {
        // Arrange
        var scenario = new CustomerImportWorkflowScenario();
        var csv = CustomerImportWorkflowScenario.Csv(
            CustomerImportWorkflowCsvRows.Build("Ana", "Silva", Email),
            CustomerImportWorkflowCsvRows.Build("Bia", "Souza", "other@example.com"),
            CustomerImportWorkflowCsvRows.Build("Clara", "Silva", Email));

        // Act
        var result = await scenario.Service.Commit(csv, new Dictionary<string, string>(), TestContext.Current.CancellationToken);

        // Assert
        result.SuccessCount.ShouldBe(2);
        result.ErrorCount.ShouldBe(1);
        var error = result.ErrorRows.ShouldNotBeNull().ShouldHaveSingleItem();
        error.LineNumber.ShouldBe(4);
        error.Field.ShouldBe("Email");
        error.Message.ShouldBe("Duplicate email found in file.");
        error.Email.ShouldBe(Email);
        var rows = result.SuccessRows.ShouldNotBeNull();
        rows.Select(row => row.Email).ShouldBe([Email, "other@example.com"]);
        rows.Select(row => row.Outcome).ShouldBe(["created", "created"]);
        scenario.Store.AllCustomers.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Commit_reports_the_first_validation_message_of_an_invalid_row()
    {
        // Arrange
        var scenario = new CustomerImportWorkflowScenario();
        var csv = CustomerImportWorkflowScenario.Csv(CustomerImportWorkflowCsvRows.Build(" ", "Silva", Email));

        // Act
        var result = await scenario.Service.Commit(csv, new Dictionary<string, string>(), TestContext.Current.CancellationToken);

        // Assert
        result.SuccessCount.ShouldBe(0);
        result.ErrorCount.ShouldBe(1);
        var error = result.ErrorRows.ShouldNotBeNull().ShouldHaveSingleItem();
        error.LineNumber.ShouldBe(2);
        error.Field.ShouldBe("FirstName");
        string.IsNullOrWhiteSpace(error.Message).ShouldBeFalse();
        error.Email.ShouldBe(Email);
    }

    [Fact]
    public async Task Commit_reports_unreadable_csv_content_as_a_single_csv_error()
    {
        // Arrange
        var scenario = new CustomerImportWorkflowScenario();

        // Act
        var result = await scenario.Service.Commit(string.Empty, new Dictionary<string, string>(), TestContext.Current.CancellationToken);

        // Assert
        result.SuccessCount.ShouldBe(0);
        var error = result.ErrorRows.ShouldNotBeNull().ShouldHaveSingleItem();
        error.LineNumber.ShouldBe(1);
        error.Field.ShouldBe("Csv");
        string.IsNullOrWhiteSpace(error.Message).ShouldBeFalse();
        error.Message.ShouldNotBe("Invalid CSV content");
    }
}
