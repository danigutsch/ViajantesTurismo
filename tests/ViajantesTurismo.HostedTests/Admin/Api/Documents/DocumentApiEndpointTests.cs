using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Contracts.Http;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;
using ViajantesTurismo.Admin.Testing;
using ViajantesTurismo.Admin.Testing.Integration.Helpers;
using System.Text.Json;
using PandaBear.Testing;

namespace ViajantesTurismo.Admin.IntegrationTests.Documents;

[Trait(TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(SharedKernelTestTraitNames.CapabilityName, AdminTestTraitValues.GeneratedDocumentsCapability)]
[TestScope(TestScope.Integration)]
public sealed class DocumentApiEndpointTests(ApiFixture fixture) : IClassFixture<ApiFixture>
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
        using var response = await DocumentApiTestSetup.PostIdempotentCommand(
            fixture.Client,
            new Uri($"/api/v1/documents/bookings/{booking.Id}/contract-drafts", UriKind.Relative),
            Guid.CreateVersion7().ToString("N"),
            cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var actualValue = await response.Content.ReadFromJsonAsync<GetDocumentDto>(cancellationToken);
        var document = actualValue.ShouldNotBeNull();
        document.BookingId.ShouldBe(booking.Id);
    }

    [Fact]
    public async Task Successful_document_routes_return_standard_no_store_headers()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var tour = await fixture.Client.CreateTestTour("document-no-store", "Document no store", cancellationToken);
        var customer = await fixture.Client.CreateTestCustomer("Document", "No store", cancellationToken);
        var booking = await fixture.Client.CreateTestBooking(tour.Id, customer.Id, null, cancellationToken);
        using var confirmResponse = await fixture.Client.ConfirmBooking(booking.Id, cancellationToken);
        confirmResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Act
        using var generateResponse = await DocumentApiTestSetup.PostIdempotentCommand(
            fixture.Client,
            new Uri($"/api/v1/documents/bookings/{booking.Id}/contract-drafts", UriKind.Relative),
            Guid.CreateVersion7().ToString("N"),
            cancellationToken);
        var generated = await generateResponse.Content.ReadFromJsonAsync<GetDocumentDto>(cancellationToken)
            ?? throw new InvalidOperationException("Document generation did not return a document.");
        using var getResponse = await fixture.Client.GetAsync(
            new Uri($"/api/v1/documents/{generated.Id}", UriKind.Relative),
            cancellationToken);
        using var updateResponse = await fixture.Client.PatchAsJsonAsync(
            new Uri($"/api/v1/documents/{generated.Id}/fields/greeting", UriKind.Relative),
            new UpdateDocumentFieldDto { Value = "No-store response value" },
            cancellationToken);
        using var approveResponse = await fixture.Client.PostAsync(
            new Uri($"/api/v1/documents/{generated.Id}/approve", UriKind.Relative),
            null,
            cancellationToken);
        using var finalizeResponse = await fixture.Client.PostAsync(
            new Uri($"/api/v1/documents/{generated.Id}/finalize", UriKind.Relative),
            null,
            cancellationToken);
        using var downloadResponse = await fixture.Client.GetAsync(
            new Uri($"/api/v1/documents/{generated.Id}/download", UriKind.Relative),
            cancellationToken);
        using var regenerateResponse = await DocumentApiTestSetup.PostIdempotentCommand(
            fixture.Client,
            new Uri($"/api/v1/documents/{generated.Id}/regenerate", UriKind.Relative),
            Guid.CreateVersion7().ToString("N"),
            cancellationToken);
        var replacement = await regenerateResponse.Content.ReadFromJsonAsync<GetDocumentDto>(cancellationToken)
            ?? throw new InvalidOperationException("Document regeneration did not return a document.");
        using var beginReviewResponse = await fixture.Client.PostAsync(
            new Uri($"/api/v1/documents/{replacement.Id}/review", UriKind.Relative),
            null,
            cancellationToken);
        using var requestChangesResponse = await fixture.Client.PostAsync(
            new Uri($"/api/v1/documents/{replacement.Id}/changes-requested", UriKind.Relative),
            null,
            cancellationToken);
        using var resumeReviewResponse = await fixture.Client.PostAsync(
            new Uri($"/api/v1/documents/{replacement.Id}/review", UriKind.Relative),
            null,
            cancellationToken);
        using var replacementApproveResponse = await fixture.Client.PostAsync(
            new Uri($"/api/v1/documents/{replacement.Id}/approve", UriKind.Relative),
            null,
            cancellationToken);
        using var replacementFinalizeResponse = await fixture.Client.PostAsync(
            new Uri($"/api/v1/documents/{replacement.Id}/finalize", UriKind.Relative),
            null,
            cancellationToken);
        using var voidResponse = await fixture.Client.PostAsync(
            new Uri($"/api/v1/documents/{replacement.Id}/void", UriKind.Relative),
            null,
            cancellationToken);
        var responses = new[]
        {
            generateResponse,
            getResponse,
            updateResponse,
            approveResponse,
            finalizeResponse,
            downloadResponse,
            regenerateResponse,
            beginReviewResponse,
            requestChangesResponse,
            resumeReviewResponse,
            replacementApproveResponse,
            replacementFinalizeResponse,
            voidResponse,
        };
        var coveredRoutes = responses
            .Select(response =>
            {
                var request = response.RequestMessage
                    ?? throw new InvalidOperationException("A document response did not retain its request metadata.");
                return (
                    request.Method.Method,
                    DocumentApiRouteCases.Normalize(
                        request.RequestUri?.AbsolutePath ?? string.Empty,
                        booking.Id,
                        generated.Id,
                        replacement.Id));
            })
            .Distinct()
            .OrderBy(route => route.Item2, StringComparer.Ordinal)
            .ThenBy(route => route.Method, StringComparer.Ordinal)
            .ToArray();
        var expectedRoutes = DocumentApiRouteCases.Expected
            .OrderBy(route => route.Route, StringComparer.Ordinal)
            .ThenBy(route => route.Method, StringComparer.Ordinal)
            .ToArray();

        // Assert
        coveredRoutes.ShouldBe(expectedRoutes);
        responses.All(response => response.IsSuccessStatusCode).ShouldBeTrue();
        responses.All(DocumentNoStoreHeaders.ArePresent).ShouldBeTrue();
    }

    [Fact]
    public async Task Contract_draft_generation_replays_a_completed_idempotency_key_without_duplicate_state_or_audit()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var tour = await fixture.Client.CreateTestTour("document-generation-replay", "Document generation replay", cancellationToken);
        var customer = await fixture.Client.CreateTestCustomer("Document", "Generation replay", cancellationToken);
        var booking = await fixture.Client.CreateTestBooking(tour.Id, customer.Id, null, cancellationToken);
        using var confirmResponse = await fixture.Client.ConfirmBooking(booking.Id, cancellationToken);
        confirmResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var route = new Uri($"/api/v1/documents/bookings/{booking.Id}/contract-drafts", UriKind.Relative);
        var idempotencyKey = Guid.CreateVersion7().ToString("N");
        using var firstRequest = new HttpRequestMessage(HttpMethod.Post, route);
        using var replayRequest = new HttpRequestMessage(HttpMethod.Post, route);
        firstRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        replayRequest.Headers.Add("Idempotency-Key", idempotencyKey);

        // Act
        using var firstResponse = await fixture.Client.SendAsync(firstRequest, cancellationToken);
        using var cancelResponse = await fixture.Client.CancelBooking(booking.Id, cancellationToken);
        using var replayResponse = await fixture.Client.SendAsync(replayRequest, cancellationToken);
        var draftCount = await fixture.GetDocumentDraftCountForBooking(booking.Id, cancellationToken);
        var audits = await fixture.GetDocumentAuditMetadataForBooking(booking.Id, cancellationToken);

        // Assert
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        cancelResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        replayResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var actualValue1 = await firstResponse.Content.ReadFromJsonAsync<GetDocumentDto>(cancellationToken);
        var firstDocument = actualValue1.ShouldNotBeNull();
        var actualValue2 = await replayResponse.Content.ReadFromJsonAsync<GetDocumentDto>(cancellationToken);
        var replayedDocument = actualValue2.ShouldNotBeNull();
        replayedDocument.Id.ShouldBe(firstDocument.Id);
        draftCount.ShouldBe(1);
        var generationAudit = audits.ShouldHaveSingleItem(audit => audit.Operation == "Generate");
        generationAudit.Outcome.ShouldBe("Succeeded");
        generationAudit.DocumentId.ShouldBe(firstDocument.Id);
    }

    [Fact]
    public async Task Contract_draft_generation_scopes_the_same_idempotency_key_to_each_booking()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var firstTour = await fixture.Client.CreateTestTour("document-key-scope-1", "Document key scope 1", cancellationToken);
        var secondTour = await fixture.Client.CreateTestTour("document-key-scope-2", "Document key scope 2", cancellationToken);
        var firstCustomer = await fixture.Client.CreateTestCustomer("Document", "Key scope 1", cancellationToken);
        var secondCustomer = await fixture.Client.CreateTestCustomer("Document", "Key scope 2", cancellationToken);
        var firstBooking = await fixture.Client.CreateTestBooking(firstTour.Id, firstCustomer.Id, null, cancellationToken);
        var secondBooking = await fixture.Client.CreateTestBooking(secondTour.Id, secondCustomer.Id, null, cancellationToken);
        using var firstConfirmResponse = await fixture.Client.ConfirmBooking(firstBooking.Id, cancellationToken);
        using var secondConfirmResponse = await fixture.Client.ConfirmBooking(secondBooking.Id, cancellationToken);
        firstConfirmResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        secondConfirmResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var idempotencyKey = Guid.CreateVersion7().ToString("N");
        using var firstRequest = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri($"/api/v1/documents/bookings/{firstBooking.Id}/contract-drafts", UriKind.Relative));
        using var secondRequest = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri($"/api/v1/documents/bookings/{secondBooking.Id}/contract-drafts", UriKind.Relative));
        firstRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        secondRequest.Headers.Add("Idempotency-Key", idempotencyKey);

        // Act
        using var firstResponse = await fixture.Client.SendAsync(firstRequest, cancellationToken);
        using var secondResponse = await fixture.Client.SendAsync(secondRequest, cancellationToken);

        // Assert
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        secondResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var actualValue3 = await firstResponse.Content.ReadFromJsonAsync<GetDocumentDto>(cancellationToken);
        var firstDocument = actualValue3.ShouldNotBeNull();
        var actualValue4 = await secondResponse.Content.ReadFromJsonAsync<GetDocumentDto>(cancellationToken);
        var secondDocument = actualValue4.ShouldNotBeNull();
        secondDocument.Id.ShouldNotBe(firstDocument.Id);
        firstDocument.BookingId.ShouldBe(firstBooking.Id);
        secondDocument.BookingId.ShouldBe(secondBooking.Id);
    }

    [Fact]
    public async Task Contract_draft_generation_rejects_an_invalid_idempotency_key_before_persistence()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var tour = await fixture.Client.CreateTestTour("document-generation-invalid-key", "Document generation invalid key", cancellationToken);
        var customer = await fixture.Client.CreateTestCustomer("Document", "Generation invalid key", cancellationToken);
        var booking = await fixture.Client.CreateTestBooking(tour.Id, customer.Id, null, cancellationToken);
        using var confirmResponse = await fixture.Client.ConfirmBooking(booking.Id, cancellationToken);
        confirmResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri($"/api/v1/documents/bookings/{booking.Id}/contract-drafts", UriKind.Relative));
        _ = request.Headers.TryAddWithoutValidation("Idempotency-Key", "invalid key");

        // Act
        using var response = await fixture.Client.SendAsync(request, cancellationToken);
        var draftCount = await fixture.GetDocumentDraftCountForBooking(booking.Id, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        draftCount.ShouldBe(0);
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
        var generated = await documents.GenerateContractDraft(booking.Id, Guid.CreateVersion7().ToString("N"), cancellationToken).RequireSucceededValue();
        var reloaded = await documents.GetDocumentById(generated.Id, cancellationToken).RequireSucceededValue();

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
        var document = await documents.GenerateContractDraft(booking.Id, Guid.CreateVersion7().ToString("N"), cancellationToken).RequireSucceededValue();
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
        using var response = await DocumentApiTestSetup.PostIdempotentCommand(
            fixture.Client,
            new Uri($"/api/v1/documents/bookings/{booking.Id}/contract-drafts", UriKind.Relative),
            Guid.CreateVersion7().ToString("N"),
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
        var responseTask = DocumentApiTestSetup.PostIdempotentCommand(
            fixture.Client,
            new Uri($"/api/v1/documents/bookings/{booking.Id}/contract-drafts", UriKind.Relative),
            Guid.CreateVersion7().ToString("N"),
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
        var document = await documents.GenerateContractDraft(booking.Id, Guid.CreateVersion7().ToString("N"), cancellationToken).RequireSucceededValue();
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
        var document = await documents.GenerateContractDraft(booking.Id, Guid.CreateVersion7().ToString("N"), cancellationToken).RequireSucceededValue();

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
        using var response = await DocumentApiTestSetup.PostIdempotentCommand(
            fixture.Client,
            new Uri($"/api/v1/documents/bookings/{booking.Id}/contract-drafts", UriKind.Relative),
            Guid.CreateVersion7().ToString("N"),
            cancellationToken);
        var draftCount = await fixture.GetDocumentDraftCountForBooking(booking.Id, cancellationToken);
        var audits = await fixture.GetDocumentAuditMetadataForBooking(booking.Id, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        draftCount.ShouldBe(0);
        audits.ShouldBeEmpty();
    }

    [Fact]
    public async Task Contract_draft_generation_completion_failure_rolls_back_state_and_audit_but_keeps_started_key()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var tour = await fixture.Client.CreateTestTour("document-generation-completion-failure", "Document generation completion failure", cancellationToken);
        var customer = await fixture.Client.CreateTestCustomer("Document", "Generation completion failure", cancellationToken);
        var booking = await fixture.Client.CreateTestBooking(tour.Id, customer.Id, null, cancellationToken);
        using var confirmResponse = await fixture.Client.ConfirmBooking(booking.Id, cancellationToken);
        confirmResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var idempotencyKey = Guid.CreateVersion7();
        var idempotencyScope = $"admin.documents.generate-contract-draft:{booking.Id:N}";
        HttpStatusCode responseStatus;
        string? idempotencyState;
        await using (var scenario = await fixture.CreateDocumentIdempotencyCompletionFailureScenario(
            idempotencyScope,
            idempotencyKey,
            cancellationToken))
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                new Uri($"/api/v1/documents/bookings/{booking.Id}/contract-drafts", UriKind.Relative));
            request.Headers.Add("Idempotency-Key", idempotencyKey.ToString("N"));

            // Act
            using var response = await fixture.Client.SendAsync(request, cancellationToken);
            responseStatus = response.StatusCode;
            idempotencyState = await scenario.GetIdempotencyState(cancellationToken);
        }

        var draftCount = await fixture.GetDocumentDraftCountForBooking(booking.Id, cancellationToken);
        var audits = await fixture.GetDocumentAuditMetadataForBooking(booking.Id, cancellationToken);

        // Assert
        responseStatus.ShouldBe(HttpStatusCode.InternalServerError);
        idempotencyState.ShouldBe("Started");
        draftCount.ShouldBe(0);
        audits.ShouldNotContain(audit => audit.Operation == "Generate");
    }

    [Fact]
    public async Task Contract_draft_regeneration_completion_failure_rolls_back_replacement_and_audit_but_keeps_started_key()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var tour = await fixture.Client.CreateTestTour("document-regeneration-completion-failure", "Document regeneration completion failure", cancellationToken);
        var customer = await fixture.Client.CreateTestCustomer("Document", "Regeneration completion failure", cancellationToken);
        var booking = await fixture.Client.CreateTestBooking(tour.Id, customer.Id, null, cancellationToken);
        using var confirmResponse = await fixture.Client.ConfirmBooking(booking.Id, cancellationToken);
        confirmResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var documents = new DocumentsApiClient(fixture.Client);
        var generated = await documents.GenerateContractDraft(booking.Id, Guid.CreateVersion7().ToString("N"), cancellationToken).RequireSucceededValue();
        var idempotencyKey = Guid.CreateVersion7();
        var idempotencyScope = $"admin.documents.regenerate-draft:{generated.Id:N}";
        HttpStatusCode responseStatus;
        string? idempotencyState;
        await using (var scenario = await fixture.CreateDocumentIdempotencyCompletionFailureScenario(
            idempotencyScope,
            idempotencyKey,
            cancellationToken))
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                new Uri($"/api/v1/documents/{generated.Id}/regenerate", UriKind.Relative));
            request.Headers.Add("Idempotency-Key", idempotencyKey.ToString("N"));

            // Act
            using var response = await fixture.Client.SendAsync(request, cancellationToken);
            responseStatus = response.StatusCode;
            idempotencyState = await scenario.GetIdempotencyState(cancellationToken);
        }

        var draftCount = await fixture.GetDocumentDraftCountForBooking(booking.Id, cancellationToken);
        var audits = await fixture.GetDocumentAuditMetadataForBooking(booking.Id, cancellationToken);

        // Assert
        responseStatus.ShouldBe(HttpStatusCode.InternalServerError);
        idempotencyState.ShouldBe("Started");
        draftCount.ShouldBe(1);
        audits.ShouldNotContain(audit => audit.Operation == "Regenerate");
    }

    [Fact]
    public async Task Contract_draft_generation_retryable_completion_failure_persists_one_revision_audit_and_completed_key()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var tour = await fixture.Client.CreateTestTour("document-generation-completion-retry", "Document generation completion retry", cancellationToken);
        var customer = await fixture.Client.CreateTestCustomer("Document", "Generation completion retry", cancellationToken);
        var booking = await fixture.Client.CreateTestBooking(tour.Id, customer.Id, null, cancellationToken);
        using var confirmResponse = await fixture.Client.ConfirmBooking(booking.Id, cancellationToken);
        confirmResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var idempotencyKey = Guid.CreateVersion7();
        var idempotencyScope = $"admin.documents.generate-contract-draft:{booking.Id:N}";
        HttpStatusCode responseStatus;
        string? idempotencyState;
        await using (var scenario = await fixture.CreateDocumentIdempotencyTransientCompletionFailureScenario(
            idempotencyScope,
            idempotencyKey,
            cancellationToken))
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                new Uri($"/api/v1/documents/bookings/{booking.Id}/contract-drafts", UriKind.Relative));
            request.Headers.Add("Idempotency-Key", idempotencyKey.ToString("N"));

            // Act
            using var response = await fixture.Client.SendAsync(request, cancellationToken);
            responseStatus = response.StatusCode;
            idempotencyState = await scenario.GetIdempotencyState(cancellationToken);
        }

        var draftCount = await fixture.GetDocumentDraftCountForBooking(booking.Id, cancellationToken);
        var audits = await fixture.GetDocumentAuditMetadataForBooking(booking.Id, cancellationToken);

        // Assert
        responseStatus.ShouldBe(HttpStatusCode.Created);
        idempotencyState.ShouldBe("Completed");
        draftCount.ShouldBe(1);
        var audit = audits.ShouldHaveSingleItem(item => item.Operation == "Generate");
        audit.Outcome.ShouldBe("Succeeded");
    }

    [Fact]
    public async Task Contract_draft_regeneration_retryable_completion_failure_persists_one_replacement_audit_and_completed_key()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var tour = await fixture.Client.CreateTestTour("document-regeneration-completion-retry", "Document regeneration completion retry", cancellationToken);
        var customer = await fixture.Client.CreateTestCustomer("Document", "Regeneration completion retry", cancellationToken);
        var booking = await fixture.Client.CreateTestBooking(tour.Id, customer.Id, null, cancellationToken);
        using var confirmResponse = await fixture.Client.ConfirmBooking(booking.Id, cancellationToken);
        confirmResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var documents = new DocumentsApiClient(fixture.Client);
        var generated = await documents.GenerateContractDraft(booking.Id, Guid.CreateVersion7().ToString("N"), cancellationToken).RequireSucceededValue();
        var idempotencyKey = Guid.CreateVersion7();
        var idempotencyScope = $"admin.documents.regenerate-draft:{generated.Id:N}";
        HttpStatusCode responseStatus;
        string? idempotencyState;
        await using (var scenario = await fixture.CreateDocumentIdempotencyTransientCompletionFailureScenario(
            idempotencyScope,
            idempotencyKey,
            cancellationToken))
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                new Uri($"/api/v1/documents/{generated.Id}/regenerate", UriKind.Relative));
            request.Headers.Add("Idempotency-Key", idempotencyKey.ToString("N"));

            // Act
            using var response = await fixture.Client.SendAsync(request, cancellationToken);
            responseStatus = response.StatusCode;
            idempotencyState = await scenario.GetIdempotencyState(cancellationToken);
        }

        var draftCount = await fixture.GetDocumentDraftCountForBooking(booking.Id, cancellationToken);
        var audits = await fixture.GetDocumentAuditMetadataForBooking(booking.Id, cancellationToken);

        // Assert
        responseStatus.ShouldBe(HttpStatusCode.OK);
        idempotencyState.ShouldBe("Completed");
        draftCount.ShouldBe(2);
        var audit = audits.ShouldHaveSingleItem(item => item.Operation == "Regenerate");
        audit.Outcome.ShouldBe("Succeeded");
        audit.DocumentRevision.ShouldBe(2);
    }

    [Fact]
    public async Task Concurrent_contract_generation_persists_one_success_and_one_rejection_without_duplicate_state()
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
        var firstRequest = DocumentApiTestSetup.PostIdempotentCommand(fixture.Client, route, Guid.CreateVersion7().ToString("N"), cancellationToken);
        await scenario.WaitForBlockedDocumentPersistence(1, cancellationToken);
        var secondRequest = DocumentApiTestSetup.PostIdempotentCommand(fixture.Client, route, Guid.CreateVersion7().ToString("N"), cancellationToken);
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
        var successfulAudit = generationAudits.ShouldHaveSingleItem(audit => audit.Outcome == "Succeeded");
        successfulAudit.ReasonCode.ShouldBe("ManualOperation");
        var rejectedAudit = generationAudits.ShouldHaveSingleItem(audit => audit.Outcome == "Rejected");
        rejectedAudit.ReasonCode.ShouldBe("StateConflict");
    }

    [Fact]
    public async Task Concurrent_regeneration_persists_one_success_and_one_rejection_without_a_sibling_revision()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var tour = await fixture.Client.CreateTestTour("document-regeneration-race", "Document regeneration race", cancellationToken);
        var customer = await fixture.Client.CreateTestCustomer("Document", "Regeneration race", cancellationToken);
        var booking = await fixture.Client.CreateTestBooking(tour.Id, customer.Id, null, cancellationToken);
        using var confirmResponse = await fixture.Client.ConfirmBooking(booking.Id, cancellationToken);
        confirmResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var documents = new DocumentsApiClient(fixture.Client);
        var generated = await documents.GenerateContractDraft(booking.Id, Guid.CreateVersion7().ToString("N"), cancellationToken).RequireSucceededValue();
        var route = new Uri($"/api/v1/documents/{generated.Id}/regenerate", UriKind.Relative);
        await using var scenario = await fixture.CreateBookingCancellationAtDocumentPersistenceScenario(
            booking.Id,
            cancellationToken);
        await scenario.HoldCancellation(cancellationToken);

        // Act
        var firstRequest = DocumentApiTestSetup.PostIdempotentCommand(fixture.Client, route, Guid.CreateVersion7().ToString("N"), cancellationToken);
        await scenario.WaitForBlockedDocumentPersistence(1, cancellationToken);
        var secondRequest = DocumentApiTestSetup.PostIdempotentCommand(fixture.Client, route, Guid.CreateVersion7().ToString("N"), cancellationToken);
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
        var responseStatuses = new[] { firstResponse.StatusCode, secondResponse.StatusCode };
        responseStatuses.ShouldHaveSingleItem(status => status == HttpStatusCode.OK);
        responseStatuses.ShouldHaveSingleItem(status => status == HttpStatusCode.Conflict);
        bookingStatus.ShouldBe("Confirmed");
        draftCount.ShouldBe(2);
        var regenerationAudits = audits.Where(audit => audit.Operation == "Regenerate").ToArray();
        var successfulAudit = regenerationAudits.ShouldHaveSingleItem(audit => audit.Outcome == "Succeeded");
        successfulAudit.DocumentRevision.ShouldBe(2);
        successfulAudit.ReasonCode.ShouldBe("ManualRegeneration");
        var rejectedAudit = regenerationAudits.ShouldHaveSingleItem(audit => audit.Outcome == "Rejected");
        rejectedAudit.DocumentRevision.ShouldBe(1);
        rejectedAudit.ReasonCode.ShouldBe("StateConflict");
    }

    [Fact]
    public async Task Contract_draft_regeneration_replays_a_completed_idempotency_key_without_a_sibling_revision_or_duplicate_audit()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var tour = await fixture.Client.CreateTestTour("document-regeneration-replay", "Document regeneration replay", cancellationToken);
        var customer = await fixture.Client.CreateTestCustomer("Document", "Regeneration replay", cancellationToken);
        var booking = await fixture.Client.CreateTestBooking(tour.Id, customer.Id, null, cancellationToken);
        using var confirmResponse = await fixture.Client.ConfirmBooking(booking.Id, cancellationToken);
        confirmResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var documents = new DocumentsApiClient(fixture.Client);
        var generated = await documents.GenerateContractDraft(booking.Id, Guid.CreateVersion7().ToString("N"), cancellationToken).RequireSucceededValue();
        var route = new Uri($"/api/v1/documents/{generated.Id}/regenerate", UriKind.Relative);
        var idempotencyKey = Guid.CreateVersion7().ToString("N");
        using var firstRequest = new HttpRequestMessage(HttpMethod.Post, route);
        using var replayRequest = new HttpRequestMessage(HttpMethod.Post, route);
        firstRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        replayRequest.Headers.Add("Idempotency-Key", idempotencyKey);

        // Act
        using var firstResponse = await fixture.Client.SendAsync(firstRequest, cancellationToken);
        using var cancelResponse = await fixture.Client.CancelBooking(booking.Id, cancellationToken);
        using var replayResponse = await fixture.Client.SendAsync(replayRequest, cancellationToken);
        var draftCount = await fixture.GetDocumentDraftCountForBooking(booking.Id, cancellationToken);
        var audits = await fixture.GetDocumentAuditMetadataForBooking(booking.Id, cancellationToken);

        // Assert
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        cancelResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        replayResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var actualValue5 = await firstResponse.Content.ReadFromJsonAsync<GetDocumentDto>(cancellationToken);
        var firstReplacement = actualValue5.ShouldNotBeNull();
        var actualValue6 = await replayResponse.Content.ReadFromJsonAsync<GetDocumentDto>(cancellationToken);
        var replayedReplacement = actualValue6.ShouldNotBeNull();
        replayedReplacement.Id.ShouldBe(firstReplacement.Id);
        firstReplacement.Revision.ShouldBe(2);
        draftCount.ShouldBe(2);
        var regenerationAudit = audits.ShouldHaveSingleItem(audit => audit.Operation == "Regenerate");
        regenerationAudit.Outcome.ShouldBe("Succeeded");
        regenerationAudit.DocumentId.ShouldBe(firstReplacement.Id);
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
        var document = await documents.GenerateContractDraft(booking.Id, Guid.CreateVersion7().ToString("N"), cancellationToken).RequireSucceededValue();
        await using var scenario = await fixture.CreateBookingCancellationAtDocumentPersistenceScenario(booking.Id, cancellationToken);
        await scenario.HoldCancellation(cancellationToken);

        // Act
        var responseTask = DocumentApiTestSetup.PostIdempotentCommand(
            fixture.Client,
            new Uri($"/api/v1/documents/{document.Id}/regenerate", UriKind.Relative),
            Guid.CreateVersion7().ToString("N"),
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
        var generated = await documents.GenerateContractDraft(booking.Id, Guid.CreateVersion7().ToString("N"), cancellationToken).RequireSucceededValue();
        var editableField = generated.Fields.First(field => field.IsEditable);

        // Act
        var updated = await documents.UpdateField(
            generated.Id,
            editableField.FieldId,
            new UpdateDocumentFieldDto { Value = "Reviewed customer-facing value" },
            cancellationToken).RequireSucceededValue();
        var approved = await documents.Approve(generated.Id, cancellationToken).RequireSucceededValue();
        var finalized = await documents.Finalize(generated.Id, cancellationToken).RequireSucceededValue();
        var replacement = await documents.Regenerate(generated.Id, Guid.CreateVersion7().ToString("N"), cancellationToken).RequireSucceededValue();
        var replacementInReview = await documents.BeginReview(replacement.Id, cancellationToken).RequireSucceededValue();
        var replacementApproved = await documents.Approve(replacement.Id, cancellationToken).RequireSucceededValue();
        var replacementFinalized = await documents.Finalize(replacement.Id, cancellationToken).RequireSucceededValue();
        var voided = await documents.Void(replacement.Id, cancellationToken).RequireSucceededValue();
        var superseded = await documents.GetDocumentById(generated.Id, cancellationToken).RequireSucceededValue();
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
        var generated = await documents.GenerateContractDraft(booking.Id, Guid.CreateVersion7().ToString("N"), cancellationToken).RequireSucceededValue();
        var inReview = await documents.BeginReview(generated.Id, cancellationToken).RequireSucceededValue();
        var changesRequested = await documents.RequestChanges(generated.Id, cancellationToken).RequireSucceededValue();
        var reviewedAgain = await documents.BeginReview(generated.Id, cancellationToken).RequireSucceededValue();
        var approved = await documents.Approve(generated.Id, cancellationToken).RequireSucceededValue();
        var finalized = await documents.Finalize(generated.Id, cancellationToken).RequireSucceededValue();
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
        DocumentNoStoreHeaders.ArePresent(artifactResponse).ShouldBeTrue();
        artifactResponse.Headers.GetValues("X-Content-Type-Options").ShouldContain("nosniff");
        artifactResponse.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("text/html");
        var contentDisposition = artifactResponse.Content.Headers.ContentDisposition.ShouldNotBeNull();
        var actualValue7 = contentDisposition.FileNameStar ?? contentDisposition.FileName?.Trim('"');
        actualValue7.ShouldBe($"document-{generated.Id:N}-r1.html");
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
