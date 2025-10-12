using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ViajantesTurismo.Admin.Domain;
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

        return app;
    }

    private static async Task<Ok<IReadOnlyList<GetCustomerDto>>> GetAllCustomers(
        [FromServices] IQueryService queryService,
        CancellationToken ct)
    {
        var customers = await queryService.GetAllCustomers(ct);
        return TypedResults.Ok(customers);
    }

    private static async Task<Results<Ok<GetCustomerDto>, NotFound>> GetCustomerById(
        [FromRoute] int id,
        [FromServices] ICustomerStore customerStore,
        CancellationToken ct)
    {
        var customer = await customerStore.GetById(id, ct);
        if (customer is null)
        {
            return TypedResults.NotFound();
        }

        var customerDto = new GetCustomerDto
        {
            Id = customer.Id,
            FirstName = customer.PersonalInfo.FirstName,
            LastName = customer.PersonalInfo.LastName,
            Email = customer.ContactInfo.Email,
            Mobile = customer.ContactInfo.Mobile,
            Nationality = customer.PersonalInfo.Nationality
        };

        return TypedResults.Ok(customerDto);
    }

    private static async Task<Results<Created<GetCustomerDto>, ValidationProblem>> CreateCustomer(
        [FromBody] CreateCustomerDto dto,
        [FromServices] ICustomerStore customerStore,
        [FromServices] IUnitOfWork unitOfWork,
        CancellationToken ct)
    {
        var personalInfo = new PersonalInfo
        {
            FirstName = dto.PersonalInfo.FirstName!,
            LastName = dto.PersonalInfo.LastName!,
            BirthDate = dto.PersonalInfo.BirthDate!.Value,
            Gender = dto.PersonalInfo.Gender!,
            Nationality = dto.PersonalInfo.Nationality!,
            Profession = dto.PersonalInfo.Profession!
        };

        var identificationInfo = new IdentificationInfo
        {
            NationalId = dto.IdentificationInfo.NationalId!,
            IdNationality = dto.IdentificationInfo.IdNationality!
        };

        var contactInfo = new ContactInfo
        {
            Email = dto.ContactInfo.Email!,
            Mobile = dto.ContactInfo.Mobile!,
            Instagram = dto.ContactInfo.Instagram,
            Facebook = dto.ContactInfo.Facebook
        };

        var address = new Address
        {
            Street = dto.Address.Street!,
            Complement = dto.Address.Complement,
            Neighborhood = dto.Address.Neighborhood!,
            PostalCode = dto.Address.PostalCode!,
            City = dto.Address.City!,
            State = dto.Address.State!,
            Country = dto.Address.Country!
        };

        var physicalInfo = new PhysicalInfo
        {
            WeightKg = dto.PhysicalInfo.WeightKg!.Value,
            HeightCentimeters = dto.PhysicalInfo.HeightCentimeters!.Value,
            BikeType = (BikeType)dto.PhysicalInfo.BikeType!.Value
        };

        var accommodationPreferences = new AccommodationPreferences
        {
            RoomType = (RoomType)dto.AccommodationPreferences.RoomType!.Value,
            BedType = (BedType)dto.AccommodationPreferences.BedType!.Value,
            CompanionId = dto.AccommodationPreferences.CompanionId
        };

        var emergencyContact = new EmergencyContact
        {
            Name = dto.EmergencyContact.Name!,
            Mobile = dto.EmergencyContact.Mobile!
        };

        var medicalInfo = new MedicalInfo
        {
            Allergies = dto.MedicalInfo.Allergies,
            AdditionalInfo = dto.MedicalInfo.AdditionalInfo
        };

        var customer = new Customer
        {
            PersonalInfo = personalInfo,
            IdentificationInfo = identificationInfo,
            ContactInfo = contactInfo,
            Address = address,
            PhysicalInfo = physicalInfo,
            AccommodationPreferences = accommodationPreferences,
            EmergencyContact = emergencyContact,
            MedicalInfo = medicalInfo
        };

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
}
