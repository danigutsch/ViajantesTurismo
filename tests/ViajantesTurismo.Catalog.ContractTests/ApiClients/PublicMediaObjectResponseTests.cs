using ViajantesTurismo.Catalog.Contracts.Http;
using SharedKernel.Testing.Contracts;

namespace ViajantesTurismo.Catalog.ContractTests.ApiClients;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, Infrastructure.TestTraits.ContractCategory)]
public sealed class PublicMediaObjectResponseTests
{
    [Fact]
    public async Task Disposes_the_http_response_when_content_disposal_fails()
    {
        // Arrange
        var responseContent = new TrackingHttpContent();
        using var response = new HttpResponseMessage { Content = responseContent };
        await using var media = new PublicMediaObjectResponse(response, new ThrowingDisposeStream(new InvalidOperationException("Content disposal failed.")), "image/jpeg");

        // Act
        Func<Task> act = async () => await media.DisposeAsync();

        // Assert
        _ = await act.ShouldThrow<InvalidOperationException>();
        responseContent.IsDisposed.ShouldBeTrue();
    }
}
