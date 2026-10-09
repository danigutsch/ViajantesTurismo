using SharedKernel.AspNetCore;
using SharedKernel.Branding;
using SharedKernel.HttpClients;
using SharedKernel.MalwareScanning.ClamAv;
using SharedKernel.OpenApi;
using ViajantesTurismo.Admin.ApiService;
using ViajantesTurismo.Admin.ApiService.Bookings;
using ViajantesTurismo.Admin.ApiService.Customers;
using ViajantesTurismo.Admin.ApiService.Documents;
using ViajantesTurismo.Admin.ApiService.Errors;
using ViajantesTurismo.Admin.ApiService.Tours;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Resources;
using ViajantesTurismo.ServiceDefaults;

const string ApiRobotsTxt = "User-agent: *\nDisallow: /";

var builder = WebApplication.CreateSlimBuilder(args);

builder.WebHost.UseKestrelHttpsConfiguration();

builder.AddServiceDefaults();
builder.Services.AddHttpClientDefaults();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHttpClient<IBrandingApiClient, BrandingApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.BrandingApi}"));

builder.Services.AddProblemDetails();
builder.AddConfiguredTrustedForwardedHeaders();
builder.Services.AddAdminSecurityBaseline(builder.Configuration);
builder.Services.AddApiSecurity(
        builder.Configuration,
        builder.Environment,
        ApiAudienceNames.Admin,
        AdminAuthorization.PermissionsByRole)
    .AddPolicy(AdminAuthorization.BookingRead, policy => policy.RequirePermission(AdminAuthorization.BookingRead))
    .AddPolicy(AdminAuthorization.BookingWrite, policy => policy.RequirePermission(AdminAuthorization.BookingWrite))
    .AddPolicy(AdminAuthorization.BookingDelete, policy => policy.RequirePermission(AdminAuthorization.BookingDelete))
    .AddPolicy(AdminAuthorization.CustomerImport, policy => policy.RequirePermission(AdminAuthorization.CustomerImport))
    .AddPolicy(AdminAuthorization.CustomerRead, policy => policy.RequirePermission(AdminAuthorization.CustomerRead))
    .AddPolicy(AdminAuthorization.CustomerSensitiveRead, policy => policy.RequirePermission(AdminAuthorization.CustomerSensitiveRead))
    .AddPolicy(AdminAuthorization.CustomerWrite, policy => policy.RequirePermission(AdminAuthorization.CustomerWrite))
    .AddPolicy(AdminAuthorization.DocumentManage, policy => policy.RequirePermission(AdminAuthorization.DocumentManage))
    .AddPolicy(AdminAuthorization.DocumentationRead, policy => policy.RequirePermission(AdminAuthorization.DocumentationRead))
    .AddPolicy(AdminAuthorization.PaymentRead, policy => policy.RequirePermission(AdminAuthorization.PaymentRead))
    .AddPolicy(AdminAuthorization.PaymentWrite, policy => policy.RequirePermission(AdminAuthorization.PaymentWrite))
    .AddPolicy(AdminAuthorization.TourRead, policy => policy.RequirePermission(AdminAuthorization.TourRead))
    .AddPolicy(AdminAuthorization.TourWrite, policy => policy.RequirePermission(AdminAuthorization.TourWrite));

builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default));

builder.Services.AddAdminOpenApiDocuments();
builder.Services.AddConfiguredClamAvMalwareScanner(builder.Configuration, builder.Environment);

builder.AddApplication();
builder.AddInfrastructure();

var app = builder.Build();

app.UseExceptionHandler();

app.UseForwardedHeaders();

app.MapConfiguredOpenApi();

app.UseCors(AdminSecurityBaseline.CorsPolicyName);

app.UseApiSecurity();
app.UseRateLimiter();

app.MapToursEndpoints();
app.MapCustomerEndpoints()
    .MapCustomerImportEndpoints();
app.MapBookingEndpoints();
app.MapDocumentEndpoints();
app.MapErrorDocumentationEndpoints();
app.MapRobotsTxt(ApiRobotsTxt);

app.MapDefaultEndpoints();

await app.RunAsync();
