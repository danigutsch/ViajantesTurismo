using Microsoft.AspNetCore.Mvc;
using PandaBear.Testing;
using ViajantesTurismo.Admin.ApiServiceTests.Infrastructure.Customers;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Testing.Behavior;
using ViajantesTurismo.Admin.Testing.Builders;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.ApiServiceTests.Customers;

[Trait(TestTraitNames.CategoryName, TestTraitValues.EndpointCategory)]
[Trait(TestTraitNames.HostName, TestTraitValues.TestServerHost)]
public sealed class CustomerMutationEndpointTests
{
    private static readonly Uri CustomersRoute = new("/api/v1/customers/", UriKind.Relative);

    [Fact]
    public async Task Create_returns_the_customer_summary_and_location_from_the_read_projection()
    {
        // Arrange
        var customers = new FakeCustomerStore();
        var projection = DtoBuilders.BuildCustomerDetailsDto();
        await using var host = new CustomerMutationTestHost(customers, id =>
            DtoBuilders.BuildCustomerDetailsDto(
                id: id,
                personalInfo: projection.PersonalInfo,
                contactInfo: projection.ContactInfo,
                physicalInfo: projection.PhysicalInfo));
        var request = DtoBuilders.BuildCreateCustomerDto(firstName: "Submitted", email: "submitted@example.com");
        var ct = TestContext.Current.CancellationToken;

        // Act
        using var response = await host.Client.PostAsJsonAsync(CustomersRoute, request, ct);
        var summary = await response.Content.ReadFromJsonAsync<GetCustomerDto>(ct);

        // Assert
        var customer = customers.AllCustomers.ShouldHaveSingleItem();
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location.ShouldNotBeNull().OriginalString.ShouldBe($"/api/v1/customers/{customer.Id}");
        summary.ShouldNotBeNull();
        summary.Id.ShouldBe(customer.Id);
        summary.FirstName.ShouldBe(projection.PersonalInfo.FirstName);
        summary.LastName.ShouldBe(projection.PersonalInfo.LastName);
        summary.Email.ShouldBe(projection.ContactInfo.Email);
        summary.Mobile.ShouldBe(projection.ContactInfo.Mobile);
        summary.Nationality.ShouldBe(projection.PersonalInfo.Nationality);
        summary.BikeType.ShouldBe(projection.PhysicalInfo.BikeType);
    }

    [Fact]
    public async Task Create_returns_not_found_when_the_written_customer_is_missing_from_the_read_projection()
    {
        // Arrange
        var customers = new FakeCustomerStore();
        await using var host = new CustomerMutationTestHost(customers);
        var request = DtoBuilders.BuildCreateCustomerDto();
        var ct = TestContext.Current.CancellationToken;

        // Act
        using var response = await host.Client.PostAsJsonAsync(CustomersRoute, request, ct);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(ct);

        // Assert
        customers.AllCustomers.ShouldHaveSingleItem();
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        problem.ShouldNotBeNull().Status.ShouldBe(404);
        response.Headers.Location.ShouldBeNull();
    }

    [Fact]
    public async Task Create_returns_a_validation_problem_and_skips_the_write_for_an_invalid_email()
    {
        // Arrange
        var customers = new FakeCustomerStore();
        await using var host = new CustomerMutationTestHost(customers);
        var request = DtoBuilders.BuildCreateCustomerDto(email: "invalid-email");
        var ct = TestContext.Current.CancellationToken;

        // Act
        using var response = await host.Client.PostAsJsonAsync(CustomersRoute, request, ct);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problem.ShouldNotBeNull().Errors.Keys.ShouldContain("Email");
        customers.AllCustomers.ShouldBeEmpty();
    }

    [Fact]
    public async Task Update_returns_an_empty_no_content_response_after_revising_the_customer()
    {
        // Arrange
        var customers = new FakeCustomerStore();
        var customer = EntityBuilders.BuildCustomer(options: null);
        customers.AddExistingCustomer(customer);
        await using var host = new CustomerMutationTestHost(customers);
        var request = DtoBuilders.BuildUpdateCustomerDto(firstName: "Revised", email: "revised@example.com");
        var uri = new Uri($"{CustomersRoute}{customer.Id}", UriKind.Relative);
        var ct = TestContext.Current.CancellationToken;

        // Act
        using var response = await host.Client.PutAsJsonAsync(uri, request, ct);
        var body = await response.Content.ReadAsByteArrayAsync(ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        body.ShouldBeEmpty();
        customer.PersonalInfo.FirstName.ShouldBe(request.PersonalInfo.FirstName);
        customer.ContactInfo.Email.Value.ShouldBe(request.ContactInfo.Email);
    }

    [Fact]
    public async Task Update_returns_not_found_without_creating_a_customer_for_an_unknown_ID()
    {
        // Arrange
        var customers = new FakeCustomerStore();
        await using var host = new CustomerMutationTestHost(customers);
        var request = DtoBuilders.BuildUpdateCustomerDto();
        var uri = new Uri($"{CustomersRoute}{Guid.CreateVersion7()}", UriKind.Relative);
        var ct = TestContext.Current.CancellationToken;

        // Act
        using var response = await host.Client.PutAsJsonAsync(uri, request, ct);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        problem.ShouldNotBeNull().Status.ShouldBe(404);
        customers.AllCustomers.ShouldBeEmpty();
    }

    [Fact]
    public async Task Update_returns_a_validation_problem_and_preserves_the_customer_for_an_invalid_email()
    {
        // Arrange
        var customers = new FakeCustomerStore();
        var customer = EntityBuilders.BuildCustomer(options: null);
        customers.AddExistingCustomer(customer);
        var originalEmail = customer.ContactInfo.Email.Value;
        var originalFirstName = customer.PersonalInfo.FirstName;
        await using var host = new CustomerMutationTestHost(customers);
        var request = DtoBuilders.BuildUpdateCustomerDto(firstName: "Rejected", email: "invalid-email");
        var uri = new Uri($"{CustomersRoute}{customer.Id}", UriKind.Relative);
        var ct = TestContext.Current.CancellationToken;

        // Act
        using var response = await host.Client.PutAsJsonAsync(uri, request, ct);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problem.ShouldNotBeNull().Errors.Keys.ShouldContain("Email");
        customer.ContactInfo.Email.Value.ShouldBe(originalEmail);
        customer.PersonalInfo.FirstName.ShouldBe(originalFirstName);
    }
}
