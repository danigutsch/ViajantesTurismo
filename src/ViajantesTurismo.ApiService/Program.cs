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

app.MapDefaultEndpoints();

app.Run();
