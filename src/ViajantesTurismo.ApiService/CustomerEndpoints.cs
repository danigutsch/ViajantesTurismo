using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ViajantesTurismo.Admin.Domain;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.AdminApi.Contracts;
using ViajantesTurismo.Common;

namespace ViajantesTurismo.ApiService;

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

    private static async Task<Results<Ok<CustomerDetailsDto>, NotFound>> GetCustomerById(
        [FromRoute] int id,
        [FromServices] IQueryService queryService,
        CancellationToken ct)
    {
        var customerDto = await queryService.GetCustomerDetailsById(id, ct);
        if (customerDto is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(customerDto);
    }

    private static async Task<Results<Created<GetCustomerDto>, ValidationProblem>> CreateCustomer(
        [FromBody] CreateCustomerDto dto,
        [FromServices] ICustomerStore customerStore,
        [FromServices] IUnitOfWork unitOfWork,
        CancellationToken ct)
    {
        var personalInfo = new PersonalInfo(
            dto.PersonalInfo.FirstName!,
            dto.PersonalInfo.LastName!,
            dto.PersonalInfo.Gender!,
            dto.PersonalInfo.BirthDate!.Value.ToUniversalTime(),
            dto.PersonalInfo.Nationality!,
            dto.PersonalInfo.Profession!);

        var identificationInfo = new IdentificationInfo(
            dto.IdentificationInfo.NationalId!,
            dto.IdentificationInfo.IdNationality!);

        var contactInfo = new ContactInfo(
            dto.ContactInfo.Email!,
            dto.ContactInfo.Mobile!,
            dto.ContactInfo.Instagram,
            dto.ContactInfo.Facebook);

        var address = new Address(
            dto.Address.Street!,
            dto.Address.Complement,
            dto.Address.Neighborhood!,
            dto.Address.PostalCode!,
            dto.Address.City!,
            dto.Address.State!,
            dto.Address.Country!);

        var physicalInfo = new PhysicalInfo(
            dto.PhysicalInfo.WeightKg!.Value,
            dto.PhysicalInfo.HeightCentimeters!.Value,
            (BikeType)dto.PhysicalInfo.BikeType!.Value);

        var accommodationPreferences = new AccommodationPreferences(
            (RoomType)dto.AccommodationPreferences.RoomType!.Value,
            (BedType)dto.AccommodationPreferences.BedType!.Value,
            dto.AccommodationPreferences.CompanionId);

        var emergencyContact = new EmergencyContact(
            dto.EmergencyContact.Name!,
            dto.EmergencyContact.Mobile!);

        var medicalInfo = new MedicalInfo(
            dto.MedicalInfo.Allergies,
            dto.MedicalInfo.AdditionalInfo);

        var customer = new Customer(
            personalInfo,
            identificationInfo,
            contactInfo,
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
            Nationality = customer.PersonalInfo.Nationality
        };

        return TypedResults.Created($"/customers/{customer.Id}", customerDto);
    }

    private static async Task<Results<NoContent, NotFound>> UpdateCustomer(
        [FromRoute] int id,
        [FromBody] UpdateCustomerDto dto,
        [FromServices] ICustomerStore customerStore,
        [FromServices] IUnitOfWork unitOfWork,
        CancellationToken ct)
    {
        var customer = await customerStore.GetById(id, ct);
        if (customer is null)
        {
            return TypedResults.NotFound();
        }

        var personalInfo = new PersonalInfo(
            dto.PersonalInfo.FirstName!,
            dto.PersonalInfo.LastName!,
            dto.PersonalInfo.Gender!,
            dto.PersonalInfo.BirthDate!.Value.ToUniversalTime(),
            dto.PersonalInfo.Nationality!,
            dto.PersonalInfo.Profession!);

        var identificationInfo = new IdentificationInfo(
            dto.IdentificationInfo.NationalId!,
            dto.IdentificationInfo.IdNationality!);

        var contactInfo = new ContactInfo(
            dto.ContactInfo.Email!,
            dto.ContactInfo.Mobile!,
            dto.ContactInfo.Instagram,
            dto.ContactInfo.Facebook);

        var address = new Address(
            dto.Address.Street!,
            dto.Address.Complement,
            dto.Address.Neighborhood!,
            dto.Address.PostalCode!,
            dto.Address.City!,
            dto.Address.State!,
            dto.Address.Country!);

        var physicalInfo = new PhysicalInfo(
            dto.PhysicalInfo.WeightKg!.Value,
            dto.PhysicalInfo.HeightCentimeters!.Value,
            (BikeType)dto.PhysicalInfo.BikeType!.Value);

        var accommodationPreferences = new AccommodationPreferences(
            (RoomType)dto.AccommodationPreferences.RoomType!.Value,
            (BedType)dto.AccommodationPreferences.BedType!.Value,
            dto.AccommodationPreferences.CompanionId);

        var emergencyContact = new EmergencyContact(
            dto.EmergencyContact.Name!,
            dto.EmergencyContact.Mobile!);

        var medicalInfo = new MedicalInfo(
            dto.MedicalInfo.Allergies,
            dto.MedicalInfo.AdditionalInfo);

        customer.Update(personalInfo, identificationInfo, contactInfo, address, physicalInfo, accommodationPreferences, emergencyContact, medicalInfo);

        await unitOfWork.SaveEntities(ct);

        return TypedResults.NoContent();
    }
}