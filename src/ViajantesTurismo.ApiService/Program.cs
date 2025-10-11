using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ViajantesTurismo.Admin.Domain;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.AdminApi.Contracts;
using ViajantesTurismo.Common.Monies;
using ViajantesTurismo.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();

builder.Services.AddOpenApi();

builder.AddInfrastructure();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var toursGroup = app.MapGroup("/tours")
    .WithGroupName("Tours")
    .WithTags("Tours");

toursGroup.MapPost("/", async ([FromBody] CreateTourDto tourDto, [FromServices] ITourStore tourStore, [FromServices] IUnitOfWork unitOfWork, CancellationToken ct) =>
    {
        var currency = (Currency)tourDto.Currency;

        var tour = new Tour(
            tourDto.Identifier,
            tourDto.Name,
            tourDto.StartDate,
            tourDto.EndDate,
            tourDto.Price,
            tourDto.SingleRoomSupplementPrice,
            tourDto.RegularBikePrice,
            tourDto.EBikePrice,
            currency,
            [.. tourDto.IncludedServices]
        );

        tourStore.Add(tour);

        await unitOfWork.SaveEntities(ct);

        return TypedResults.Created($"/tours/{tour.Id}", tour);
    })
    .WithName("CreateTour")
    .WithDescription("Creates a new tour.")
    .WithSummary("Creates a new tour.");

toursGroup.MapGet("/", async ([FromServices] IQueryService queryService, CancellationToken ct) =>
    {
        var allTours = await queryService.GetAllTours(ct);
        return TypedResults.Ok(allTours);
    })
    .WithName("GetTours")
    .WithDescription("Retrieves all available tours.")
    .WithSummary("Retrieves all available tours.");

toursGroup.MapGet("/{id:int}", async Task<Results<Ok<GetTourDto>, NotFound>> ([FromRoute] int id, [FromServices] ITourStore tourStore, CancellationToken ct) =>
    {
        var tour = await tourStore.GetById(id, ct);
        if (tour is null)
        {
            return TypedResults.NotFound();
        }

        var tourDto = new GetTourDto
        {
            Id = tour.Id,
            Identifier = tour.Identifier,
            Name = tour.Name,
            StartDate = tour.StartDate,
            EndDate = tour.EndDate,
            Price = tour.Price,
            SingleRoomSupplementPrice = tour.SingleRoomSupplementPrice,
            RegularBikePrice = tour.RegularBikePrice,
            EBikePrice = tour.EBikePrice,
            Currency = (CurrencyDto)tour.Currency,
            IncludedServices = tour.IncludedServices.ToList()
        };

        return TypedResults.Ok(tourDto);
    })
    .WithName("GetTourById")
    .WithDescription("Retrieves a tour by its ID.")
    .WithSummary("Retrieves a tour by its ID.");

toursGroup.MapPut("/{id:int}", async Task<Results<NoContent, NotFound>> (int id, [FromBody] UpdateTourDto tourDto, [FromServices] ITourStore tourStore, [FromServices] IUnitOfWork unitOfWork, CancellationToken ct) =>
    {
        var tour = await tourStore.GetById(id, ct);
        if (tour is null)
        {
            return TypedResults.NotFound();
        }

        var currency = (Currency)tourDto.Currency;

        tour.Update(
            tourDto.Identifier,
            tourDto.Name,
            tourDto.StartDate,
            tourDto.EndDate,
            tourDto.Price,
            tourDto.SingleRoomSupplementPrice,
            tourDto.RegularBikePrice,
            tourDto.EBikePrice,
            currency,
            [.. tourDto.IncludedServices]
        );

        tourStore.Update(tour);
        await unitOfWork.SaveEntities(ct);

        return TypedResults.NoContent();
    })
    .WithName("UpdateTour")
    .WithDescription("Updates an existing tour.")
    .WithSummary("Updates an existing tour.");

app.MapDefaultEndpoints();

app.Run();
