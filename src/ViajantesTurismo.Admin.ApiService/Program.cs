using SharedKernel.AspNetCore;
using ViajantesTurismo.Admin.ApiService;
using ViajantesTurismo.Admin.ApiService.Bookings;
using ViajantesTurismo.Admin.ApiService.Customers;
using ViajantesTurismo.Admin.ApiService.Errors;
using ViajantesTurismo.Admin.ApiService.Tours;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.ServiceDefaults;

const string ApiRobotsTxt = "User-agent: *\nDisallow: /";

var builder = WebApplication.CreateSlimBuilder(args);

builder.WebHost.UseKestrelHttpsConfiguration();

builder.AddServiceDefaults();

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddProblemDetails();
builder.Services.AddConfiguredTrustedForwardedHeaders(builder.Configuration.GetSection("Security:ForwardedHeaders"));
builder.Services.AddAdminSecurityBaseline(builder.Configuration);

builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default));

builder.Services.AddAdminOpenApiDocuments();

builder.AddApplication();
builder.AddInfrastructure();

var app = builder.Build();

app.UseExceptionHandler();

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(AdminSecurityBaseline.CorsPolicyName);

app.UseRateLimiter();

app.MapToursEndpoints();
app.MapCustomerEndpoints()
    .MapCustomerImportEndpoints();
app.MapBookingEndpoints();
app.MapErrorDocumentationEndpoints();
app.MapRobotsTxt(ApiRobotsTxt);

app.MapDefaultEndpoints();

await app.RunAsync();
