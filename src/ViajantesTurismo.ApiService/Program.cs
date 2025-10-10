using ViajantesTurismo.ApiService;
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

Tour[] tours =
[
    new("City Highlights", DateTime.Now.AddDays(1), DateTime.Now.AddDays(3)),
    new("Historical Landmarks", DateTime.Now.AddDays(4), DateTime.Now.AddDays(6)),
    new("Cultural Experience", DateTime.Now.AddDays(7), DateTime.Now.AddDays(10)),
    new("Nature and Adventure", DateTime.Now.AddDays(11), DateTime.Now.AddDays(15)),
    new("Food and Wine Tour", DateTime.Now.AddDays(16), DateTime.Now.AddDays(20))
];

app.MapGet("/tours", () => tours)
    .WithName("GetTours");

app.MapDefaultEndpoints();

app.Run();