using System.Text.Json;
using SharedKernel.Testing;

namespace ViajantesTurismo.Admin.IntegrationTests.Documents;

[Trait(TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(TestTraitNames.ScopeName, TestTraits.IntegrationScope)]
[Trait(SharedKernelTestTraitNames.CapabilityName, AdminTestTraitValues.GeneratedDocumentsCapability)]
public sealed class DocumentApiEndpointTests(ApiFixture fixture)
{
    [Fact]
    public void Authenticated_admin_token_contains_an_opaque_subject()
    {
        // Arrange
        var accessToken = fixture.Client.DefaultRequestHeaders.Authorization?.Parameter;
        var confirmedAccessToken = accessToken.ShouldNotBeNull();
        var payloadSegment = confirmedAccessToken.Split('.')[1];
        var normalizedPayload = payloadSegment.Replace('-', '+').Replace('_', '/');
        var paddingLength = (4 - (normalizedPayload.Length % 4)) % 4;
        var payloadBytes = Convert.FromBase64String(normalizedPayload.PadRight(normalizedPayload.Length + paddingLength, '='));
        using var payload = JsonDocument.Parse(payloadBytes);

        // Act
        var hasSubject = payload.RootElement.TryGetProperty("sub", out var subject);
        var actorId = hasSubject && subject.ValueKind is JsonValueKind.String ? subject.GetString() : null;

        // Assert
        hasSubject.ShouldBeTrue();
        subject.ValueKind.ShouldBe(JsonValueKind.String);
        string.IsNullOrWhiteSpace(actorId).ShouldBeFalse();
        actorId.ShouldBe(KeycloakConformanceClient.ConformanceUserId);
    }

    [Fact]
    public async Task Authenticated_admin_can_read_a_missing_document_without_leaking_audit_errors()
    {
        // Arrange
        var documentId = Guid.CreateVersion7();

        // Act
        using var response = await fixture.Client.GetAsync(
            new Uri($"/api/v1/documents/{documentId}", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var audits = await fixture.GetDocumentAuditMetadata(documentId, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var audit = audits.ShouldHaveSingleItem();
        audit.Operation.ShouldBe("Read");
        audit.Outcome.ShouldBe("Rejected");
        audit.ReasonCode.ShouldBe("DocumentNotFound");
        audit.DocumentId.ShouldBe(documentId);
        audit.BookingId.ShouldBeNull();
        audit.DocumentRevision.ShouldBeNull();
        audit.ActorId.ShouldBe(KeycloakConformanceClient.ConformanceUserId);
        string.IsNullOrWhiteSpace(audit.CorrelationId).ShouldBeFalse();
        audit.RetentionExpiresAt.ShouldBe(audit.OccurredAtUtc.AddMonths(24));
    }

    [Fact]
    public async Task Authenticated_admin_can_generate_contract_draft_for_an_accepted_booking()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var tour = await fixture.Client.CreateTestTour("document-generation", "Document generation", cancellationToken);
        var customer = await fixture.Client.CreateTestCustomer("Document", "Generation", cancellationToken);
        var booking = await fixture.Client.CreateTestBooking(tour.Id, customer.Id, null, cancellationToken);
        using var confirmResponse = await fixture.Client.ConfirmBooking(booking.Id, cancellationToken);
        confirmResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Act
        using var response = await fixture.Client.PostAsync(
            new Uri($"/api/v1/documents/bookings/{booking.Id}/contract-drafts", UriKind.Relative),
            null,
            cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var document = (await response.Content.ReadFromJsonAsync<GetDocumentDto>(cancellationToken)).ShouldNotBeNull();
        document.BookingId.ShouldBe(booking.Id);
    }

    [Fact]
    public async Task Accepted_booking_with_joined_included_services_over_4000_characters_generates_and_reloads_the_full_document_value()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var includedServices = Enumerable.Range(1, 17)
            .Select(index => $"Service {index:D2} {new string('x', 230)}")
            .ToArray();
        var expectedValue = string.Join(", ", includedServices);
        var tour = await fixture.Client.CreateTestTour(
            "document-long-services",
            "Document long services",
            includedServices,
            cancellationToken);
        var customer = await fixture.Client.CreateTestCustomer("Document", "Long services", cancellationToken);
        var booking = await fixture.Client.CreateTestBooking(tour.Id, customer.Id, null, cancellationToken);
        using var confirmResponse = await fixture.Client.ConfirmBooking(booking.Id, cancellationToken);
        confirmResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var documents = new DocumentsApiClient(fixture.Client);

        // Act
        var generated = await documents.GenerateContractDraft(booking.Id, cancellationToken);
        var reloaded = await documents.GetDocumentById(generated.Id, cancellationToken);

        // Assert
        expectedValue.Length.ShouldBe(4_129);
        var generatedField = generated.Fields.ShouldHaveSingleItem(field => field.FieldId == "included-services");
        var reloadedDocument = reloaded.ShouldNotBeNull();
        var reloadedField = reloadedDocument.Fields.ShouldHaveSingleItem(field => field.FieldId == "included-services");
        generatedField.RenderedValue.ShouldBe(expectedValue);
        reloadedField.RenderedValue.ShouldBe(expectedValue);
    }

    [Fact]
    public async Task Null_document_field_update_is_rejected_and_audited()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var tour = await fixture.Client.CreateTestTour("document-null-field", "Document null field", cancellationToken);
        var customer = await fixture.Client.CreateTestCustomer("Document", "Null field", cancellationToken);
        var booking = await fixture.Client.CreateTestBooking(tour.Id, customer.Id, null, cancellationToken);
        using var confirmResponse = await fixture.Client.ConfirmBooking(booking.Id, cancellationToken);
        confirmResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var documents = new DocumentsApiClient(fixture.Client);
        var document = await documents.GenerateContractDraft(booking.Id, cancellationToken);
        var fieldId = document.Fields.First(field => field.IsEditable).FieldId;
        using var content = new StringContent(
            """{"value":null}""",
            System.Text.Encoding.UTF8,
            "application/json");

        // Act
        using var response = await fixture.Client.PatchAsync(
            new Uri($"/api/v1/documents/{document.Id}/fields/{fieldId}", UriKind.Relative),
            content,
            cancellationToken);
        var audits = await fixture.GetDocumentAuditMetadata(document.Id, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var audit = audits.ShouldHaveSingleItem(entry => entry.Operation == "UpdateField");
        audit.Operation.ShouldBe("UpdateField");
        audit.Outcome.ShouldBe("Rejected");
        audit.ReasonCode.ShouldBe("ValidationRejected");
    }

    [Theory]
    [InlineData(BookingStatusDto.Pending)]
    [InlineData(BookingStatusDto.Cancelled)]
    public async Task Contract_draft_generation_rejects_pending_and_cancelled_bookings(BookingStatusDto bookingStatus)
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var tour = await fixture.Client.CreateTestTour("document-ineligible", "Document ineligible", cancellationToken);
        var customer = await fixture.Client.CreateTestCustomer("Document", "Ineligible", cancellationToken);
        var booking = await fixture.Client.CreateTestBooking(tour.Id, customer.Id, null, cancellationToken);
        booking.Status.ShouldBe(BookingStatusDto.Pending);

        if (bookingStatus == BookingStatusDto.Cancelled)
        {
            using var cancelResponse = await fixture.Client.CancelBooking(booking.Id, cancellationToken);
            cancelResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        // Act
        using var response = await fixture.Client.PostAsync(
            new Uri($"/api/v1/documents/bookings/{booking.Id}/contract-drafts", UriKind.Relative),
            null,
            cancellationToken);
        var audits = await fixture.GetDocumentAuditMetadataForBooking(booking.Id, cancellationToken);
        var draftCount = await fixture.GetDocumentDraftCountForBooking(booking.Id, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        draftCount.ShouldBe(0);
        var audit = audits.ShouldHaveSingleItem();
        audit.Operation.ShouldBe("Generate");
        audit.Outcome.ShouldBe("Rejected");
        audit.ReasonCode.ShouldBe("StateConflict");
        audit.DocumentId.ShouldBeNull();
        audit.BookingId.ShouldBe(booking.Id);
        audit.DocumentRevision.ShouldBeNull();
        audit.ActorId.ShouldBe(KeycloakConformanceClient.ConformanceUserId);
        string.IsNullOrWhiteSpace(audit.CorrelationId).ShouldBeFalse();
    }

    [Fact]
    public async Task Contract_draft_generation_rejects_a_booking_cancelled_during_persistence()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var tour = await fixture.Client.CreateTestTour("document-generation-race", "Document generation race", cancellationToken);
        var customer = await fixture.Client.CreateTestCustomer("Document", "Generation race", cancellationToken);
        var booking = await fixture.Client.CreateTestBooking(tour.Id, customer.Id, null, cancellationToken);
        using var confirmResponse = await fixture.Client.ConfirmBooking(booking.Id, cancellationToken);
        confirmResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        await using var scenario = await fixture.CreateBookingCancellationAtDocumentPersistenceScenario(booking.Id, cancellationToken);
        await scenario.HoldCancellation(cancellationToken);

        // Act
        var responseTask = fixture.Client.PostAsync(
            new Uri($"/api/v1/documents/bookings/{booking.Id}/contract-drafts", UriKind.Relative),
            null,
            cancellationToken);
        await scenario.WaitForBlockedDocumentPersistence(1, cancellationToken);
        await scenario.CommitCancellation(cancellationToken);
        using var response = await responseTask;
        var draftCount = await fixture.GetDocumentDraftCountForBooking(booking.Id, cancellationToken);
        var audits = await fixture.GetDocumentAuditMetadataForBooking(booking.Id, cancellationToken);
        var bookingStatus = await scenario.GetBookingStatus(cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        draftCount.ShouldBe(0);
        var audit = audits.ShouldHaveSingleItem(audit => audit.Operation == "Generate");
        audit.Outcome.ShouldBe("Rejected");
        audit.ReasonCode.ShouldBe("StateConflict");
        audit.BookingId.ShouldBe(booking.Id);
        audit.DocumentId.ShouldBeNull();
        audit.DocumentRevision.ShouldBeNull();
        bookingStatus.ShouldBe("Cancelled");
    }

    [Fact]
    public async Task Document_review_concurrency_returns_conflict_and_persists_rejected_audit_record()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var tour = await fixture.Client.CreateTestTour("document-concurrency", "Document concurrency", cancellationToken);
        var customer = await fixture.Client.CreateTestCustomer("Document", "Concurrency", cancellationToken);
        var booking = await fixture.Client.CreateTestBooking(tour.Id, customer.Id, null, cancellationToken);
        using var confirmResponse = await fixture.Client.ConfirmBooking(booking.Id, cancellationToken);
        confirmResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var documents = new DocumentsApiClient(fixture.Client);
        var document = await documents.GenerateContractDraft(booking.Id, cancellationToken);
        await using var scenario = await fixture.CreateDocumentMutationConcurrencyScenario(document.Id, cancellationToken);

        // Act
        using var response = await fixture.Client.PostAsync(
            new Uri($"/api/v1/documents/{document.Id}/review", UriKind.Relative),
            null,
            cancellationToken);
        var audits = await scenario.GetDocumentAuditMetadata(cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var reviewAudit = audits.ShouldHaveSingleItem(audit => audit.Operation == "BeginReview");
        reviewAudit.Outcome.ShouldBe("Rejected");
        reviewAudit.ReasonCode.ShouldBe("StateConflict");
        reviewAudit.DocumentId.ShouldBe(document.Id);
        reviewAudit.ActorId.ShouldBe(KeycloakConformanceClient.ConformanceUserId);
        string.IsNullOrWhiteSpace(reviewAudit.CorrelationId).ShouldBeFalse();
        reviewAudit.BookingId.ShouldBe(document.BookingId);
        reviewAudit.DocumentRevision.ShouldBe(document.Revision);
    }

    [Fact]
    public async Task Document_read_and_rejected_download_persist_retained_metadata_only_audits()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var tour = await fixture.Client.CreateTestTour("document-audit-boundary", "Document audit boundary", cancellationToken);
        var customer = await fixture.Client.CreateTestCustomer("Document", "Audit boundary", cancellationToken);
        var booking = await fixture.Client.CreateTestBooking(tour.Id, customer.Id, null, cancellationToken);
        using var confirmResponse = await fixture.Client.ConfirmBooking(booking.Id, cancellationToken);
        confirmResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var documents = new DocumentsApiClient(fixture.Client);
        var document = await documents.GenerateContractDraft(booking.Id, cancellationToken);

        // Act
        using var readResponse = await fixture.Client.GetAsync(
            new Uri($"/api/v1/documents/{document.Id}", UriKind.Relative),
            cancellationToken);
        using var downloadResponse = await fixture.Client.GetAsync(
            new Uri($"/api/v1/documents/{document.Id}/download", UriKind.Relative),
            cancellationToken);
        var audits = await fixture.GetDocumentAuditMetadata(document.Id, cancellationToken);

        // Assert
        readResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        downloadResponse.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        audits.Select(audit => audit.Operation).ShouldBe(["Generate", "Read", "Download"]);
        audits.Select(audit => audit.Outcome).ShouldBe(["Succeeded", "Succeeded", "Rejected"]);
        audits.Select(audit => audit.ReasonCode).ShouldBe(["ManualOperation", "None", "ArtifactUnavailable"]);
        foreach (var audit in audits)
        {
            audit.DocumentId.ShouldBe(document.Id);
            audit.BookingId.ShouldBe(booking.Id);
            audit.DocumentRevision.ShouldBe(1);
            audit.ActorId.ShouldBe(KeycloakConformanceClient.ConformanceUserId);
            string.IsNullOrWhiteSpace(audit.CorrelationId).ShouldBeFalse();
            audit.RetentionExpiresAt.ShouldBe(audit.OccurredAtUtc.AddMonths(24));
        }
    }

    [Fact]
    public async Task Audit_insert_failure_rolls_back_generated_draft_and_audit_record()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var tour = await fixture.Client.CreateTestTour("document-audit-atomicity", "Document audit atomicity", cancellationToken);
        var customer = await fixture.Client.CreateTestCustomer("Document", "Audit atomicity", cancellationToken);
        var booking = await fixture.Client.CreateTestBooking(tour.Id, customer.Id, null, cancellationToken);
        using var confirmResponse = await fixture.Client.ConfirmBooking(booking.Id, cancellationToken);
        confirmResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        await using var scenario = await fixture.CreateDocumentAuditInsertFailureScenario(booking.Id, cancellationToken);

        // Act
        using var response = await fixture.Client.PostAsync(
            new Uri($"/api/v1/documents/bookings/{booking.Id}/contract-drafts", UriKind.Relative),
            null,
            cancellationToken);
        var draftCount = await fixture.GetDocumentDraftCountForBooking(booking.Id, cancellationToken);
        var audits = await fixture.GetDocumentAuditMetadataForBooking(booking.Id, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        draftCount.ShouldBe(0);
        audits.ShouldBeEmpty();
    }

    [Fact]
    public async Task Concurrent_contract_generation_returns_conflict_without_duplicate_success_audits()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var tour = await fixture.Client.CreateTestTour("document-audit-idempotency", "Document audit idempotency", cancellationToken);
        var customer = await fixture.Client.CreateTestCustomer("Document", "Audit idempotency", cancellationToken);
        var booking = await fixture.Client.CreateTestBooking(tour.Id, customer.Id, null, cancellationToken);
        using var confirmResponse = await fixture.Client.ConfirmBooking(booking.Id, cancellationToken);
        confirmResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var route = new Uri($"/api/v1/documents/bookings/{booking.Id}/contract-drafts", UriKind.Relative);
        await using var scenario = await fixture.CreateBookingCancellationAtDocumentPersistenceScenario(
            booking.Id,
            cancellationToken);
        await scenario.HoldCancellation(cancellationToken);

        // Act
        var firstRequest = fixture.Client.PostAsync(route, null, cancellationToken);
        await scenario.WaitForBlockedDocumentPersistence(1, cancellationToken);
        var secondRequest = fixture.Client.PostAsync(route, null, cancellationToken);
        await scenario.WaitForBlockedDocumentPersistence(2, cancellationToken);
        var firstRequestWasBlocked = !firstRequest.IsCompleted;
        var secondRequestWasBlocked = !secondRequest.IsCompleted;
        await scenario.RollbackCancellation(cancellationToken);
        using var firstResponse = await firstRequest;
        using var secondResponse = await secondRequest;
        var draftCount = await fixture.GetDocumentDraftCountForBooking(booking.Id, cancellationToken);
        var audits = await fixture.GetDocumentAuditMetadataForBooking(booking.Id, cancellationToken);
        var bookingStatus = await scenario.GetBookingStatus(cancellationToken);

        // Assert
        firstRequestWasBlocked.ShouldBeTrue();
        secondRequestWasBlocked.ShouldBeTrue();
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        secondResponse.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        bookingStatus.ShouldBe("Confirmed");
        draftCount.ShouldBe(1);
        var generationAudits = audits.Where(audit => audit.Operation == "Generate").ToArray();
        generationAudits.ShouldHaveCount(2);
        generationAudits.ShouldHaveSingleItem(audit => audit.Outcome == "Succeeded")
            .ReasonCode.ShouldBe("ManualOperation");
        generationAudits.ShouldHaveSingleItem(audit => audit.Outcome == "Rejected")
            .ReasonCode.ShouldBe("StateConflict");
    }

    [Fact]
    public async Task Regeneration_concurrency_conflict_does_not_create_a_sibling_revision()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var tour = await fixture.Client.CreateTestTour("document-regeneration-race", "Document regeneration race", cancellationToken);
        var customer = await fixture.Client.CreateTestCustomer("Document", "Regeneration race", cancellationToken);
        var booking = await fixture.Client.CreateTestBooking(tour.Id, customer.Id, null, cancellationToken);
        using var confirmResponse = await fixture.Client.ConfirmBooking(booking.Id, cancellationToken);
        confirmResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var documents = new DocumentsApiClient(fixture.Client);
        var generated = await documents.GenerateContractDraft(booking.Id, cancellationToken);
        var route = new Uri($"/api/v1/documents/{generated.Id}/regenerate", UriKind.Relative);
        var replacement = await documents.Regenerate(generated.Id, cancellationToken);
        await using var scenario = await fixture.CreateDocumentMutationConcurrencyScenario(
            generated.Id,
            cancellationToken);

        // Act
        using var response = await fixture.Client.PostAsync(route, null, cancellationToken);
        var draftCount = await fixture.GetDocumentDraftCountForBooking(booking.Id, cancellationToken);
        var audits = await fixture.GetDocumentAuditMetadataForBooking(booking.Id, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        replacement.Revision.ShouldBe(2);
        draftCount.ShouldBe(2);
        var regenerationAudits = audits.Where(audit => audit.Operation == "Regenerate").ToArray();
        regenerationAudits.ShouldHaveCount(2);
        regenerationAudits.ShouldHaveSingleItem(audit => audit.Outcome == "Succeeded")
            .DocumentRevision.ShouldBe(2);
        regenerationAudits.ShouldHaveSingleItem(audit => audit.Outcome == "Rejected")
            .ReasonCode.ShouldBe("StateConflict");
    }

    [Fact]
    public async Task Contract_draft_regeneration_rejects_a_booking_cancelled_during_persistence()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var tour = await fixture.Client.CreateTestTour("document-regeneration-race", "Document regeneration race", cancellationToken);
        var customer = await fixture.Client.CreateTestCustomer("Document", "Regeneration race", cancellationToken);
        var booking = await fixture.Client.CreateTestBooking(tour.Id, customer.Id, null, cancellationToken);
        using var confirmResponse = await fixture.Client.ConfirmBooking(booking.Id, cancellationToken);
        confirmResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var documents = new DocumentsApiClient(fixture.Client);
        var document = await documents.GenerateContractDraft(booking.Id, cancellationToken);
        await using var scenario = await fixture.CreateBookingCancellationAtDocumentPersistenceScenario(booking.Id, cancellationToken);
        await scenario.HoldCancellation(cancellationToken);

        // Act
        var responseTask = fixture.Client.PostAsync(
            new Uri($"/api/v1/documents/{document.Id}/regenerate", UriKind.Relative),
            null,
            cancellationToken);
        await scenario.WaitForBlockedDocumentPersistence(1, cancellationToken);
        await scenario.CommitCancellation(cancellationToken);
        using var response = await responseTask;
        var draftCount = await fixture.GetDocumentDraftCountForBooking(booking.Id, cancellationToken);
        var audits = await fixture.GetDocumentAuditMetadataForBooking(booking.Id, cancellationToken);
        var bookingStatus = await scenario.GetBookingStatus(cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        draftCount.ShouldBe(1);
        var audit = audits.ShouldHaveSingleItem(audit => audit.Operation == "Regenerate");
        audit.Outcome.ShouldBe("Rejected");
        audit.ReasonCode.ShouldBe("StateConflict");
        audit.BookingId.ShouldBe(booking.Id);
        audit.DocumentId.ShouldBe(document.Id);
        audit.DocumentRevision.ShouldBe(1);
        bookingStatus.ShouldBe("Cancelled");
    }

    [Fact]
    public async Task Regeneration_and_void_preserve_revision_lineage_and_audit_reason_codes()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var tour = await fixture.Client.CreateTestTour("document-audit-lineage", "Document audit lineage", cancellationToken);
        var customer = await fixture.Client.CreateTestCustomer("Document", "Audit lineage", cancellationToken);
        var booking = await fixture.Client.CreateTestBooking(tour.Id, customer.Id, null, cancellationToken);
        using var confirmResponse = await fixture.Client.ConfirmBooking(booking.Id, cancellationToken);
        confirmResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var documents = new DocumentsApiClient(fixture.Client);
        var generated = await documents.GenerateContractDraft(booking.Id, cancellationToken);
        var editableField = generated.Fields.First(field => field.IsEditable);

        // Act
        var updated = await documents.UpdateField(
            generated.Id,
            editableField.FieldId,
            new UpdateDocumentFieldDto { Value = "Reviewed customer-facing value" },
            cancellationToken);
        var approved = await documents.Approve(generated.Id, cancellationToken);
        var finalized = await documents.Finalize(generated.Id, cancellationToken);
        var replacement = await documents.Regenerate(generated.Id, cancellationToken);
        var replacementInReview = await documents.BeginReview(replacement.Id, cancellationToken);
        var replacementApproved = await documents.Approve(replacement.Id, cancellationToken);
        var replacementFinalized = await documents.Finalize(replacement.Id, cancellationToken);
        var voided = await documents.Void(replacement.Id, cancellationToken);
        var superseded = await documents.GetDocumentById(generated.Id, cancellationToken);
        var originalAudits = await fixture.GetDocumentAuditMetadata(generated.Id, cancellationToken);
        var replacementAudits = await fixture.GetDocumentAuditMetadata(replacement.Id, cancellationToken);

        // Assert
        updated.Status.ShouldBe(DocumentStatusDto.InReview);
        approved.Status.ShouldBe(DocumentStatusDto.Approved);
        finalized.Status.ShouldBe(DocumentStatusDto.Finalized);
        replacement.Revision.ShouldBe(2);
        replacement.ReplacesDocumentId.ShouldBe(generated.Id);
        replacementInReview.Status.ShouldBe(DocumentStatusDto.InReview);
        replacementApproved.Status.ShouldBe(DocumentStatusDto.Approved);
        replacementFinalized.Status.ShouldBe(DocumentStatusDto.Finalized);
        voided.Status.ShouldBe(DocumentStatusDto.Voided);
        superseded.ShouldNotBeNull().Status.ShouldBe(DocumentStatusDto.Superseded);

        originalAudits.Select(audit => audit.Operation).ShouldBe(
            ["Generate", "UpdateField", "Approve", "Finalize", "Read"]);
        replacementAudits.Select(audit => audit.Operation).ShouldBe(
            ["Regenerate", "BeginReview", "Approve", "Finalize", "Void"]);
        replacementAudits.Select(audit => audit.ReasonCode).ShouldBe(
            ["ManualRegeneration", "ManualOperation", "ManualOperation", "ManualFinalize", "ManualVoid"]);

        foreach (var audit in replacementAudits)
        {
            audit.DocumentId.ShouldBe(replacement.Id);
            audit.BookingId.ShouldBe(booking.Id);
            audit.DocumentRevision.ShouldBe(2);
            audit.Outcome.ShouldBe("Succeeded");
            audit.RetentionExpiresAt.ShouldBe(audit.OccurredAtUtc.AddMonths(24));
        }
    }

    [Fact]
    public async Task Authenticated_admin_can_complete_the_contract_document_lifecycle_and_download_the_finalized_artifact()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var tour = await fixture.Client.CreateTestTour("document-lifecycle", "Document lifecycle", cancellationToken);
        var customer = await fixture.Client.CreateTestCustomer("Document", "Lifecycle", cancellationToken);
        var booking = await fixture.Client.CreateTestBooking(tour.Id, customer.Id, null, cancellationToken);
        using var confirmResponse = await fixture.Client.ConfirmBooking(booking.Id, cancellationToken);
        confirmResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var documents = new DocumentsApiClient(fixture.Client);

        // Act
        var generated = await documents.GenerateContractDraft(booking.Id, cancellationToken);
        var inReview = await documents.BeginReview(generated.Id, cancellationToken);
        var changesRequested = await documents.RequestChanges(generated.Id, cancellationToken);
        var reviewedAgain = await documents.BeginReview(generated.Id, cancellationToken);
        var approved = await documents.Approve(generated.Id, cancellationToken);
        var finalized = await documents.Finalize(generated.Id, cancellationToken);
        using var artifactResponse = await fixture.Client.GetAsync(
            new Uri($"/api/v1/documents/{generated.Id}/download", UriKind.Relative),
            cancellationToken);
        var artifactContent = await artifactResponse.Content.ReadAsStringAsync(cancellationToken);
        var audits = await fixture.GetDocumentAuditMetadata(generated.Id, cancellationToken);

        // Assert
        generated.Status.ShouldBe(DocumentStatusDto.DraftGenerated);
        inReview.Status.ShouldBe(DocumentStatusDto.InReview);
        changesRequested.Status.ShouldBe(DocumentStatusDto.ChangesRequested);
        reviewedAgain.Status.ShouldBe(DocumentStatusDto.InReview);
        approved.Status.ShouldBe(DocumentStatusDto.Approved);
        finalized.Status.ShouldBe(DocumentStatusDto.Finalized);
        finalized.HasFinalizedArtifact.ShouldBeTrue();
        finalized.FinalizedAt.ShouldNotBeNull();

        artifactResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        artifactResponse.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        artifactResponse.Headers.Pragma.Select(value => value.Name).ShouldContain("no-cache");
        artifactResponse.Content.Headers.GetValues("Expires").ShouldContain("0");
        artifactResponse.Headers.GetValues("X-Content-Type-Options").ShouldContain("nosniff");
        artifactResponse.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("text/html");
        var contentDisposition = artifactResponse.Content.Headers.ContentDisposition.ShouldNotBeNull();
        (contentDisposition.FileNameStar ?? contentDisposition.FileName?.Trim('"')).ShouldBe($"document-{generated.Id:N}-r1.html");
        string.IsNullOrWhiteSpace(artifactContent).ShouldBeFalse();

        audits.Select(audit => audit.Operation).ShouldBe(
        [
            "Generate",
            "BeginReview",
            "RequestChanges",
            "BeginReview",
            "Approve",
            "Finalize",
            "Download"
        ]);
        audits.Select(audit => audit.Outcome).ShouldBe(
        [
            "Succeeded",
            "Succeeded",
            "Succeeded",
            "Succeeded",
            "Succeeded",
            "Succeeded",
            "Succeeded"
        ]);
        audits.Select(audit => audit.ReasonCode).ShouldBe(
        [
            "ManualOperation",
            "ManualOperation",
            "ManualOperation",
            "ManualOperation",
            "ManualOperation",
            "ManualFinalize",
            "None"
        ]);

        foreach (var audit in audits)
        {
            audit.DocumentId.ShouldBe(generated.Id);
            audit.BookingId.ShouldBe(booking.Id);
            audit.DocumentRevision.ShouldBe(1);
            audit.ActorId.ShouldBe(KeycloakConformanceClient.ConformanceUserId);
            string.IsNullOrWhiteSpace(audit.CorrelationId).ShouldBeFalse();
            audit.RetentionExpiresAt.ShouldBe(audit.OccurredAtUtc.AddMonths(24));
        }
    }
}
