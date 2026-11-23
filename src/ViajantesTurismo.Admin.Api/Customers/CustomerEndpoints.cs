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
    private const string ListEndpoint = nameof(GetCustomers);
    private const string ItemEndpoint = nameof(GetCustomerById);

    /// <summary>
    /// Maps all customer endpoints to the application.
    /// </summary>
    /// <param name="app">The web application builder.</param>
    /// <returns>The web application for chaining.</returns>
    public static WebApplication MapCustomerEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var customersGroup = app.MapCustomersGroup();

        customersGroup.MapGet("/", GetCustomers)
            .RequireAuthorization(AdminAuthorization.CustomerRead, AdminAuthorization.CustomerSensitiveRead)
            .WithName(ListEndpoint)
            .WithDescription("Retrieves all customers.")
            .WithSummary("Retrieves all customers.");

        customersGroup.MapPost("/options/search", SearchCustomerOptions)
            .RequireAuthorization(AdminAuthorization.CustomerRead, AdminAuthorization.CustomerSensitiveRead)
            .WithName(nameof(SearchCustomerOptions))
            .WithDescription("Searches for a bounded set of customer choices by name or exact identifier.")
            .WithSummary("Searches customer choices.");

        customersGroup.MapGet("/{id:guid}", GetCustomerById)
            .RequireAuthorization(AdminAuthorization.CustomerRead, AdminAuthorization.CustomerSensitiveRead)
            .WithName(ItemEndpoint)
            .WithDescription("Retrieves a customer by their ID.")
            .WithSummary("Retrieves a customer by their ID.");

        customersGroup.MapCreateCustomerEndpoint();
        customersGroup.MapUpdateCustomerEndpoint();
        app.MapCustomerImportEndpoints();

        return app;
    }

    private static async Task<Ok<IReadOnlyList<GetCustomerDto>>> GetCustomers(
        [FromServices] IQueryService queryService,
        LinkGenerator linkGenerator,
        HttpContext context,
        CancellationToken ct)
    {
        var customers = await queryService.GetAllCustomers(ct);
        var links = GetResourceLinks(context, linkGenerator);
        return TypedResults.Ok<IReadOnlyList<GetCustomerDto>>(
            [.. customers.Select(customer => customer with { Links = links.For(customer.Id).Links })]);
    }

    private static async Task<Results<Ok<GetCustomerOptionsDto>, BadRequest>> SearchCustomerOptions(
        [FromBody] CustomerOptionSearchDto request,
        [FromServices] IQueryService queryService,
        CancellationToken ct)
    {
        if (request.Query is { Length: > ContractConstants.MaxCustomerOptionQueryLength } || request.Limit <= 0)
        {
            return TypedResults.BadRequest();
        }

        var options = await queryService.SearchCustomerOptions(request, ct);
        return TypedResults.Ok(options);
    }

    private static async Task<Results<Ok<CustomerDetailsDto>, NotFound<ProblemDetails>>> GetCustomerById(
        [FromRoute] Guid id,
        [FromServices] IQueryService queryService,
        LinkGenerator linkGenerator,
        HttpContext context,
        CancellationToken ct)
    {
        var customerDto = await queryService.GetCustomerDetailsById(id, ct);

        return customerDto is null
            ? CustomerErrors.CustomerNotFound().ToNotFound()
            : TypedResults.Ok(customerDto with
            {
                Links = GetResourceLinks(context, linkGenerator).For(id).Links
            });
    }

    /// <summary>Creates the link factory for customer representations.</summary>
    internal static AdminResourceLinks GetResourceLinks(HttpContext context, LinkGenerator linkGenerator) =>
        new(context, linkGenerator, ItemEndpoint, ListEndpoint);
}
