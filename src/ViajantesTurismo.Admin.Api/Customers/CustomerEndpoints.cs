using ViajantesTurismo.Admin.Api.Customers.UpdateCustomer;
using ViajantesTurismo.Admin.Api.Customers.Import;
using ViajantesTurismo.Admin.Api.Customers.CreateCustomer;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Customers;

namespace ViajantesTurismo.Admin.Api.Customers;

/// <summary>
/// Defines all endpoints related to customer queries and operations.
/// </summary>
internal static class CustomerEndpoints
{
    /// <summary>
    /// Maps all customer endpoints to the application.
    /// </summary>
    /// <param name="app">The web application builder.</param>
    /// <returns>The web application for chaining.</returns>
    public static WebApplication MapCustomerEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var customersGroup = app.MapCustomersGroup();

        customersGroup.MapGet("/", GetAllCustomers)
            .RequireAuthorization(AdminAuthorization.CustomerRead, AdminAuthorization.CustomerSensitiveRead)
            .WithName("GetCustomers")
            .WithDescription("Retrieves all customers.")
            .WithSummary("Retrieves all customers.");

        customersGroup.MapGet("/{id:guid}", GetCustomerById)
            .RequireAuthorization(AdminAuthorization.CustomerRead, AdminAuthorization.CustomerSensitiveRead)
            .WithName("GetCustomerById")
            .WithDescription("Retrieves a customer by their ID.")
            .WithSummary("Retrieves a customer by their ID.");

        customersGroup.MapCreateCustomerEndpoint();
        customersGroup.MapUpdateCustomerEndpoint();
        app.MapCustomerImportEndpoints();

        return app;
    }

    private static async Task<Ok<IReadOnlyList<GetCustomerDto>>> GetAllCustomers(
        [FromServices] IQueryService queryService,
        CancellationToken ct)
    {
        var customers = await queryService.GetAllCustomers(ct);
        return TypedResults.Ok(customers);
    }

    private static async Task<Results<Ok<CustomerDetailsDto>, NotFound<ProblemDetails>>> GetCustomerById(
        [FromRoute] Guid id,
        [FromServices] IQueryService queryService,
        CancellationToken ct)
    {
        var customerDto = await queryService.GetCustomerDetailsById(id, ct);

        return customerDto is null
            ? CustomerErrors.CustomerNotFound().ToNotFound()
            : TypedResults.Ok(customerDto);
    }
}
