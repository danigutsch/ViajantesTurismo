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
        TestAssert.Equal("customers.csv", state.PendingFileName);
        TestAssert.Equal([1, 2, 3], state.PendingFileBytes);
        TestAssert.Equal(csvHeaders, state.CsvHeaders);
        TestAssert.Equal(fieldMappings, state.FieldMappings);
        TestAssert.Empty(state.UserMappings);
        TestAssert.Empty(state.PreviewRows);
        TestAssert.Empty(state.ConflictStates);
        TestAssert.Null(state.Result);
        TestAssert.Null(state.Error);
        TestAssert.Null(state.ValidationError);
        TestAssert.Equal(ImportCustomersWorkflowStep.HeaderMapping, state.Step);
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
        TestAssert.Equal(string.Empty, state.PendingFileName);
        TestAssert.Empty(state.PendingFileBytes);
        TestAssert.Empty(state.CsvHeaders);
        TestAssert.Empty(state.FieldMappings);
        TestAssert.Empty(state.UserMappings);
        TestAssert.Empty(state.PreviewRows);
        TestAssert.Empty(state.ConflictStates);
        TestAssert.Null(state.Result);
        TestAssert.Null(state.Error);
        TestAssert.Null(state.ValidationError);
        TestAssert.False(state.Uploading);
        TestAssert.Equal(ImportCustomersWorkflowStep.FileSelection, state.Step);
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
        TestAssert.Equal("customers.csv", state.PendingFileName);
        TestAssert.NotEmpty(state.PendingFileBytes);
        TestAssert.Empty(state.PreviewRows);
        TestAssert.Empty(state.ConflictStates);
        TestAssert.Null(state.Result);
        TestAssert.Null(state.Error);
        TestAssert.Null(state.ValidationError);
        TestAssert.False(state.Uploading);
        TestAssert.Equal(ImportCustomersWorkflowStep.HeaderMapping, state.Step);
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
        TestAssert.Equal("keep", decisions["keep@example.com"]);
        TestAssert.Equal("mixed", decisions["MIXED@example.com"]);
        TestAssert.Equal(string.Empty, decisions["pending@example.com"]);
    }
}
