namespace ViajantesTurismo.Admin.IntegrationTests.Documents;

internal static class DocumentApiTestSetup
{
    public static async Task<(Guid BookingId, DocumentsApiClient Client, GetDocumentDto Document)>
        CreateGeneratedDocument(ApiFixture fixture, string scenarioName, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioName);

        var tour = await fixture.Client.CreateTestTour(
            $"document-{scenarioName}",
            $"Document {scenarioName}",
            ct);
        var customer = await fixture.Client.CreateTestCustomer("Document", "Lifecycle", ct);
        var booking = await fixture.Client.CreateTestBooking(tour.Id, customer.Id, null, ct);
        using var confirmResponse = await fixture.Client.ConfirmBooking(booking.Id, ct);
        confirmResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var client = new DocumentsApiClient(fixture.Client);
        var document = await client.GenerateContractDraft(booking.Id, ct);
        return (booking.Id, client, document);
    }
}
