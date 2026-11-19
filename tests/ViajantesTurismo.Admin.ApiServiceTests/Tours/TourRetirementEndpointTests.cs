using Microsoft.Extensions.DependencyInjection.Extensions;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Tours;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Testing.Behavior;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.ApiServiceTests.Tours;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, PandaBear.Testing.TestTraitValues.EndpointCategory)]
[Trait(PandaBear.Testing.TestTraitNames.HostName, PandaBear.Testing.TestTraitValues.TestServerHost)]
public sealed class TourRetirementEndpointTests
{
    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("Reader", HttpStatusCode.Forbidden)]
    [InlineData("Operator", HttpStatusCode.Forbidden)]
    public async Task Retirement_and_status_require_the_tour_delete_permission(string? role, HttpStatusCode expected)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var factory = AdminApiTestHost.Create();
        using var client = factory.CreateClient();
        if (role is not null)
        {
            AdminApiTestHost.ConfigureAuthenticatedClient(client, role);
        }

        var path = $"/api/v1/tours/{Guid.CreateVersion7()}";

        // Act
        using var deletion = await client.DeleteAsync(new Uri(path, UriKind.Relative), ct);
        using var status = await client.GetAsync(new Uri(path + "/retirement", UriKind.Relative), ct);

        // Assert
        deletion.StatusCode.ShouldBe(expected);
        status.StatusCode.ShouldBe(expected);
    }

    [Theory]
    [InlineData(false, HttpStatusCode.Accepted)]
    [InlineData(true, HttpStatusCode.NoContent)]
    public async Task Retirement_reports_pending_before_catalog_completion_and_no_content_after_completion(
        bool applied, HttpStatusCode expected)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tour = EntityBuilders.BuildTour();
        var unitOfWork = new FakeUnitOfWork();
        var tours = new FakeTourStore();
        tours.Add(tour);
        DateTimeOffset? completedAt = null;
        if (applied)
        {
            var retiredAt = new DateTimeOffset(2026, 11, 19, 10, 0, 0, TimeSpan.Zero);
            tour.Retire(Guid.CreateVersion7(), "original-admin", retiredAt).IsSuccess.ShouldBeTrue();
            completedAt = retiredAt.AddMinutes(1);
        }

        await using var factory = AdminApiTestHost.Create(services =>
        {
            services.Replace(ServiceDescriptor.Singleton<ITourCommandStore>(tours));
            services.Replace(ServiceDescriptor.Singleton<IUnitOfWork>(unitOfWork));
            services.Replace(ServiceDescriptor.Singleton<IQueryService>(new TourRetirementQueries(tour, completedAt)));
        });
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
        var path = $"/api/v1/tours/{tour.Id}";
        var statusUri = new Uri(path + "/retirement", UriKind.Relative);

        // Act
        using var deletion = await client.DeleteAsync(new Uri(path, UriKind.Relative), ct);
        using var status = await client.GetAsync(statusUri, ct);
        var state = await status.Content.ReadFromJsonAsync<GetTourRetirementDto>(ct);

        // Assert
        deletion.StatusCode.ShouldBe(expected);
        if (!applied)
        {
            deletion.Headers.Location.ShouldBe(statusUri);
        }

        tour.IsRetired.ShouldBeTrue();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(applied ? 0 : 1);
        status.StatusCode.ShouldBe(HttpStatusCode.OK);
        status.Headers.CacheControl.ShouldNotBeNull();
        status.Headers.CacheControl.NoStore.ShouldBeTrue();
        state.ShouldNotBeNull();
        state.TourId.ShouldBe(tour.Id);
        state.RetiredAt.ShouldBe(tour.RetiredAt.GetValueOrDefault());
        state.CompletedAt.ShouldBe(completedAt);
    }

    [Fact]
    public async Task Retirement_rejects_active_bookings_and_returns_not_found_for_unknown_tours()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tour = EntityBuilders.BuildTour();
        BookingTestHelpers.AddSingleCustomerBooking(tour).IsSuccess.ShouldBeTrue();
        var tours = new FakeTourStore();
        tours.Add(tour);
        var unitOfWork = new FakeUnitOfWork();
        await using var factory = AdminApiTestHost.Create(services =>
        {
            services.Replace(ServiceDescriptor.Singleton<ITourCommandStore>(tours));
            services.Replace(ServiceDescriptor.Singleton<IUnitOfWork>(unitOfWork));
            services.Replace(ServiceDescriptor.Singleton<IQueryService>(new TourRetirementQueries(tour, null)));
        });
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");

        // Act
        using var conflict = await client.DeleteAsync(new Uri($"/api/v1/tours/{tour.Id}", UriKind.Relative), ct);
        using var missing = await client.DeleteAsync(new Uri($"/api/v1/tours/{Guid.Empty}", UriKind.Relative), ct);
        using var status = await client.GetAsync(new Uri($"/api/v1/tours/{tour.Id}/retirement", UriKind.Relative), ct);

        // Assert
        conflict.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        missing.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        status.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }
}
