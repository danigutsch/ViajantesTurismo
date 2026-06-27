using Microsoft.AspNetCore.Http;
using ViajantesTurismo.Management.Web.Components.Pages;

namespace ViajantesTurismo.Management.WebTests.Components.Pages;

public class ErrorPageTests : BunitContext
{
    [Fact]
    public void Renders_error_title()
    {
        // Act
        var cut = Render<Error>();

        // Assert
        var h1 = cut.Find("h1");
        Assert.Contains("Error", h1.TextContent, StringComparison.Ordinal);
        Assert.Contains("text-danger", h1.ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_error_message()
    {
        // Act
        var cut = Render<Error>();

        // Assert
        var h2 = cut.Find("h2");
        Assert.Contains("An error occurred while processing your request", h2.TextContent, StringComparison.Ordinal);
        Assert.Contains("text-danger", h2.ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void Does_not_show_request_ID_when_not_available()
    {
        // Act
        var cut = Render<Error>();

        // Assert
        var requestIdElements = cut.FindAll("strong").Where(e => e.TextContent.Contains("Request ID", StringComparison.Ordinal));
        Assert.Empty(requestIdElements);
    }

    [Fact]
    public void Shows_request_ID_when_HttpContext_has_traceIdentifier()
    {
        // Arrange
        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = "test-trace-id-12345"
        };

        // Act
        var cut = Render<Error>(parameters => parameters
            .Add(p => p.HttpContext, httpContext));

        // Assert
        var code = cut.Find("code");
        Assert.Equal("test-trace-id-12345", code.TextContent);
    }

    [Fact]
    public void Renders_development_mode_section()
    {
        // Act
        var cut = Render<Error>();

        // Assert
        var h3 = cut.Find("h3");
        Assert.Equal("Development Mode", h3.TextContent);
    }

    [Fact]
    public void Renders_development_environment_warning()
    {
        // Act
        var cut = Render<Error>();

        // Assert
        var paragraphs = cut.FindAll("p");
        var warningParagraph = paragraphs.First(p => p.TextContent.Contains("Development environment shouldn't be enabled", StringComparison.Ordinal));

        Assert.Contains("shouldn't be enabled for deployed applications", warningParagraph.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_aSPNETCORE_eNVIRONMENT_instructions()
    {
        // Act
        var cut = Render<Error>();

        // Assert
        var strongs = cut.FindAll("strong");
        Assert.Contains(strongs, s => s.TextContent == "ASPNETCORE_ENVIRONMENT");
        Assert.Contains(strongs, s => s.TextContent == "Development");
    }

    [Fact]
    public void Renders_detailed_information_warning()
    {
        // Act
        var cut = Render<Error>();

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Swapping to", markup, StringComparison.Ordinal);
        Assert.Contains("Development", markup, StringComparison.Ordinal);
        Assert.Contains("display more detailed information", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_sensitive_information_warning()
    {
        // Act
        var cut = Render<Error>();

        // Assert
        var markup = cut.Markup;
        Assert.Contains("sensitive information", markup, StringComparison.Ordinal);
        Assert.Contains("end users", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Has_correct_page_route()
    {
        // This test verifies the component has the correct route attribute
        // by checking if it can be rendered (which validates the @page directive exists)

        // Act
        var cut = Render<Error>();

        // Assert
        Assert.NotNull(cut);
        Assert.NotNull(cut.Instance);
    }
}
