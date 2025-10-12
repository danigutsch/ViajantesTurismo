using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.ApiService;
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

app.MapToursEndpoints();
app.MapCustomerEndpoints();

app.MapDefaultEndpoints();

app.Run();