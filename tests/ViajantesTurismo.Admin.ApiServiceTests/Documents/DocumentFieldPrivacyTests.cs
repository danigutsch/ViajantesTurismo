using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PandaBear.Testing;
using ViajantesTurismo.Admin.ApiServiceTests.Infrastructure.Documents;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Testing;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.ApiServiceTests.Documents;

[Trait(TestTraitNames.CategoryName, TestTraitValues.SecurityCategory)]
[Trait(TestTraitNames.HostName, TestTraitValues.TestServerHost)]
public sealed class DocumentFieldPrivacyTests
{
    [Fact]
    public async Task Updating_an_unknown_field_returns_a_safe_error_and_persists_only_rejection_metadata()
    {
        // Arrange
        const string fieldMarker = "synthetic-traveler@example.test";
        const string bodyMarker = "SYNTHETIC_DOCUMENT_MEDICAL_NOTE";
        var ct = TestContext.Current.CancellationToken;
        var store = DocumentFieldTestStore.Create();
        var audit = new CapturingDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        await using var factory = AdminApiTestHost.Create(services =>
        {
            services.Replace(ServiceDescriptor.Singleton<IDocumentStore>(store));
            services.Replace(ServiceDescriptor.Singleton<IDocumentAuditStore>(audit));
            services.Replace(ServiceDescriptor.Singleton<IUnitOfWork>(unitOfWork));
        });
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
        var path = new Uri($"/api/v1/documents/{store.DocumentId}/fields/{Uri.EscapeDataString(fieldMarker)}", UriKind.Relative);

        // Act
        using var response = await client.PatchAsJsonAsync(path, new UpdateDocumentFieldDto { Value = bodyMarker }, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(ct);
        var auditText = JsonSerializer.Serialize(audit.Records);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        DocumentNoStoreHeaders.ArePresent(response).ShouldBeTrue();
        body.ShouldNotContain(fieldMarker, StringComparison.Ordinal);
        body.ShouldNotContain(bodyMarker, StringComparison.Ordinal);
        problem.ShouldNotBeNull();
        problem.Detail.ShouldBe("The requested document field was not found.");
        auditText.ShouldNotContain(fieldMarker, StringComparison.Ordinal);
        auditText.ShouldNotContain(bodyMarker, StringComparison.Ordinal);
        var rejection = audit.Records.ShouldHaveSingleItem();
        rejection.DocumentId.ShouldBe(store.DocumentId);
        rejection.Operation.ShouldBe(DocumentAuditOperation.UpdateField);
        rejection.Outcome.ShouldBe(DocumentAuditOutcome.Rejected);
        rejection.ReasonCode.ShouldBe(DocumentAuditReasonCode.ValidationRejected);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }
}
