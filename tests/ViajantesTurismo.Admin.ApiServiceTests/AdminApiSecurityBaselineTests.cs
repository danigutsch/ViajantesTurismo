using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.ApiServiceTests;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, SharedKernel.Testing.TestTraitValues.SecurityCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.HostName, SharedKernel.Testing.TestTraitValues.TestServerHost)]
public sealed class AdminApiSecurityBaselineTests
{
    [Fact]
    public async Task Admin_mutations_return_too_many_requests_after_policy_limit()
    {
        // Arrange
        await using var factory = AdminApiTestHost.Create(services =>
        {
            services.Replace(ServiceDescriptor.Scoped<ITourStore, FakeTourStore>());
            services.Replace(ServiceDescriptor.Scoped<IUnitOfWork, FakeUnitOfWork>());
        });
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
        var request = new CreateTourDto
        {
            Identifier = "rate-limit-tour",
            Name = "Rate limit tour",
            StartDate = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2030, 1, 8, 0, 0, 0, DateTimeKind.Utc),
            Price = 0m,
            SingleRoomSupplementPrice = 1m,
            RegularBikePrice = 1m,
            EBikePrice = 1m,
            Currency = CurrencyDto.UsDollar,
            IncludedServices = ["Hotel"],
            MinCustomers = 1,
            MaxCustomers = 1
        };

        // Act
        for (var requestNumber = 0; requestNumber < 300; requestNumber++)
        {
            using var content = JsonContent.Create(request);
            using var allowedResponse = await client.PostAsync(
                new Uri("/api/v1/tours/", UriKind.Relative),
                content,
                TestContext.Current.CancellationToken);
            allowedResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }

        using var limitedContent = JsonContent.Create(request);
        using var limitedResponse = await client.PostAsync(
            new Uri("/api/v1/tours/", UriKind.Relative),
            limitedContent,
            TestContext.Current.CancellationToken);

        // Assert
        limitedResponse.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Admin_imports_return_too_many_requests_after_policy_limit()
    {
        // Arrange
        await using var factory = AdminApiTestHost.Create();
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");

        // Act
        for (var requestNumber = 0; requestNumber < 5; requestNumber++)
        {
            using var content = new MultipartFormDataContent();
            var file = new ByteArrayContent([]);
            file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                ContractConstants.CustomerImportTextCsvContentType);
            content.Add(file, "file", "customers.csv");
            using var allowedResponse = await client.PostAsync(
                new Uri("/api/v1/customers/import/", UriKind.Relative),
                content,
                TestContext.Current.CancellationToken);
            allowedResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }

        using var limitedContent = new MultipartFormDataContent();
        var limitedFile = new ByteArrayContent([]);
        limitedFile.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
            ContractConstants.CustomerImportTextCsvContentType);
        limitedContent.Add(limitedFile, "file", "customers.csv");
        using var limitedResponse = await client.PostAsync(
            new Uri("/api/v1/customers/import/", UriKind.Relative),
            limitedContent,
            TestContext.Current.CancellationToken);

        // Assert
        limitedResponse.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }
}
