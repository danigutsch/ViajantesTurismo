using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SharedKernel.AspNetCore;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Testing.Builders;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.ApiServiceTests;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, SharedKernel.Testing.TestTraitValues.SecurityCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.HostName, SharedKernel.Testing.TestTraitValues.TestServerHost)]
public sealed class AdminApiErrorTelemetryTests
{
    [Fact]
    public async Task Unhandled_exception_returns_problem_JSON_without_exception_text_and_is_counted()
    {
        // Arrange
        const string secret = "secret-store-detail";
        await using var factory = AdminApiTestHost.Create(services =>
        {
            services.Replace(ServiceDescriptor.Scoped<ICustomerStore>(_ => new FakeCustomerStore()));
            services.Replace(ServiceDescriptor.Scoped<IUnitOfWork>(_ => new ThrowingUnitOfWork(new InvalidOperationException(secret))));
        });
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
        using var recorder = new MeterFactoryRecorder(factory.Services.GetRequiredService<IMeterFactory>());

        // Act
        using var response = await client.PostAsJsonAsync(
            new Uri("/api/v1/customers/", UriKind.Relative),
            DtoBuilders.BuildCreateCustomerDto(),
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        var contentType = response.Content.Headers.ContentType;
        contentType.ShouldNotBeNull();
        contentType.MediaType.ShouldBe("application/problem+json");
        problem.ShouldNotBeNull();
        body.ShouldNotContain(secret, StringComparison.Ordinal);
        body.ShouldNotContain(nameof(InvalidOperationException), StringComparison.Ordinal);
        recorder.Sum(
            AspNetCoreTelemetry.UnhandledExceptionsCounterName,
            AspNetCoreTelemetry.ErrorTypeTag,
            typeof(InvalidOperationException).FullName.ShouldNotBeNull()).ShouldBe(1);
    }

    [Fact]
    public async Task Anonymous_request_to_a_protected_route_counts_one_authentication_challenge()
    {
        // Arrange
        await using var factory = AdminApiTestHost.Create();
        using var client = factory.CreateClient();
        using var recorder = new MeterFactoryRecorder(factory.Services.GetRequiredService<IMeterFactory>());

        // Act
        using var response = await client.GetAsync(new Uri("/api/v1/tours/", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        recorder.Sum(
            AspNetCoreTelemetry.AuthenticationEventsCounterName,
            AspNetCoreTelemetry.EventTag,
            "challenge").ShouldBe(1);
    }
}
