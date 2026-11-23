using PandaBear.Testing;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.ApiServiceTests.Payments;

[Trait(TestTraitNames.CategoryName, TestTraitValues.EndpointCategory)]
[Trait(TestTraitNames.HostName, TestTraitValues.TestServerHost)]
[TestScope(TestScope.Component)]
public sealed class BookingFinancialPageEndpointTests
{
    [Theory]
    [InlineData("Admin", true, false)]
    [InlineData("Admin", true, true)]
    [InlineData("Operator", true, false)]
    [InlineData("Operator", false, false)]
    public async Task Authorized_finances_windows_obey_the_feature_flag_and_disable_caching(string role, bool enabled, bool evidence)
    {
        // Arrange
        var queries = new BookingFinancialPageQueryProbe();
        await using var factory = queries.CreateHost(enabled);
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, role);
        var bookingId = Guid.CreateVersion7();
        var suffix = evidence ? "projection/details" : "projection";
        var uri = new Uri($"/api/v1/bookings/{bookingId}/finances/{suffix}?section=Receipts&page=2&pageSize=10", UriKind.Relative);

        // Act
        using var response = await client.GetAsync(uri, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(enabled ? HttpStatusCode.OK : HttpStatusCode.NotFound);
        var noStore = response.Headers.CacheControl?.NoStore;
        noStore.ShouldBe(true);
        if (enabled)
        {
            var (observedBooking, request, actorId) = queries.Read.ShouldNotBeNull();
            observedBooking.ShouldBe(bookingId);
            request.Section.ShouldBe(BookingFinancialSection.Receipts);
            request.Page.ShouldBe(2);
            request.PageSize.ShouldBe(10);
            if (evidence)
            {
                actorId.ShouldNotBeNull().ShouldNotBeEmpty();
            }
            else
            {
                actorId.ShouldBeNull();
            }
        }
        else
        {
            queries.Read.ShouldBeNull();
        }
    }

    [Theory]
    [InlineData("?pageSize=0")]
    [InlineData("?pageSize=101")]
    [InlineData("?section=911")]
    [InlineData("?section=ScheduleInstallments")]
    [InlineData("?section=Receipts&parentId=ed4796d1-3ade-48d5-8ce0-83ec2d17969e")]
    public async Task Invalid_projection_ranges_and_parent_arguments_are_rejected_before_reading(string query)
    {
        // Arrange
        var queries = new BookingFinancialPageQueryProbe();
        await using var factory = queries.CreateHost(enabled: true);
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Operator");
        var bookingId = Guid.CreateVersion7();
        var uri = new Uri($"/api/v1/bookings/{bookingId}/finances/projection{query}", UriKind.Relative);

        // Act
        using var response = await client.GetAsync(uri, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        queries.Read.ShouldBeNull();
    }
}
