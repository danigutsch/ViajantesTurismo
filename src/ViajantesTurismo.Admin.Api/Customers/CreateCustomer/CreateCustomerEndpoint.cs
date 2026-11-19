using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using PandaBear.Mediator;
using PandaBear.Results;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Customers.CreateCustomer;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Customers;

namespace ViajantesTurismo.Admin.Api.Customers.CreateCustomer;

/// <summary>Endpoint for creating a customer.</summary>
internal static class CreateCustomerEndpoint
{
    /// <summary>Maps the creating operation on the customers group.</summary>
    /// <param name="customersGroup">The Admin customer routes.</param>
    public static void MapCreateCustomerEndpoint(this RouteGroupBuilder customersGroup)
    {
        ArgumentNullException.ThrowIfNull(customersGroup);

        customersGroup.MapPost("/", CreateCustomer)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .RequireAuthorization(AdminAuthorization.CustomerWrite)
            .WithName("CreateCustomer")
            .WithDescription("Creates a new customer with all required information.")
            .WithSummary("Creates a new customer.");
    }

    private static async Task<Results<Created<GetCustomerDto>, ValidationProblem, NotFound<ProblemDetails>, Conflict<ProblemDetails>>>
        CreateCustomer(
            [FromBody] CreateCustomerDto dto,
            [FromServices] ISender sender,
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

        var result = await sender.Send(command, ct);

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
            return CustomerErrors.CustomerNotFound().ToNotFound();
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

        return TypedResults.Created($"/api/v1/customers/{result.Value}", getCustomerDto);
    }
}
