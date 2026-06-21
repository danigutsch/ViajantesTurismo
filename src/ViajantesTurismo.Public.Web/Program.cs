using ViajantesTurismo.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.MapGet("/", () => Results.Content(
    """
    <!doctype html>
    <html lang="en">
    <head>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1">
        <title>Viajantes Turismo</title>
    </head>
    <body>
        <main>
            <h1>Viajantes Turismo</h1>
            <p>Public travel discovery experience coming soon.</p>
        </main>
    </body>
    </html>
    """,
    "text/html"));

app.MapGet("/Error", () => Results.Problem())
    .ExcludeFromDescription();

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
