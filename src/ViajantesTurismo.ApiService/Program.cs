using Microsoft.AspNetCore.Mvc;
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

GetTourDto[] tours =
[
    new()
    {
        Id = 1,
        Identifier = "CITY001",
        Name = "City Highlights",
        StartDate = DateTime.Now.AddDays(1),
        EndDate = DateTime.Now.AddDays(3),
        Price = 1500,
        SingleRoomSupplementPrice = 300,
        RegularBikePrice = 100,
        EBikePrice = 200,
        Currency = CurrencyDto.Real,
        IncludedServices = ["Hotel", "Breakfast", "City Tour"]
    },
    new()
    {
        Id = 2,
        Identifier = "HIST002",
        Name = "Historical Landmarks",
        StartDate = DateTime.Now.AddDays(4),
        EndDate = DateTime.Now.AddDays(6),
        Price = 2000,
        SingleRoomSupplementPrice = 400,
        RegularBikePrice = 150,
        EBikePrice = 250,
        Currency = CurrencyDto.Euro,
        IncludedServices = ["Hotel", "Breakfast", "Museum Tickets"]
    },
    new()
    {
        Id = 3,
        Identifier = "CULT001",
        Name = "Cultural Experience",
        StartDate = DateTime.Now.AddDays(7),
        EndDate = DateTime.Now.AddDays(10),
        Price = 1800,
        SingleRoomSupplementPrice = 350,
        RegularBikePrice = 120,
        EBikePrice = 220,
        Currency = CurrencyDto.UsDollar,
        IncludedServices = ["Hotel", "Breakfast", "Cultural Show"]
    },
    new()
    {
        Id = 4,
        Identifier = "NATR001",
        Name = "Nature and Adventure",
        StartDate = DateTime.Now.AddDays(11),
        EndDate = DateTime.Now.AddDays(15),
        Price = 2200,
        SingleRoomSupplementPrice = 450,
        RegularBikePrice = 180,
        EBikePrice = 280,
        Currency = CurrencyDto.Real,
        IncludedServices = ["Hotel", "Breakfast", "Hiking Tour"]
    },
    new()
    {
        Id = 5,
        Identifier = "FOWI003",
        Name = "Food and Wine Tour",
        StartDate = DateTime.Now.AddDays(16),
        EndDate = DateTime.Now.AddDays(20),
        Price = 2500,
        SingleRoomSupplementPrice = 500,
        RegularBikePrice = 200,
        EBikePrice = 300,
        Currency = CurrencyDto.Euro,
        IncludedServices = ["Hotel", "Breakfast", "Wine Tasting"]
    }
];

var toursGroup = app.MapGroup("/tours")
    .WithGroupName("Tours")
    .WithTags("Tours");

toursGroup.MapPost("/", async ([FromBody] CreateTourDto tourDto, [FromServices] ApplicationDbContext dbContext) =>
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

        await dbContext.SaveChangesAsync();

        return TypedResults.Created($"/tours/{tour.Id}", tour);
    })
    .WithName("CreateTour")
    .WithDescription("Creates a new tour.")
    .WithSummary("Creates a new tour.");

toursGroup.MapGet("/", () =>
    {
        var allTours = tours.ToArray();
        return allTours;
    })
    .WithName("GetTours")
    .WithDescription("Retrieves all available tours.")
    .WithSummary("Retrieves all available tours.");

app.MapDefaultEndpoints();

app.Run();