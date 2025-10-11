using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.AdminApi.Contracts;
using ViajantesTurismo.ApiService;
using ViajantesTurismo.Common.Monies;
using ViajantesTurismo.Resources;
using ViajantesTurismo.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();

builder.Services.AddOpenApi();

builder.AddNpgsqlDbContext<ApplicationDbContext>(ResourceNames.Database);

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var toursGroup = app.MapGroup("/tours")
    .WithGroupName("Tours")
    .WithTags("Tours");

toursGroup.MapPost("/", async ([FromBody] CreateTourDto tourDto, [FromServices] ApplicationDbContext dbContext, CancellationToken ct) =>
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

        dbContext.Add(tour);

        await dbContext.SaveChangesAsync(ct);

        return TypedResults.Created($"/tours/{tour.Id}", tour);
    })
    .WithName("CreateTour")
    .WithDescription("Creates a new tour.")
    .WithSummary("Creates a new tour.");

toursGroup.MapGet("/", async ([FromServices] ApplicationDbContext dbContext, CancellationToken ct) =>
    {
        var allTours = await dbContext.Tours.ToArrayAsync(ct);
        return TypedResults.Ok(allTours);
    })
    .WithName("GetTours")
    .WithDescription("Retrieves all available tours.")
    .WithSummary("Retrieves all available tours.");

app.MapDefaultEndpoints();

app.Run();
