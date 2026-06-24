using ViajantesTurismo.Public.Web.Components;
using ViajantesTurismo.Public.Web;
using ViajantesTurismo.Resources;
using ViajantesTurismo.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHttpClient<IPublicCatalogApiClient, PublicCatalogApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.CatalogApi}"));
builder.Services.AddRazorComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapGet("/Error", () => Results.Problem())
    .ExcludeFromDescription();

app.MapStaticAssets();

app.MapRazorComponents<App>();

app.MapDefaultEndpoints();

await app.RunAsync();

/// <summary>
/// Exposes the public web application entry point for integration tests.
/// </summary>
internal partial class Program
{
    protected Program()
    {
    }
}
