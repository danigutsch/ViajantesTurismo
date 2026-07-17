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

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
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
        }
    }
}
