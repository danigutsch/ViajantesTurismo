using Index = ViajantesTurismo.Management.Web.Components.Pages.Customers.Index;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers;

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
    public void Renders_no_customers_message_when_empty()
    {
        // Arrange
        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.FindAll(".alert.alert-info").Count > 0, TimeSpan.FromSeconds(2));

        // Assert
        var alert = cut.Find(".alert.alert-info");
        Assert.Contains("No customers found", alert.TextContent, StringComparison.Ordinal);
        Assert.Contains("Create your first customer", alert.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_customer_basic_information()
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
        cut.WaitForState(() => cut.Markup.Contains("John", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Assert
        Assert.Contains("John Doe", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("john.doe@example.com", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("+1234567890", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Brazilian", cut.Markup, StringComparison.Ordinal);
    }


    [Fact]
    public void Renders_name_column_with_firstName_and_lastName()
    {
        // Arrange
        _fakeCustomersApi.AddCustomer(BuildCustomerDto(firstName: "Jane", lastName: "Doe"));

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.Markup.Contains("Jane Doe", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Assert
        Assert.Contains("Jane Doe", cut.Markup, StringComparison.Ordinal);
    }
}
