using ViajantesTurismo.Admin.Api.Customers.UpdateCustomer;
using ViajantesTurismo.Admin.Api.Customers.Import;
using ViajantesTurismo.Admin.Api.Customers.CreateCustomer;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Customers.Import;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Customers;

namespace ViajantesTurismo.Admin.Api.Customers;

/// <summary>
/// Defines all endpoints related to customer queries and operations.
/// </summary>
internal static class CustomerEndpoints
{
    private const string ListEndpoint = nameof(GetCustomerPage);
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

        customersGroup.MapGet("/page", GetCustomerPage)
            .RequireAuthorization(AdminAuthorization.CustomerRead, AdminAuthorization.CustomerSensitiveRead)
            .WithAdminMetadata(ListEndpoint, "Retrieves a page of customers.",
                "Retrieves bounded customer summaries and the total count. The optional page number is one-based.");

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

        customersGroup.MapPost("/import/conflicts/lookup", FindCustomerImportConflictIds)
            .RequireAuthorization(AdminAuthorization.CustomerRead, AdminAuthorization.CustomerSensitiveRead)
            .WithName(nameof(FindCustomerImportConflictIds))
            .WithSummary("Finds existing customer IDs for import conflicts.")
            .WithDescription($"Returns an ID or null for each email in request order. A request accepts at most {ContractConstants.CustomerImportConflictLookupMaxKeys} emails.");

        customersGroup.MapCreateCustomerEndpoint();
        customersGroup.MapUpdateCustomerEndpoint();
        app.MapCustomerImportEndpoints();

        return app;
    }

    private static async Task<Results<Ok<Guid?[]>, ValidationProblem>> FindCustomerImportConflictIds(
        [FromBody] string[] emails,
        [FromServices] CustomerImportConflictDetector detector,
        CancellationToken ct)
    {
        if (emails.Length is 0 or > ContractConstants.CustomerImportConflictLookupMaxKeys)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["emails"] = [$"Provide between 1 and {ContractConstants.CustomerImportConflictLookupMaxKeys} conflict email keys."]
            });
        }

        return TypedResults.Ok(await detector.FindExistingCustomerIds(emails, ct));
    }

    private static async Task<Results<Ok<GetCustomerPageDto>, BadRequest>> GetCustomerPage(
        [FromQuery] string? page,
        [FromQuery] string? sort,
        [FromQuery] bool? descending,
        [FromServices] IQueryService queryService,
        LinkGenerator linkGenerator,
        HttpContext context,
        CancellationToken ct)
    {
        var sortKey = CustomerPageSort.Name;
        if (sort is not null && (!Enum.TryParse(sort, ignoreCase: true, out sortKey) || !Enum.IsDefined(sortKey)))
        {
            return TypedResults.BadRequest();
        }

        var customers = await queryService.GetCustomerPage(page, sortKey, descending.GetValueOrDefault(), ct);
        var links = GetResourceLinks(context, linkGenerator);
        return TypedResults.Ok(customers with
        {
            Items = [.. customers.Items.Select(customer => customer with { Links = links.For(customer.Id).Links })]
        });
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
