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
        _ = state.Upload.Begin("customers.csv", 3, isRetry: false);

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
            PreviewRows = [new Dictionary<string, string> { [CustomerImportFieldNames.Email] = "preview@example.com" }],
            ConflictStates = [new ImportCustomerConflictState("existing@example.com", null, null)],
            Step = ImportCustomersWorkflowStep.DuplicateResolution,
        };
        state.SetPendingFile(
            "customers.csv",
            [1, 2, 3],
            [CustomerImportFieldNames.Email],
            CustomerImportHeaderMatcher.AutoMatch([CustomerImportFieldNames.Email]));
        _ = state.Upload.Begin("customers.csv", 3, isRetry: false);

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

    [Fact]
    public void Upload_operation_reports_only_authoritative_bytes_and_keeps_server_processing_indeterminate()
    {
        // Arrange
        var state = new ImportCustomersWorkflowState();
        var operationId = state.Upload.Begin("customers.csv", 100, isRetry: false);

        // Act
        state.Upload.ReportBytes(operationId, 25);

        // Assert
        state.Upload.Status.ShouldBe(UploadOperationStatus.Uploading);
        state.Upload.BytesTransferred.ShouldBe(25);
        state.Upload.TotalBytes.ShouldBe(100);
        state.Upload.IsDeterminate.ShouldBeTrue();

        // Act
        state.Upload.ReportBytes(operationId, 100);

        // Assert
        state.Upload.Status.ShouldBe(UploadOperationStatus.Processing);
        state.Upload.IsDeterminate.ShouldBeFalse();
    }

    [Fact]
    public void Upload_operation_ignores_callbacks_after_cancellation()
    {
        // Arrange
        var state = new ImportCustomersWorkflowState();
        var operationId = state.Upload.Begin("customers.csv", 100, isRetry: false);
        state.Upload.ReportBytes(operationId, 25);

        // Act
        state.Upload.Cancel(operationId);
        state.Upload.ReportBytes(operationId, 100);
        state.Upload.Complete(operationId);

        // Assert
        state.Upload.Status.ShouldBe(UploadOperationStatus.Canceled);
        state.Upload.BytesTransferred.ShouldBe(25);
    }

    [Fact]
    public void Retry_creates_a_new_operation_that_stale_callbacks_cannot_replace()
    {
        // Arrange
        var state = new ImportCustomersWorkflowState();
        var canceledOperationId = state.Upload.Begin("customers.csv", 100, isRetry: false);
        state.Upload.Cancel(canceledOperationId);

        // Act
        var retryOperationId = state.Upload.Begin("customers.csv", 100, isRetry: true);
        state.Upload.ReportBytes(canceledOperationId, 100);
        state.Upload.Complete(canceledOperationId);

        // Assert
        retryOperationId.ShouldNotBe(canceledOperationId);
        state.Upload.Status.ShouldBe(UploadOperationStatus.Retrying);
        state.Upload.BytesTransferred.ShouldBe(0);
    }

    [Fact]
    public void Upload_operation_rejects_a_second_active_operation()
    {
        // Arrange
        var state = new ImportCustomersWorkflowState();
        _ = state.Upload.Begin("customers.csv", 100, isRetry: false);

        // Act
        Action action = () => state.Upload.Begin("other.csv", 100, isRetry: false);

        // Assert
        action.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    public void Upload_operation_rejects_completion_before_all_declared_bytes_are_reported()
    {
        // Arrange
        var state = new ImportCustomersWorkflowState();
        var operationId = state.Upload.Begin("customers.csv", 100, isRetry: false);
        state.Upload.ReportBytes(operationId, 50);

        // Act
        Action action = () => state.Upload.Complete(operationId);

        // Assert
        action.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    public void Upload_announcements_are_throttled_between_percentage_boundaries()
    {
        // Arrange
        var state = new ImportCustomersWorkflowState();
        var operationId = state.Upload.Begin("customers.csv", 100, isRetry: false);

        // Act
        state.Upload.ReportBytes(operationId, 1);
        var firstAnnouncement = state.Upload.Announcement;
        state.Upload.ReportBytes(operationId, 2);
        var secondAnnouncement = state.Upload.Announcement;
        state.Upload.ReportBytes(operationId, 10);

        // Assert
        firstAnnouncement.ShouldBe("Uploading customers.csv.");
        secondAnnouncement.ShouldBe(firstAnnouncement);
        state.Upload.Announcement.ShouldBe("customers.csv upload is 10% complete.");
    }

    [Fact]
    public void First_percentage_announcement_is_not_replaced_with_a_less_informative_status()
    {
        // Arrange
        var state = new ImportCustomersWorkflowState();
        var operationId = state.Upload.Begin("customers.csv", 100, isRetry: false);

        // Act
        state.Upload.ReportBytes(operationId, 10);
        var boundaryAnnouncement = state.Upload.Announcement;
        state.Upload.ReportBytes(operationId, 11);

        // Assert
        boundaryAnnouncement.ShouldBe("customers.csv upload is 10% complete.");
        state.Upload.Announcement.ShouldBe(boundaryAnnouncement);
    }
}
