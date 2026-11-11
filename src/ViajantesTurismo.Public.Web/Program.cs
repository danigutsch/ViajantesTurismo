using System.Globalization;
using PandaBear.Branding;
using PandaBear.HttpClients;
using Microsoft.Extensions.Options;
using PandaBear.AspNetCore;
using PandaBear.AspNetCore.Localization;
using PandaBear.HttpCaching.AspNetCore;
using ViajantesTurismo.Public.Web;
using ViajantesTurismo.Resources;
using PandaBear.ServiceDefaults;
using ViajantesTurismo.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddSharedKernelServiceDefaults(ViajantesTurismoServiceDefaults.Configure).AddViajantesTurismoTelemetry();
builder.Services.AddHttpClientDefaults(PublicWebSecurityBaseline.ConfigureCatalogResilience);
builder.AddConfiguredTrustedForwardedHeaders();
builder.Services.AddPublicWebSecurityBaseline();
builder.Services.AddHttpClient<IPublicCatalogApiClient, PublicCatalogApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.CatalogApi}"))
    .AddForwardedClientIp();
builder.Services.AddHttpClient<IBrandingApiClient, BrandingApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.CatalogApi}"));
builder.Services.AddRazorComponents();
builder.Services.AddOptions<PublicWebSitemapOptions>()
    .BindConfiguration(PublicWebSitemapOptions.SectionName)
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<PublicWebSitemapOptions>, PublicWebSitemapOptionsValidator>();

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
