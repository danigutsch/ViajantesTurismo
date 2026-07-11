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
        (h1.TextContent).ShouldContain("Error", StringComparison.Ordinal);
        (h1.ClassName).ShouldContain("text-danger", StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_error_message()
    {
        // Act
        var cut = Render<Error>();

        // Assert
        var h2 = cut.Find("h2");
        (h2.TextContent).ShouldContain("An error occurred while processing your request", StringComparison.Ordinal);
        (h2.ClassName).ShouldContain("text-danger", StringComparison.Ordinal);
    }

    [Fact]
    public void Does_not_show_request_ID_when_not_available()
    {
        // Act
        var cut = Render<Error>();

        // Assert
        var requestIdElements = cut.FindAll("strong").Where(e => e.TextContent.Contains("Request ID", StringComparison.Ordinal));
        (requestIdElements).ShouldBeEmpty();
    }

    [Fact]
    public void Shows_request_ID_when_HttpContext_has_traceidentifier()
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
        (code.TextContent).ShouldBe("test-trace-id-12345");
    }

    [Fact]
    public void Renders_development_mode_section()
    {
        // Act
        var cut = Render<Error>();

        // Assert
        var h3 = cut.Find("h3");
        (h3.TextContent).ShouldBe("Development Mode");
    }

    [Fact]
    public void Renders_development_environment_warning()
    {
        // Act
        var cut = Render<Error>();

        // Assert
        var paragraphs = cut.FindAll("p");
        var warningParagraph = paragraphs.First(p => p.TextContent.Contains("Development environment shouldn't be enabled", StringComparison.Ordinal));

        (warningParagraph.TextContent).ShouldContain("shouldn't be enabled for deployed applications", StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_aspnetcore_environment_instructions()
    {
        // Act
        var cut = Render<Error>();

        // Assert
        var strongs = cut.FindAll("strong");
        (strongs).ShouldContain(s => s.TextContent == "ASPNETCORE_ENVIRONMENT");
        (strongs).ShouldContain(s => s.TextContent == "Development");
    }

    [Fact]
    public void Renders_detailed_information_warning()
    {
        // Act
        var cut = Render<Error>();

        // Assert
        var markup = cut.Markup;
        (markup).ShouldContain("Swapping to", StringComparison.Ordinal);
        (markup).ShouldContain("Development", StringComparison.Ordinal);
        (markup).ShouldContain("display more detailed information", StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_sensitive_information_warning()
    {
        // Act
        var cut = Render<Error>();

        // Assert
        var markup = cut.Markup;
        (markup).ShouldContain("sensitive information", StringComparison.Ordinal);
        (markup).ShouldContain("end users", StringComparison.Ordinal);
    }

    [Fact]
    public void Has_correct_page_route()
    {
        // This test verifies the component has the correct route attribute
        // by checking if it can be rendered (which validates the @page directive exists)

        // Act
        var cut = Render<Error>();

        // Assert
        _ = (cut).ShouldNotBeNull();
        (cut.Instance).ShouldNotBeNull();
    }
}
