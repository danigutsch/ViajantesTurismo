using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Customers;
using ViajantesTurismo.Admin.Application.Mappings;
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
        var customersGroup = app.MapGroup("/customers")
            .WithGroupName("Customers")
            .WithTags("Customers");

        customersGroup.MapGet("/", GetAllCustomers)
            .WithName("GetCustomers")
            .WithDescription("Retrieves all customers.")
            .WithSummary("Retrieves all customers.");

        customersGroup.MapGet("/{id:int}", GetCustomerById)
            .WithName("GetCustomerById")
            .WithDescription("Retrieves a customer by their ID.")
            .WithSummary("Retrieves a customer by their ID.");

        customersGroup.MapPost("/", CreateCustomer)
            .WithName("CreateCustomer")
            .WithDescription("Creates a new customer with all required information.")
            .WithSummary("Creates a new customer.");

        customersGroup.MapPut("/{id:int}", UpdateCustomer)
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
        [FromRoute] int id,
        [FromServices] IQueryService queryService,
        CancellationToken ct)
    {
        var customerDto = await queryService.GetCustomerDetailsById(id, ct);
        if (customerDto is null)
        {
            return CustomerErrors.CustomerNotFound(id).ToNotFound();
        }

        return TypedResults.Ok(customerDto);
    }

    private static async Task<Results<Created<GetCustomerDto>, ValidationProblem>> CreateCustomer(
        [FromBody] CreateCustomerDto dto,
        [FromServices] ICustomerStore customerStore,
        [FromServices] IUnitOfWork unitOfWork,
        [FromServices] TimeProvider timeProvider,
        CancellationToken ct)
    {
        var personalInfoResult = PersonalInfo.Create(
            dto.PersonalInfo.FirstName,
            dto.PersonalInfo.LastName,
            dto.PersonalInfo.Gender,
            dto.PersonalInfo.BirthDate.ToUniversalTime(),
            dto.PersonalInfo.Nationality,
            dto.PersonalInfo.Profession,
            timeProvider);

        var identificationInfoResult = IdentificationInfo.Create(
            dto.IdentificationInfo.NationalId,
            dto.IdentificationInfo.IdNationality);

        var contactInfoResult = ContactInfo.Create(
            dto.ContactInfo.Email,
            dto.ContactInfo.Mobile,
            dto.ContactInfo.Instagram,
            dto.ContactInfo.Facebook);

        var errors = new ValidationErrors();
        if (!personalInfoResult.IsSuccess)
        {
            errors.Add(personalInfoResult);
        }

        if (!identificationInfoResult.IsSuccess)
        {
            errors.Add(identificationInfoResult);
        }

        if (!contactInfoResult.IsSuccess)
        {
            errors.Add(contactInfoResult);
        }

        if (errors.HasErrors)
        {
            return errors.ToResult<GetCustomerDto>().ToValidationProblem();
        }

        var address = CustomerMapper.MapToAddress(dto.Address);
        var physicalInfo = CustomerMapper.MapToPhysicalInfo(dto.PhysicalInfo);
        var accommodationPreferences = CustomerMapper.MapToAccommodationPreferences(dto.AccommodationPreferences);
        var emergencyContact = CustomerMapper.MapToEmergencyContact(dto.EmergencyContact);
        var medicalInfo = CustomerMapper.MapToMedicalInfo(dto.MedicalInfo);

        var customer = new Customer(
            personalInfoResult.Value,
            identificationInfoResult.Value,
            contactInfoResult.Value,
            address,
            physicalInfo,
            accommodationPreferences,
            emergencyContact,
            medicalInfo
        );

        customerStore.Add(customer);
        await unitOfWork.SaveEntities(ct);

        var customerDto = new GetCustomerDto
        {
            Id = customer.Id,
            FirstName = customer.PersonalInfo.FirstName,
            LastName = customer.PersonalInfo.LastName,
            Email = customer.ContactInfo.Email,
            Mobile = customer.ContactInfo.Mobile,
            Nationality = customer.PersonalInfo.Nationality,
            BikeType = BookingMapper.MapToBikeTypeDto(customer.PhysicalInfo.BikeType)
        };

        return TypedResults.Created($"/customers/{customer.Id}", customerDto);
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>, ValidationProblem>> UpdateCustomer(
        [FromRoute] int id,
        [FromBody] UpdateCustomerDto dto,
        [FromServices] ICustomerStore customerStore,
        [FromServices] IUnitOfWork unitOfWork,
        [FromServices] TimeProvider timeProvider,
        CancellationToken ct)
    {
        var customer = await customerStore.GetById(id, ct);
        if (customer is null)
        {
            return CustomerErrors.CustomerNotFound(id).ToNotFound();
        }

        var personalInfoResult = PersonalInfo.Create(
            dto.PersonalInfo.FirstName,
            dto.PersonalInfo.LastName,
            dto.PersonalInfo.Gender,
            dto.PersonalInfo.BirthDate.ToUniversalTime(),
            dto.PersonalInfo.Nationality,
            dto.PersonalInfo.Profession,
            timeProvider);

        var identificationInfoResult = IdentificationInfo.Create(
            dto.IdentificationInfo.NationalId,
            dto.IdentificationInfo.IdNationality);

        var contactInfoResult = ContactInfo.Create(
            dto.ContactInfo.Email,
            dto.ContactInfo.Mobile,
            dto.ContactInfo.Instagram,
            dto.ContactInfo.Facebook);

        var errors = new ValidationErrors();
        if (!personalInfoResult.IsSuccess)
        {
            errors.Add(personalInfoResult);
        }

        if (!identificationInfoResult.IsSuccess)
        {
            errors.Add(identificationInfoResult);
        }

        if (!contactInfoResult.IsSuccess)
        {
            errors.Add(contactInfoResult);
        }

        if (errors.HasErrors)
        {
            return errors.ToResult().ToValidationProblem();
        }

        var address = CustomerMapper.MapToAddress(dto.Address);
        var physicalInfo = CustomerMapper.MapToPhysicalInfo(dto.PhysicalInfo);
        var accommodationPreferences = CustomerMapper.MapToAccommodationPreferences(dto.AccommodationPreferences);
        var emergencyContact = CustomerMapper.MapToEmergencyContact(dto.EmergencyContact);
        var medicalInfo = CustomerMapper.MapToMedicalInfo(dto.MedicalInfo);

        customer.UpdatePersonalInfo(personalInfoResult.Value);
        customer.UpdateIdentificationInfo(identificationInfoResult.Value);
        customer.UpdateContactInfo(contactInfoResult.Value);
        customer.UpdateAddress(address);
        customer.UpdatePhysicalInfo(physicalInfo);
        customer.UpdateAccommodationPreferences(accommodationPreferences);
        customer.UpdateEmergencyContact(emergencyContact);
        customer.UpdateMedicalInfo(medicalInfo);

        await unitOfWork.SaveEntities(ct);

        return TypedResults.NoContent();
    }
}