using ViajantesTurismo.Management.Web.Components.Pages.Customers;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers;

public sealed class ImportCustomersWorkflowStateTests
{
    [Fact]
    public void SetPendingFile_stores_file_data_and_resets_transient_state()
    {
        // Arrange
        var state = new ImportCustomersWorkflowState
        {
            ValidationError = "Previous validation error",
            Error = "Previous error",
            Result = new ImportResultDto(1, 0),
            PreviewRows = [new Dictionary<string, string> { [CustomerImportFieldNames.Email] = "preview@example.com" }],
            ConflictStates = [new ImportCustomerConflictState("existing@example.com", null, null)],
            Step = ImportCustomersWorkflowStep.Preview,
        };
        state.UserMappings[CustomerImportFieldNames.FirstName] = "First Name";

        var csvHeaders = new[] { CustomerImportFieldNames.FirstName, CustomerImportFieldNames.Email };
        var fieldMappings = CustomerImportHeaderMatcher.AutoMatch(csvHeaders);

        // Act
        state.SetPendingFile("customers.csv", [1, 2, 3], csvHeaders, fieldMappings);

        // Assert
        (state.PendingFileName).ShouldBe("customers.csv");
        (state.PendingFileBytes).ShouldBe([1, 2, 3]);
        (state.CsvHeaders).ShouldBe(csvHeaders);
        (state.FieldMappings).ShouldBe(fieldMappings);
        (state.UserMappings).ShouldBeEmpty();
        (state.PreviewRows).ShouldBeEmpty();
        (state.ConflictStates).ShouldBeEmpty();
        (state.Result).ShouldBeNull();
        (state.Error).ShouldBeNull();
        (state.ValidationError).ShouldBeNull();
        (state.Step).ShouldBe(ImportCustomersWorkflowStep.HeaderMapping);
    }

    [Fact]
    public void ResetToFileSelection_clears_all_state_and_returns_to_fileselection()
    {
        // Arrange
        var state = new ImportCustomersWorkflowState
        {
            Dragging = 1,
            ValidationError = "Validation",
            Error = "Error",
            Result = new ImportResultDto(1, 0),
            Uploading = true,
            PreviewRows = [new Dictionary<string, string> { [CustomerImportFieldNames.Email] = "preview@example.com" }],
            ConflictStates = [new ImportCustomerConflictState("existing@example.com", null, null)],
            Step = ImportCustomersWorkflowStep.DuplicateResolution,
        };
        state.SetPendingFile(
            "customers.csv",
            [1, 2, 3],
            [CustomerImportFieldNames.Email],
            CustomerImportHeaderMatcher.AutoMatch([CustomerImportFieldNames.Email]));
        state.UserMappings[CustomerImportFieldNames.Email] = CustomerImportFieldNames.Email;

        // Act
        state.ResetToFileSelection();

        // Assert
        (state.PendingFileName).ShouldBe(string.Empty);
        (state.PendingFileBytes).ShouldBeEmpty();
        (state.CsvHeaders).ShouldBeEmpty();
        (state.FieldMappings).ShouldBeEmpty();
        (state.UserMappings).ShouldBeEmpty();
        (state.PreviewRows).ShouldBeEmpty();
        (state.ConflictStates).ShouldBeEmpty();
        (state.Result).ShouldBeNull();
        (state.Error).ShouldBeNull();
        (state.ValidationError).ShouldBeNull();
        (state.Uploading).ShouldBeFalse();
        (state.Step).ShouldBe(ImportCustomersWorkflowStep.FileSelection);
    }

    [Fact]
    public void RetryCurrentFile_when_pending_file_exists_returns_to_headermapping_and_clears_transient_state()
    {
        // Arrange
        var state = new ImportCustomersWorkflowState
        {
            ValidationError = "Validation",
            Error = "Error",
            Result = new ImportResultDto(1, 0),
            Uploading = true,
            PreviewRows = [new Dictionary<string, string> { [CustomerImportFieldNames.Email] = "preview@example.com" }],
            ConflictStates = [new ImportCustomerConflictState("existing@example.com", null, null)],
            Step = ImportCustomersWorkflowStep.DuplicateResolution,
        };
        state.SetPendingFile(
            "customers.csv",
            [1, 2, 3],
            [CustomerImportFieldNames.Email],
            CustomerImportHeaderMatcher.AutoMatch([CustomerImportFieldNames.Email]));

        // Act
        state.RetryCurrentFile();

        // Assert
        (state.PendingFileName).ShouldBe("customers.csv");
        (state.PendingFileBytes).ShouldNotBeEmpty();
        (state.PreviewRows).ShouldBeEmpty();
        (state.ConflictStates).ShouldBeEmpty();
        (state.Result).ShouldBeNull();
        (state.Error).ShouldBeNull();
        (state.ValidationError).ShouldBeNull();
        (state.Uploading).ShouldBeFalse();
        (state.Step).ShouldBe(ImportCustomersWorkflowStep.HeaderMapping);
    }

    [Fact]
    public void BuildConflictDecisions_returns_case_insensitive_decision_map()
    {
        // Arrange
        var keepState = new ImportCustomerConflictState("keep@example.com", null, null);
        keepState.SetDecision("keep", CustomerImportHeaderMatcher.Fields);

        var mixedState = new ImportCustomerConflictState("mixed@example.com", null, null);
        mixedState.SetDecision("mixed", CustomerImportHeaderMatcher.Fields);

        var unresolvedState = new ImportCustomerConflictState("pending@example.com", null, null);

        var state = new ImportCustomersWorkflowState
        {
            ConflictStates = [keepState, mixedState, unresolvedState],
        };

        // Act
        var decisions = state.BuildConflictDecisions();

        // Assert
        (decisions["keep@example.com"]).ShouldBe("keep");
        (decisions["MIXED@example.com"]).ShouldBe("mixed");
        (decisions["pending@example.com"]).ShouldBe(string.Empty);
    }
}
