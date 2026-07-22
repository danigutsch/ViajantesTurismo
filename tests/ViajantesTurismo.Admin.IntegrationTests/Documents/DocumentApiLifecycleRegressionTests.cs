using System.Text;
using System.Text.Json;
using SharedKernel.Testing;

namespace ViajantesTurismo.Admin.IntegrationTests.Documents;

[Trait(TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(TestTraitNames.ScopeName, TestTraits.IntegrationScope)]
[Trait(SharedKernelTestTraitNames.CapabilityName, AdminTestTraitValues.GeneratedDocumentsCapability)]
public sealed class DocumentApiLifecycleRegressionTests(ApiFixture fixture)
{
    [Fact]
    public async Task Missing_booking_generation_returns_not_found_and_persists_rejected_audit()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var bookingId = Guid.CreateVersion7();

        // Act
        using var response = await fixture.Client.PostAsync(
            new Uri($"/api/v1/documents/bookings/{bookingId}/contract-drafts", UriKind.Relative),
            null,
            cancellationToken);
        var draftCount = await fixture.GetDocumentDraftCountForBooking(bookingId, cancellationToken);
        var audits = await fixture.GetDocumentAuditMetadataForBooking(bookingId, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        draftCount.ShouldBe(0);
        var audit = audits.ShouldHaveSingleItem();
        audit.Operation.ShouldBe("Generate");
        audit.Outcome.ShouldBe("Rejected");
        audit.ReasonCode.ShouldBe("BookingNotFound");
        audit.DocumentId.ShouldBeNull();
        audit.BookingId.ShouldBe(bookingId);
        audit.DocumentRevision.ShouldBeNull();
    }

    [Fact]
    public async Task Draft_document_cannot_be_approved_before_review()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var setup = await DocumentApiTestSetup.CreateGeneratedDocument(
            fixture,
            "approve-gate",
            cancellationToken);

        // Act
        using var response = await fixture.Client.PostAsync(
            new Uri($"/api/v1/documents/{setup.Document.Id}/approve", UriKind.Relative),
            null,
            cancellationToken);
        var persisted = await setup.Client.GetDocumentById(setup.Document.Id, cancellationToken);
        var audits = await fixture.GetDocumentAuditMetadata(setup.Document.Id, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var document = persisted.ShouldNotBeNull();
        document.Status.ShouldBe(DocumentStatusDto.DraftGenerated);
        document.HasFinalizedArtifact.ShouldBeFalse();
        var audit = audits.ShouldHaveSingleItem(entry => entry.Operation == "Approve");
        audit.Outcome.ShouldBe("Rejected");
        audit.ReasonCode.ShouldBe("StateConflict");
    }

    [Fact]
    public async Task In_review_document_cannot_be_finalized_before_approval()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var setup = await DocumentApiTestSetup.CreateGeneratedDocument(
            fixture,
            "finalize-gate",
            cancellationToken);
        _ = await setup.Client.BeginReview(setup.Document.Id, cancellationToken);

        // Act
        using var response = await fixture.Client.PostAsync(
            new Uri($"/api/v1/documents/{setup.Document.Id}/finalize", UriKind.Relative),
            null,
            cancellationToken);
        var persisted = await setup.Client.GetDocumentById(setup.Document.Id, cancellationToken);
        var audits = await fixture.GetDocumentAuditMetadata(setup.Document.Id, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var document = persisted.ShouldNotBeNull();
        document.Status.ShouldBe(DocumentStatusDto.InReview);
        document.HasFinalizedArtifact.ShouldBeFalse();
        var audit = audits.ShouldHaveSingleItem(entry => entry.Operation == "Finalize");
        audit.Outcome.ShouldBe("Rejected");
        audit.ReasonCode.ShouldBe("StateConflict");
    }

    [Fact]
    public async Task Approved_document_cannot_be_voided_before_finalization()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var setup = await DocumentApiTestSetup.CreateGeneratedDocument(
            fixture,
            "void-gate",
            cancellationToken);
        _ = await setup.Client.BeginReview(setup.Document.Id, cancellationToken);
        _ = await setup.Client.Approve(setup.Document.Id, cancellationToken);

        // Act
        using var response = await fixture.Client.PostAsync(
            new Uri($"/api/v1/documents/{setup.Document.Id}/void", UriKind.Relative),
            null,
            cancellationToken);
        var persisted = await setup.Client.GetDocumentById(setup.Document.Id, cancellationToken);
        var audits = await fixture.GetDocumentAuditMetadata(setup.Document.Id, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var document = persisted.ShouldNotBeNull();
        document.Status.ShouldBe(DocumentStatusDto.Approved);
        document.HasFinalizedArtifact.ShouldBeFalse();
        var audit = audits.ShouldHaveSingleItem(entry => entry.Operation == "Void");
        audit.Outcome.ShouldBe("Rejected");
        audit.ReasonCode.ShouldBe("StateConflict");
    }

    [Fact]
    public async Task Editing_an_approved_field_revokes_approval_until_reapproved()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var setup = await DocumentApiTestSetup.CreateGeneratedDocument(
            fixture,
            "approval-revocation",
            cancellationToken);
        var editableField = setup.Document.Fields.First(field => field.IsEditable);
        _ = await setup.Client.BeginReview(setup.Document.Id, cancellationToken);
        _ = await setup.Client.Approve(setup.Document.Id, cancellationToken);

        // Act
        var updated = await setup.Client.UpdateField(
            setup.Document.Id,
            editableField.FieldId,
            new UpdateDocumentFieldDto { Value = "Approval must be renewed" },
            cancellationToken);
        using var rejectedFinalize = await fixture.Client.PostAsync(
            new Uri($"/api/v1/documents/{setup.Document.Id}/finalize", UriKind.Relative),
            null,
            cancellationToken);
        var afterRejectedFinalize = await setup.Client.GetDocumentById(setup.Document.Id, cancellationToken);
        _ = await setup.Client.Approve(setup.Document.Id, cancellationToken);
        var finalized = await setup.Client.Finalize(setup.Document.Id, cancellationToken);
        var audits = await fixture.GetDocumentAuditMetadata(setup.Document.Id, cancellationToken);

        // Assert
        updated.Status.ShouldBe(DocumentStatusDto.InReview);
        rejectedFinalize.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var rejectedDocument = afterRejectedFinalize.ShouldNotBeNull();
        rejectedDocument.Status.ShouldBe(DocumentStatusDto.InReview);
        rejectedDocument.HasFinalizedArtifact.ShouldBeFalse();
        finalized.Status.ShouldBe(DocumentStatusDto.Finalized);
        finalized.HasFinalizedArtifact.ShouldBeTrue();
        audits.ShouldHaveSingleItem(entry => entry.Operation == "UpdateField")
            .ReasonCode.ShouldBe("ManualOperation");
        audits.ShouldHaveSingleItem(entry => entry.Operation == "Finalize" && entry.Outcome == "Rejected")
            .ReasonCode.ShouldBe("StateConflict");
        audits.ShouldHaveSingleItem(entry => entry.Operation == "Finalize" && entry.Outcome == "Succeeded")
            .ReasonCode.ShouldBe("ManualFinalize");
    }

    [Fact]
    public async Task Protected_and_unknown_fields_are_rejected_without_mutating_the_document()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var setup = await DocumentApiTestSetup.CreateGeneratedDocument(
            fixture,
            "field-boundary",
            cancellationToken);
        var originalFields = setup.Document.Fields
            .Select(field => (field.FieldId, field.RenderedValue))
            .ToArray();
        using var protectedContent = new StringContent(
            """{"value":"tampered protected value"}""",
            Encoding.UTF8,
            "application/json");
        using var unknownContent = new StringContent(
            """{"value":"unknown value"}""",
            Encoding.UTF8,
            "application/json");

        // Act
        using var protectedResponse = await fixture.Client.PatchAsync(
            new Uri($"/api/v1/documents/{setup.Document.Id}/fields/booking-reference", UriKind.Relative),
            protectedContent,
            cancellationToken);
        using var unknownResponse = await fixture.Client.PatchAsync(
            new Uri($"/api/v1/documents/{setup.Document.Id}/fields/missing-field", UriKind.Relative),
            unknownContent,
            cancellationToken);
        var persisted = await setup.Client.GetDocumentById(setup.Document.Id, cancellationToken);
        var audits = await fixture.GetDocumentAuditMetadata(setup.Document.Id, cancellationToken);

        // Assert
        protectedResponse.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        unknownResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var document = persisted.ShouldNotBeNull();
        document.Status.ShouldBe(DocumentStatusDto.DraftGenerated);
        document.Fields.Select(field => (field.FieldId, field.RenderedValue)).ShouldBe(originalFields);
        var updateAudits = audits.Where(entry => entry.Operation == "UpdateField").ToArray();
        updateAudits.Select(entry => entry.Outcome).ShouldBe(["Rejected", "Rejected"]);
        updateAudits.Select(entry => entry.ReasonCode).ShouldBe(["StateConflict", "ValidationRejected"]);
    }

    [Fact]
    public async Task Staff_override_accepts_4000_characters_and_rejects_4001_without_losing_the_saved_value()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var setup = await DocumentApiTestSetup.CreateGeneratedDocument(
            fixture,
            "override-limit",
            cancellationToken);
        var fieldId = setup.Document.Fields.First(field => field.IsEditable).FieldId;
        var acceptedValue = new string('a', ContractConstants.MaxDocumentFieldValueLength);
        var rejectedValue = acceptedValue + "b";
        using var rejectedContent = new StringContent(
            JsonSerializer.Serialize(new UpdateDocumentFieldDto { Value = rejectedValue }),
            Encoding.UTF8,
            "application/json");

        // Act
        var accepted = await setup.Client.UpdateField(
            setup.Document.Id,
            fieldId,
            new UpdateDocumentFieldDto { Value = acceptedValue },
            cancellationToken);
        using var rejectedResponse = await fixture.Client.PatchAsync(
            new Uri($"/api/v1/documents/{setup.Document.Id}/fields/{fieldId}", UriKind.Relative),
            rejectedContent,
            cancellationToken);
        var persisted = await setup.Client.GetDocumentById(setup.Document.Id, cancellationToken);
        var audits = await fixture.GetDocumentAuditMetadata(setup.Document.Id, cancellationToken);

        // Assert
        accepted.Status.ShouldBe(DocumentStatusDto.InReview);
        rejectedResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var document = persisted.ShouldNotBeNull();
        document.Status.ShouldBe(DocumentStatusDto.InReview);
        document.Fields.ShouldHaveSingleItem(field => field.FieldId == fieldId)
            .RenderedValue.ShouldBe(acceptedValue);
        var updateAudits = audits.Where(entry => entry.Operation == "UpdateField").ToArray();
        updateAudits.Select(entry => entry.Outcome).ShouldBe(["Succeeded", "Rejected"]);
        updateAudits.Select(entry => entry.ReasonCode).ShouldBe(["ManualOperation", "ValidationRejected"]);
    }

    [Fact]
    public async Task Provenance_like_json_members_are_ignored_and_server_audit_metadata_is_preserved()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var setup = await DocumentApiTestSetup.CreateGeneratedDocument(
            fixture,
            "provenance-boundary",
            cancellationToken);
        var editableField = setup.Document.Fields.First(field => field.IsEditable);
        using var content = new StringContent(
            """
            {
              "value": "Trusted update",
              "actorId": "attacker",
              "correlationId": "attacker",
              "sourceVersion": "attacker",
              "templateId": "attacker",
              "isEditable": false
            }
            """,
            Encoding.UTF8,
            "application/json");

        // Act
        using var response = await fixture.Client.PatchAsync(
            new Uri($"/api/v1/documents/{setup.Document.Id}/fields/{editableField.FieldId}", UriKind.Relative),
            content,
            cancellationToken);
        var updated = await response.Content.ReadFromJsonAsync<GetDocumentDto>(cancellationToken);
        var audits = await fixture.GetDocumentAuditMetadata(setup.Document.Id, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var document = updated.ShouldNotBeNull();
        document.TemplateId.ShouldBe(setup.Document.TemplateId);
        document.SourceVersion.ShouldBe(setup.Document.SourceVersion);
        var field = document.Fields.ShouldHaveSingleItem(item => item.FieldId == editableField.FieldId);
        field.RenderedValue.ShouldBe("Trusted update");
        field.IsEditable.ShouldBeTrue();
        var audit = audits.ShouldHaveSingleItem(entry => entry.Operation == "UpdateField");
        audit.ActorId.ShouldBe(KeycloakConformanceClient.ConformanceUserId);
        string.Equals(audit.CorrelationId, "attacker", StringComparison.Ordinal).ShouldBeFalse();
    }

    [Fact]
    public async Task Finalized_artifact_encodes_malicious_staff_markup_as_text()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var setup = await DocumentApiTestSetup.CreateGeneratedDocument(
            fixture,
            "markup-encoding",
            cancellationToken);
        var fieldId = setup.Document.Fields.First(field => field.IsEditable).FieldId;
        const string maliciousValue = "<script>alert(1)</script><img src=x onerror=alert(2)>";

        // Act
        _ = await setup.Client.UpdateField(
            setup.Document.Id,
            fieldId,
            new UpdateDocumentFieldDto { Value = maliciousValue },
            cancellationToken);
        _ = await setup.Client.Approve(setup.Document.Id, cancellationToken);
        _ = await setup.Client.Finalize(setup.Document.Id, cancellationToken);
        var artifact = await setup.Client.DownloadFinalizedArtifact(setup.Document.Id, cancellationToken);
        var audits = await fixture.GetDocumentAuditMetadata(setup.Document.Id, cancellationToken);

        // Assert
        var downloaded = artifact.ShouldNotBeNull();
        var html = Encoding.UTF8.GetString(downloaded.Content.ToArray());
        html.ShouldContain("&lt;script&gt;alert(1)&lt;/script&gt;", StringComparison.Ordinal);
        html.ShouldContain("&lt;img src=x onerror=alert(2)&gt;", StringComparison.Ordinal);
        html.ShouldNotContain("<script", StringComparison.OrdinalIgnoreCase);
        html.ShouldNotContain("<img", StringComparison.OrdinalIgnoreCase);
        audits.ShouldHaveSingleItem(entry => entry.Operation == "UpdateField")
            .ReasonCode.ShouldBe("ManualOperation");
        audits.ShouldHaveSingleItem(entry => entry.Operation == "Finalize")
            .ReasonCode.ShouldBe("ManualFinalize");
        audits.ShouldHaveSingleItem(entry => entry.Operation == "Download")
            .ReasonCode.ShouldBe("None");
    }

    [Fact]
    public async Task Regeneration_does_not_mutate_the_finalized_predecessor()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var setup = await DocumentApiTestSetup.CreateGeneratedDocument(
            fixture,
            "sealed-predecessor",
            cancellationToken);
        var fieldId = setup.Document.Fields.First(field => field.IsEditable).FieldId;
        const string firstValue = "Sealed revision one";
        const string secondValue = "Editable revision two";
        _ = await setup.Client.UpdateField(
            setup.Document.Id,
            fieldId,
            new UpdateDocumentFieldDto { Value = firstValue },
            cancellationToken);
        _ = await setup.Client.Approve(setup.Document.Id, cancellationToken);
        _ = await setup.Client.Finalize(setup.Document.Id, cancellationToken);
        var artifactBeforeRegeneration = await setup.Client.DownloadFinalizedArtifact(
            setup.Document.Id,
            cancellationToken);

        // Act
        var replacement = await setup.Client.Regenerate(setup.Document.Id, cancellationToken);
        var updatedReplacement = await setup.Client.UpdateField(
            replacement.Id,
            fieldId,
            new UpdateDocumentFieldDto { Value = secondValue },
            cancellationToken);
        var predecessor = await setup.Client.GetDocumentById(setup.Document.Id, cancellationToken);
        var artifactAfterRegeneration = await setup.Client.DownloadFinalizedArtifact(
            setup.Document.Id,
            cancellationToken);
        var replacementAudits = await fixture.GetDocumentAuditMetadata(replacement.Id, cancellationToken);

        // Assert
        var firstArtifact = artifactBeforeRegeneration.ShouldNotBeNull();
        var retainedArtifact = artifactAfterRegeneration.ShouldNotBeNull();
        retainedArtifact.Content.ToArray().ShouldBe(firstArtifact.Content.ToArray());
        var original = predecessor.ShouldNotBeNull();
        original.Status.ShouldBe(DocumentStatusDto.Finalized);
        original.Fields.ShouldHaveSingleItem(field => field.FieldId == fieldId)
            .RenderedValue.ShouldBe(firstValue);
        replacement.Revision.ShouldBe(2);
        replacement.ReplacesDocumentId.ShouldBe(setup.Document.Id);
        updatedReplacement.Fields.ShouldHaveSingleItem(field => field.FieldId == fieldId)
            .RenderedValue.ShouldBe(secondValue);
        replacementAudits.ShouldHaveSingleItem(entry => entry.Operation == "Regenerate")
            .ReasonCode.ShouldBe("ManualRegeneration");
    }

    [Fact]
    public async Task Replacement_finalization_audit_failure_rolls_back_both_revisions()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var setup = await DocumentApiTestSetup.CreateGeneratedDocument(
            fixture,
            "replacement-rollback",
            cancellationToken);
        _ = await setup.Client.BeginReview(setup.Document.Id, cancellationToken);
        _ = await setup.Client.Approve(setup.Document.Id, cancellationToken);
        _ = await setup.Client.Finalize(setup.Document.Id, cancellationToken);
        var replacement = await setup.Client.Regenerate(setup.Document.Id, cancellationToken);
        _ = await setup.Client.BeginReview(replacement.Id, cancellationToken);
        _ = await setup.Client.Approve(replacement.Id, cancellationToken);

        // Act
        HttpResponseMessage failedResponse;
        await using (var scenario = await fixture.CreateDocumentAuditInsertFailureScenario(
            setup.BookingId,
            cancellationToken))
        {
            failedResponse = await fixture.Client.PostAsync(
                new Uri($"/api/v1/documents/{replacement.Id}/finalize", UriKind.Relative),
                null,
                cancellationToken);
        }

        using var response = failedResponse;
        var predecessor = await setup.Client.GetDocumentById(setup.Document.Id, cancellationToken);
        var persistedReplacement = await setup.Client.GetDocumentById(replacement.Id, cancellationToken);
        var artifact = await setup.Client.DownloadFinalizedArtifact(setup.Document.Id, cancellationToken);
        var replacementAudits = await fixture.GetDocumentAuditMetadata(replacement.Id, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        var original = predecessor.ShouldNotBeNull();
        original.Status.ShouldBe(DocumentStatusDto.Finalized);
        original.HasFinalizedArtifact.ShouldBeTrue();
        var rolledBackReplacement = persistedReplacement.ShouldNotBeNull();
        rolledBackReplacement.Status.ShouldBe(DocumentStatusDto.Approved);
        rolledBackReplacement.HasFinalizedArtifact.ShouldBeFalse();
        artifact.ShouldNotBeNull();
        replacementAudits.Any(entry => entry.Operation == "Finalize" && entry.Outcome == "Succeeded")
            .ShouldBeFalse();
    }

    [Fact]
    public async Task Finalized_download_fails_closed_when_audit_persistence_fails()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var setup = await DocumentApiTestSetup.CreateGeneratedDocument(
            fixture,
            "download-fail-closed",
            cancellationToken);
        var fieldId = setup.Document.Fields.First(field => field.IsEditable).FieldId;
        const string uniqueMarker = "secret-download-marker-72f4c3";
        _ = await setup.Client.UpdateField(
            setup.Document.Id,
            fieldId,
            new UpdateDocumentFieldDto { Value = uniqueMarker },
            cancellationToken);
        _ = await setup.Client.Approve(setup.Document.Id, cancellationToken);
        _ = await setup.Client.Finalize(setup.Document.Id, cancellationToken);

        // Act
        HttpResponseMessage failedResponse;
        string responseBody;
        await using (var scenario = await fixture.CreateDocumentAuditInsertFailureScenario(
            setup.BookingId,
            cancellationToken))
        {
            failedResponse = await fixture.Client.GetAsync(
                new Uri($"/api/v1/documents/{setup.Document.Id}/download", UriKind.Relative),
                cancellationToken);
            responseBody = await failedResponse.Content.ReadAsStringAsync(cancellationToken);
        }

        using var response = failedResponse;
        var audits = await fixture.GetDocumentAuditMetadata(setup.Document.Id, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        string.Equals(
            response.Content.Headers.ContentType?.MediaType,
            "text/html",
            StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
        response.Content.Headers.ContentDisposition.ShouldBeNull();
        responseBody.ShouldNotContain("<!DOCTYPE html>", StringComparison.OrdinalIgnoreCase);
        responseBody.ShouldNotContain(uniqueMarker, StringComparison.Ordinal);
        audits.Any(entry => entry.Operation == "Download" && entry.Outcome == "Succeeded")
            .ShouldBeFalse();
    }
}
