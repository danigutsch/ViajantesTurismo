using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using PandaBear.Testing.Http;
using ViajantesTurismo.Branding.Contracts.Http;

namespace ViajantesTurismo.Management.WebBrowserTests;

/// <summary>
/// Stands in for the Branding API: serves one settings document and answers every save with a
/// <c>400 application/problem+json</c> validation problem for the given field message.
/// </summary>
internal static class BrandingSettingsStub
{
    private static readonly Guid SourceEpoch = Guid.Parse("0f0f0f0f-0f0f-4f0f-8f0f-0f0f0f0f0f0f");

    /// <summary>Creates the stub handler.</summary>
    /// <param name="field">The model field the problem names.</param>
    /// <param name="message">The field message the problem carries.</param>
    public static DelegateHttpMessageHandler Create(string field, string message) => new(request =>
    {
        if (request.Method == HttpMethod.Put)
        {
            var problem = JsonSerializer.Serialize(new
            {
                type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                title = "One or more validation errors occurred.",
                status = 400,
                errors = new Dictionary<string, string[]> { [field] = [message] }
            });
            return new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(problem, Encoding.UTF8, "application/problem+json")
            };
        }

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(
                new BrandingManagementSettingsDto
                {
                    BrandName = "Viajantes Turismo",
                    PrimaryColor = "#0F766E",
                    AccentColor = "#F97316",
                    BackgroundColor = "#FFFBF5",
                    TextColor = "#1F2937",
                    HeadingFontFamily = "Georgia",
                    BodyFontFamily = "system-ui",
                    SourceEpoch = SourceEpoch,
                    SourceRevision = 1
                },
                options: new JsonSerializerOptions(JsonSerializerDefaults.Web))
        };
        response.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue(BrandingEntityTags.Format(SourceEpoch, 1));
        return response;
    });
}
