using PandaBear.Mediator;
using PandaBear.AspNetCore;
using PandaBear.HttpCaching.AspNetCore;
using PandaBear.HttpClients;
using PandaBear.MalwareScanning.ClamAv;
using PandaBear.OpenApi;
using ViajantesTurismo.Admin.ApiService;
using ViajantesTurismo.Admin.ApiService.Bookings;
using ViajantesTurismo.Admin.ApiService.Customers;
using ViajantesTurismo.Admin.ApiService.Documents;
using ViajantesTurismo.Admin.ApiService.Payments;
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

builder.Services.AddProblemDetails();
builder.Services.AddUnhandledExceptionHandler();
builder.Services.AddSecurityEventTelemetry();
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
    .AddPolicy(AdminAuthorization.DocumentHold, policy => policy.RequirePermission(AdminAuthorization.DocumentHold))
    .AddPolicy(AdminAuthorization.DocumentationRead, policy => policy.RequirePermission(AdminAuthorization.DocumentationRead))
    .AddPolicy(AdminAuthorization.PaymentManage, policy => policy.RequirePermission(AdminAuthorization.PaymentManage))
    .AddPolicy(AdminAuthorization.PaymentRead, policy => policy.RequirePermission(AdminAuthorization.PaymentRead))
    .AddPolicy(AdminAuthorization.PaymentWrite, policy => policy.RequirePermission(AdminAuthorization.PaymentWrite))
    .AddPolicy(AdminAuthorization.TourRead, policy => policy.RequirePermission(AdminAuthorization.TourRead))
    .AddPolicy(AdminAuthorization.TourWrite, policy => policy.RequirePermission(AdminAuthorization.TourWrite));

builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default));

builder.Services.AddAdminOpenApiDocuments();
builder.Services.AddConfiguredClamAvMalwareScanner(builder.Configuration, builder.Environment);

builder.AddApplication();
builder.Services.AddSharedKernelMediator();
builder.AddInfrastructure();

builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.TypeInfoResolverChain.Insert(0, ViajantesTurismo.Admin.Contracts.Application.PaymentAccountContractJsonContext.Default));

builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.TypeInfoResolverChain.Insert(0, ViajantesTurismo.Admin.Contracts.Application.PaymentFinancialContractJsonContext.Default));

var app = builder.Build();

app.UseExceptionHandler();

app.UseForwardedHeaders();

app.MapConfiguredOpenApi();

app.UseCors(AdminSecurityBaseline.CorsPolicyName);

app.UseNoStoreResponses();

app.UseApiSecurity();
app.UseRateLimiter();

app.MapToursEndpoints();
app.MapCustomerEndpoints()
    .MapCustomerImportEndpoints();
app.MapBookingEndpoints();
app.MapDocumentEndpoints();
app.MapDocumentHoldEndpoints();
app.MapPaymentAccountEndpoints();
app.MapBookingFinancialEndpoints();
app.MapPaymentInstructionEndpoints();
app.MapErrorDocumentationEndpoints();
app.MapRobotsTxt(ApiRobotsTxt);

app.MapDefaultEndpoints();

await app.RunAsync();
