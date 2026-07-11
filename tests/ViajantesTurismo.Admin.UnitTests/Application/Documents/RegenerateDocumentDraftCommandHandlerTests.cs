using SharedKernel.Branding;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Admin.UnitTests.ApiService;
using ViajantesTurismo.Admin.UnitTests.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Application.Documents;

[Trait(SharedKernelTestTraitNames.CapabilityName, TestTraitValues.GeneratedDocumentsCapability)]
public sealed class RegenerateDocumentDraftCommandHandlerTests
{
    [Fact]
    public async Task Handle_does_not_save_when_current_document_is_missing()
    {
        // Arrange
        var store = new FakeDocumentStore();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RegenerateDocumentDraftCommandHandler(
            store,
            new FakeQueryService(null),
            new FakeBrandingApiClient(new BrandingSettingsDto()),
            unitOfWork,
            TimeProvider.System);

        // Act
        var result = await handler.Handle(
            new RegenerateDocumentDraftCommand(Guid.CreateVersion7(), "booking-confirmation", "2"),
            CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        store.AddedDocuments.ShouldBeEmpty();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }
}
