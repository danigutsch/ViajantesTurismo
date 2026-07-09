using SharedKernel.HttpClients;
using ViajantesTurismo.Catalog.Contracts;
using ViajantesTurismo.Public.Web;
using ViajantesTurismo.Resources;
using ViajantesTurismo.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHttpClientDefaults();
builder.Services.AddHttpClient<IPublicCatalogApiClient, PublicCatalogApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.CatalogApi}"));
builder.Services.AddRazorComponents();
builder.Services.AddPublicWebOutputCache();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UsePublicWebCacheHeaders();

app.UseOutputCache();

app.MapPublicWebEndpoints();

app.MapDefaultEndpoints();

await app.RunAsync();
