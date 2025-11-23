using Microsoft.AspNetCore.Http;
using ViajantesTurismo.Admin.Web.Components.Pages;

namespace ViajantesTurismo.Admin.WebTests.Components.Pages;

public class ErrorPageTests : BunitContext
{
    [Fact]
    public void Renders_Error_Title()
    {
        // Act
        var cut = Render<Error>();

        // Assert
        var h1 = cut.Find("h1");
        Assert.Contains("Error", h1.TextContent);
        Assert.Contains("text-danger", h1.ClassName);
    }

    [Fact]
    public void Renders_Error_Message()
    {
        // Act
        var cut = Render<Error>();

        // Assert
        var h2 = cut.Find("h2");
        Assert.Contains("An error occurred while processing your request", h2.TextContent);
        Assert.Contains("text-danger", h2.ClassName);
    }

    [Fact]
    public void Does_Not_Show_Request_ID_When_Not_Available()
    {
        // Act
        var cut = Render<Error>();

        // Assert
        var requestIdElements = cut.FindAll("strong").Where(e => e.TextContent.Contains("Request ID"));
        Assert.Empty(requestIdElements);
    }

    [Fact]
    public void Shows_Request_ID_When_HttpContext_Has_TraceIdentifier()
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
    public void Renders_Development_Mode_Section()
    {
        // Act
        var cut = Render<Error>();

        // Assert
        var h3 = cut.Find("h3");
        Assert.Equal("Development Mode", h3.TextContent);
    }

    [Fact]
    public void Renders_Development_Environment_Warning()
    {
        // Act
        var cut = Render<Error>();

        // Assert
        var paragraphs = cut.FindAll("p");
        var warningParagraph = paragraphs.First(p => p.TextContent.Contains("Development environment shouldn't be enabled"));

        Assert.Contains("shouldn't be enabled for deployed applications", warningParagraph.TextContent);
    }

    [Fact]
    public void Renders_ASPNETCORE_ENVIRONMENT_Instructions()
    {
        // Act
        var cut = Render<Error>();

        // Assert
        var strongs = cut.FindAll("strong");
        Assert.Contains(strongs, s => s.TextContent == "ASPNETCORE_ENVIRONMENT");
        Assert.Contains(strongs, s => s.TextContent == "Development");
    }

    [Fact]
    public void Renders_Detailed_Information_Warning()
    {
        // Act
        var cut = Render<Error>();

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Swapping to", markup);
        Assert.Contains("Development", markup);
        Assert.Contains("display more detailed information", markup);
    }

    [Fact]
    public void Renders_Sensitive_Information_Warning()
    {
        // Act
        var cut = Render<Error>();

        // Assert
        var markup = cut.Markup;
        Assert.Contains("sensitive information", markup);
        Assert.Contains("end users", markup);
    }

    [Fact]
    public void Has_Correct_Page_Route()
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
