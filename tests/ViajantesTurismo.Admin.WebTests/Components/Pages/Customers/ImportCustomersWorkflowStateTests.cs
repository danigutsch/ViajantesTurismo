using ViajantesTurismo.Admin.Web.Components.Pages.Customers;

namespace ViajantesTurismo.Admin.WebTests.Components.Pages.Customers;

public sealed class ImportCustomersWorkflowStateTests
{
    [Fact]
    public void SetPendingFile_Stores_File_Data_And_Resets_Transient_State()
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
        Assert.Equal("customers.csv", state.PendingFileName);
        Assert.Equal([1, 2, 3], state.PendingFileBytes);
        Assert.Equal(csvHeaders, state.CsvHeaders);
        Assert.Equal(fieldMappings, state.FieldMappings);
        Assert.Empty(state.UserMappings);
        Assert.Empty(state.PreviewRows);
        Assert.Empty(state.ConflictStates);
        Assert.Null(state.Result);
        Assert.Null(state.Error);
        Assert.Null(state.ValidationError);
        Assert.Equal(ImportCustomersWorkflowStep.HeaderMapping, state.Step);
    }

    [Fact]
    public void ResetToFileSelection_Clears_All_State_And_Returns_To_FileSelection()
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
        Assert.Equal(string.Empty, state.PendingFileName);
        Assert.Empty(state.PendingFileBytes);
        Assert.Empty(state.CsvHeaders);
        Assert.Empty(state.FieldMappings);
        Assert.Empty(state.UserMappings);
        Assert.Empty(state.PreviewRows);
        Assert.Empty(state.ConflictStates);
        Assert.Null(state.Result);
        Assert.Null(state.Error);
        Assert.Null(state.ValidationError);
        Assert.False(state.Uploading);
        Assert.Equal(ImportCustomersWorkflowStep.FileSelection, state.Step);
    }

    [Fact]
    public void RetryCurrentFile_When_Pending_File_Exists_Returns_To_HeaderMapping_And_Clears_Transient_State()
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
        Assert.Equal("customers.csv", state.PendingFileName);
        Assert.NotEmpty(state.PendingFileBytes);
        Assert.Empty(state.PreviewRows);
        Assert.Empty(state.ConflictStates);
        Assert.Null(state.Result);
        Assert.Null(state.Error);
        Assert.Null(state.ValidationError);
        Assert.False(state.Uploading);
        Assert.Equal(ImportCustomersWorkflowStep.HeaderMapping, state.Step);
    }

    [Fact]
    public void BuildConflictDecisions_Returns_Case_Insensitive_Decision_Map()
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
        Assert.Equal("keep", decisions["keep@example.com"]);
        Assert.Equal("mixed", decisions["MIXED@example.com"]);
        Assert.Equal(string.Empty, decisions["pending@example.com"]);
    }
}
