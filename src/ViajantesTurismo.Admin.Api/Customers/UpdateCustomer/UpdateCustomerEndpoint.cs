using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using PandaBear.Mediator;
using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Customers.UpdateCustomer;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Api.Customers.UpdateCustomer;

/// <summary>Endpoint for updating a customer.</summary>
internal static class UpdateCustomerEndpoint
{
    /// <summary>Maps the updating operation on the customers group.</summary>
    /// <param name="customersGroup">The Admin customer routes.</param>
    public static void MapUpdateCustomerEndpoint(this RouteGroupBuilder customersGroup)
    {
        ArgumentNullException.ThrowIfNull(customersGroup);

        customersGroup.MapPut("/{id:guid}", UpdateCustomer)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .RequireAuthorization(AdminAuthorization.CustomerWrite)
            .WithName("UpdateCustomer")
            .WithDescription("Updates an existing customer.")
            .WithSummary("Updates an existing customer.");
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem>>
        UpdateCustomer(
        [FromRoute] Guid id,
        [FromBody] UpdateCustomerDto dto,
        [FromServices] ISender sender,
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

        var result = await sender.Send(command, ct);

        if (result.IsFailure)
        {
            return result.Status switch
            {
                ResultStatus.NotFound => result.ToNotFound(),
                ResultStatus.Conflict => result.ToConflict(),
                _ => result.ToValidationProblem()
            };
        }

        return TypedResults.NoContent();
    }
}
