using ViajantesTurismo.Admin.ApiService;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddSingleton(TimeProvider.System);

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
app.MapBookingEndpoints();

app.MapDefaultEndpoints();

app.Run();
