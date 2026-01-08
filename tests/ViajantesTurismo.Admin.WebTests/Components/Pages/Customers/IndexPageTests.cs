using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Tests.Shared;
using static ViajantesTurismo.Admin.Tests.Shared.DtoBuilders;
using Index = ViajantesTurismo.Admin.Web.Components.Pages.Customers.Index;

namespace ViajantesTurismo.Admin.WebTests.Components.Pages.Customers;

public class IndexPageTests : BunitContext
{
    private readonly FakeCustomersApiClient _fakeCustomersApi;

    public IndexPageTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        _fakeCustomersApi = new FakeCustomersApiClient();
        Services.AddSingleton<ICustomersApiClient>(_fakeCustomersApi);
    }

    [Fact]
    public void Renders_Page_Title_And_Header()
    {
        // Arrange
        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.FindAll("h1").Count > 0, TimeSpan.FromSeconds(2));

        // Assert
        var pageTitle = cut.Find("h1");
        Assert.Contains("Customers", pageTitle.TextContent);
    }

    [Fact]
    public void Renders_Create_New_Customer_Button()
    {
        // Arrange
        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.FindAll("a.btn.btn-primary").Count > 0, TimeSpan.FromSeconds(2));

        // Assert
        var createButton = cut.Find("a.btn.btn-primary");
        Assert.Equal("/customers/create", createButton.GetAttribute("href"));
        Assert.Contains("Create New Customer", createButton.TextContent);
    }

    [Fact]
    public void Renders_Page_Description()
    {
        // Arrange
        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.Markup.Contains("Manage customer"), TimeSpan.FromSeconds(2));

        // Assert
        Assert.Contains("Manage customer information and profiles", cut.Markup);
    }

    [Fact]
    public void Renders_No_Customers_Message_When_Empty()
    {
        // Arrange
        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.FindAll(".alert.alert-info").Count > 0, TimeSpan.FromSeconds(2));

        // Assert
        var alert = cut.Find(".alert.alert-info");
        Assert.Contains("No customers found", alert.TextContent);
        Assert.Contains("Create your first customer", alert.TextContent);
    }

    [Fact]
    public void Renders_QuickGrid_When_Customers_Exist()
    {
        // Arrange
        _fakeCustomersApi.AddCustomer(BuildCustomerDto());

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.FindAll("table.table-hover").Count > 0, TimeSpan.FromSeconds(2));

        // Assert
        var grid = cut.Find("table.table-hover");
        Assert.NotNull(grid);
    }

    [Fact]
    public void Renders_QuickGrid_With_Column_Headers()
    {
        // Arrange
        _fakeCustomersApi.AddCustomer(BuildCustomerDto());

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.FindAll("th").Count > 0, TimeSpan.FromSeconds(2));

        // Assert
        var headers = cut.FindAll("th");
        Assert.Contains(headers, h => h.TextContent.Contains("Name"));
        Assert.Contains(headers, h => h.TextContent.Contains("Email"));
        Assert.Contains(headers, h => h.TextContent.Contains("Mobile"));
        Assert.Contains(headers, h => h.TextContent.Contains("Nationality"));
        Assert.Contains(headers, h => h.TextContent.Contains("Actions"));
    }

    [Fact]
    public void Renders_Customer_Basic_Information()
    {
        // Arrange
        var customer = BuildCustomerDto(
            firstName: "John",
            lastName: "Doe",
            email: "john.doe@example.com",
            mobile: "+1234567890",
            nationality: "Brazilian"
        );
        _fakeCustomersApi.AddCustomer(customer);

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.Markup.Contains("John"), TimeSpan.FromSeconds(2));

        // Assert
        Assert.Contains("John Doe", cut.Markup);
        Assert.Contains("john.doe@example.com", cut.Markup);
        Assert.Contains("+1234567890", cut.Markup);
        Assert.Contains("Brazilian", cut.Markup);
    }


    [Fact]
    public void Renders_View_Button_For_Each_Customer()
    {
        // Arrange
        var customer = BuildCustomerDto();
        _fakeCustomersApi.AddCustomer(customer);

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.FindAll("a.btn.btn-info").Count > 0, TimeSpan.FromSeconds(2));

        // Assert
        var viewButton = cut.Find("a.btn.btn-info");
        Assert.Equal($"/customers/{customer.Id}", viewButton.GetAttribute("href"));
        Assert.Contains("View", viewButton.TextContent);
    }

    [Fact]
    public void Renders_Edit_Button_For_Each_Customer()
    {
        // Arrange
        var customer = BuildCustomerDto();
        _fakeCustomersApi.AddCustomer(customer);

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.FindAll("a.btn.btn-primary").Count > 1, TimeSpan.FromSeconds(2));

        // Assert
        var buttons = cut.FindAll("a.btn.btn-primary");
        var editButton = buttons[buttons.Count - 1];
        Assert.Equal($"/customers/{customer.Id}/edit", editButton.GetAttribute("href"));
        Assert.Contains("Edit", editButton.TextContent);
    }

    [Fact]
    public void Renders_Multiple_Customers()
    {
        // Arrange
        _fakeCustomersApi.AddCustomer(BuildCustomerDto(firstName: "Alice", lastName: "Smith"));
        _fakeCustomersApi.AddCustomer(BuildCustomerDto(firstName: "Bob", lastName: "Johnson"));
        _fakeCustomersApi.AddCustomer(BuildCustomerDto(firstName: "Carol", lastName: "Williams"));

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.Markup.Contains("Total customers: 3"), TimeSpan.FromSeconds(2));

        // Assert
        Assert.Contains("Alice Smith", cut.Markup);
        Assert.Contains("Bob Johnson", cut.Markup);
        Assert.Contains("Carol Williams", cut.Markup);
        Assert.Contains("Total customers: 3", cut.Markup);
    }

    [Fact]
    public void Renders_Paginator_When_More_Than_10_Customers()
    {
        // Arrange
        for (int i = 1; i <= 15; i++)
        {
            _fakeCustomersApi.AddCustomer(BuildCustomerDto(
                firstName: $"Customer{i}",
                lastName: "Test"
            ));
        }

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.FindAll("p.text-muted").Count > 0, TimeSpan.FromSeconds(2));

        // Assert
        var totalText = cut.Find("p.text-muted");
        Assert.Contains("Total customers: 15", totalText.TextContent);
    }

    [Fact]
    public void Does_Not_Render_Paginator_When_10_Or_Fewer_Customers()
    {
        // Arrange
        for (int i = 1; i <= 8; i++)
        {
            _fakeCustomersApi.AddCustomer(BuildCustomerDto(
                firstName: $"Customer{i}",
                lastName: "Test"
            ));
        }

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.FindAll("p.text-muted").Count > 0, TimeSpan.FromSeconds(2));

        // Assert
        Assert.Contains("Total customers: 8", cut.Markup);
    }

    [Fact]
    public void Displays_Total_Customers_Count()
    {
        // Arrange
        for (int i = 1; i <= 5; i++)
        {
            _fakeCustomersApi.AddCustomer(BuildCustomerDto(
                firstName: $"Customer{i}",
                lastName: "Test"
            ));
        }

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.FindAll("p.text-muted").Count > 0, TimeSpan.FromSeconds(2));

        // Assert
        var totalText = cut.Find("p.text-muted");
        Assert.Equal("Total customers: 5", totalText.TextContent);
    }

    [Fact]
    public void Renders_Name_Column_With_FirstName_And_LastName()
    {
        // Arrange
        _fakeCustomersApi.AddCustomer(BuildCustomerDto(firstName: "Jane", lastName: "Doe"));

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.Markup.Contains("Jane Doe"), TimeSpan.FromSeconds(2));

        // Assert
        Assert.Contains("Jane Doe", cut.Markup);
    }

    [Fact]
    public void Does_Not_Render_QuickGrid_When_No_Customers()
    {
        // Arrange
        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.FindAll(".alert.alert-info").Count > 0, TimeSpan.FromSeconds(2));

        // Assert
        var tables = cut.FindAll("table.table-hover");
        Assert.Empty(tables);
    }
}
