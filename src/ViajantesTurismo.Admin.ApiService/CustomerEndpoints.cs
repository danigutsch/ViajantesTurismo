using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Customers.CreateCustomer;
using ViajantesTurismo.Admin.Application.Customers.UpdateCustomer;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.ApiService;

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

        var customersGroup = app.MapGroup("/customers")
            .WithGroupName("Customers")
            .WithTags("Customers");

        customersGroup.MapGet("/", GetAllCustomers)
            .WithName("GetCustomers")
            .WithDescription("Retrieves all customers.")
            .WithSummary("Retrieves all customers.");

        customersGroup.MapGet("/{id:guid}", GetCustomerById)
            .WithName("GetCustomerById")
            .WithDescription("Retrieves a customer by their ID.")
            .WithSummary("Retrieves a customer by their ID.");

        customersGroup.MapPost("/", CreateCustomer)
            .WithName("CreateCustomer")
            .WithDescription("Creates a new customer with all required information.")
            .WithSummary("Creates a new customer.");

        customersGroup.MapPut("/{id:guid}", UpdateCustomer)
            .WithName("UpdateCustomer")
            .WithDescription("Updates an existing customer.")
            .WithSummary("Updates an existing customer.");

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
            ? CustomerErrors.CustomerNotFound(id).ToNotFound()
            : TypedResults.Ok(customerDto);
    }

    private static async Task<Results<Created<GetCustomerDto>, ValidationProblem, NotFound<ProblemDetails>, Conflict<ProblemDetails>>>
        CreateCustomer(
            [FromBody] CreateCustomerDto dto,
            [FromServices] CreateCustomerCommandHandler handler,
            [FromServices] IQueryService queryService,
            CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var command = new CreateCustomerCommand(
            dto.PersonalInfo,
            dto.IdentificationInfo,
            dto.ContactInfo,
            dto.Address,
            dto.PhysicalInfo,
            dto.AccommodationPreferences,
            dto.EmergencyContact,
            dto.MedicalInfo);

        var result = await handler.Handle(command, ct);

        if (result.IsFailure)
        {
            return result.Status switch
            {
                ResultStatus.Conflict => result.ToConflict(),
                _ => result.ToValidationProblem()
            };
        }

        var customerDto = await queryService.GetCustomerDetailsById(result.Value, ct);

        if (customerDto is null)
        {
            return CustomerErrors.CustomerNotFound(result.Value).ToNotFound();
        }

        var getCustomerDto = new GetCustomerDto
        {
            Id = customerDto.Id,
            FirstName = customerDto.PersonalInfo.FirstName,
            LastName = customerDto.PersonalInfo.LastName,
            Email = customerDto.ContactInfo.Email,
            Mobile = customerDto.ContactInfo.Mobile,
            Nationality = customerDto.PersonalInfo.Nationality,
            BikeType = customerDto.PhysicalInfo.BikeType
        };

        return TypedResults.Created($"/customers/{result.Value}", getCustomerDto);
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>, ValidationProblem>> UpdateCustomer(
        [FromRoute] Guid id,
        [FromBody] UpdateCustomerDto dto,
        [FromServices] UpdateCustomerCommandHandler handler,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var command = new UpdateCustomerCommand(
            id,
            dto.PersonalInfo,
            dto.IdentificationInfo,
            dto.ContactInfo,
            dto.Address,
            dto.PhysicalInfo,
            dto.AccommodationPreferences,
            dto.EmergencyContact,
            dto.MedicalInfo);

        var result = await handler.Handle(command, ct);

        if (result.IsFailure)
        {
            return result.Status switch
            {
                ResultStatus.NotFound => result.ToNotFound(),
                _ => result.ToValidationProblem()
            };
        }

        return TypedResults.NoContent();
    }
}
