using ViajantesTurismo.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
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

internal sealed record Tour(string Name, DateTime StartDate, DateTime EndDate);
