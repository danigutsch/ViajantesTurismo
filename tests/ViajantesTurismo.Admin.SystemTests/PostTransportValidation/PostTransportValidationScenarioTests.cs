using System.Net;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Admin.SystemTests.Infrastructure;

namespace ViajantesTurismo.Admin.SystemTests.PostTransportValidation;

[Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.PostTransportArea)]
[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.IntegrationEventTransportCategory)]
public sealed class PostTransportValidationScenarioTests
{
    [Fact]
    public async Task Wait_for_catalog_tour_retries_after_a_transient_server_error()
    {
        // Arrange
        var expectedTour = new CatalogTourDto
        {
            Id = Guid.CreateVersion7(),
            AdminTourId = Guid.CreateVersion7(),
            Identifier = "TRANSIENT-500",
            Title = "Transient catalog result",
            Slug = "transient-catalog-result",
            IsPublished = false,
            Images = [],
            UpdatedAt = DateTimeOffset.UtcNow
        };
        using var adminApi = new HttpClient();
        var catalogTours = new TransientCatalogToursApiClient(expectedTour);
        var scenario = new PostTransportValidationScenario(adminApi, catalogTours);

        // Act
        var catalogTour = await scenario.WaitForCatalogTour(
            expectedTour.AdminTourId,
            TestContext.Current.CancellationToken);

        // Assert
        catalogTour.ShouldBe(expectedTour);
        catalogTours.GetToursCallCount.ShouldBe(2);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task Wait_for_catalog_tour_propagates_nonretryable_catalog_errors(HttpStatusCode statusCode)
    {
        // Arrange
        var expectedTour = new CatalogTourDto
        {
            Id = Guid.CreateVersion7(),
            AdminTourId = Guid.CreateVersion7(),
            Identifier = "NONRETRYABLE",
            Title = "Nonretryable catalog result",
            Slug = "nonretryable-catalog-result",
            IsPublished = false,
            Images = [],
            UpdatedAt = DateTimeOffset.UtcNow
        };
        using var adminApi = new HttpClient();
        var catalogTours = new TransientCatalogToursApiClient(expectedTour, statusCode);
        var scenario = new PostTransportValidationScenario(adminApi, catalogTours);

        // Act
        Func<Task> action = () => scenario.WaitForCatalogTour(
            expectedTour.AdminTourId,
            TestContext.Current.CancellationToken);
        var exception = await action.ShouldThrow<HttpRequestException>();

        // Assert
        exception.StatusCode.ShouldBe(statusCode);
        catalogTours.GetToursCallCount.ShouldBe(1);
    }
}
