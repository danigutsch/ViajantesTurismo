using System.Globalization;
using PandaBear.AspNetCore;
using PandaBear.AspNetCore.Localization;
using PandaBear.AspNetCore.Seo;
using PandaBear.HttpCaching.AspNetCore;
using ViajantesTurismo.Public.Web;
using ViajantesTurismo.Public.Web.Branding;
using ViajantesTurismo.Public.Web.Catalog;
using PandaBear.ServiceDefaults;
using ViajantesTurismo.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddSharedKernelServiceDefaults(static options =>
{
    ViajantesTurismoServiceDefaults.Configure(options);
    options.ConfigureResilience = PublicCatalogClientServiceCollectionExtensions.ConfigureResilience;
}).AddViajantesTurismoTelemetry();
builder.AddConfiguredTrustedForwardedHeaders();
builder.Services.AddPublicWebSecurityBaseline();
builder.Services.AddPublicCatalogClient();
builder.Services.AddPublicBranding();
builder.Services.AddRazorComponents();
builder.Services.AddCanonicalOrigin(PublicSiteLinks.CanonicalOriginKey);

var maintenanceEnabled = builder.AddHttpMaintenanceReadiness();
var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseDefaultPublicCaching(PublicWebHttpCache.PublishedContentFreshness);
app.UsePublicWebSecurityHeaders();
app.UseHttpMaintenance(maintenanceEnabled, browser: true);
app.UsePermanentRedirects(PublicCultureRedirects.Resolve);
app.UseCulturePathRedirect(PublicSiteCultures.Supported, excludedPrefixes: PublicSiteCultures.UnlocalizedPrefixes);
app.UseRequestLocalization(options =>
{
    CultureInfo[] cultures = [.. PublicSiteCultures.Supported.Cultures.Select(culture => CultureInfo.GetCultureInfo(culture.Value))];
    options.DefaultRequestCulture = new(PublicSiteCultures.Supported.Default.Value);
    options.SupportedCultures = cultures;
    options.SupportedUICultures = cultures;
    options.RequestCultureProviders = [new PathCultureRequestCultureProvider(PublicSiteCultures.Supported)];
});
app.UseRateLimiter();
app.MapPublicWebEndpoints();

app.MapSharedKernelDefaultEndpoints();

await app.RunAsync();
