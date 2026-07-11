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
        (alert.TextContent).ShouldContain("No customers found", StringComparison.Ordinal);
        (alert.TextContent).ShouldContain("Create your first customer", StringComparison.Ordinal);
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
        (cut.Markup).ShouldContain("John Doe", StringComparison.Ordinal);
        (cut.Markup).ShouldContain("john.doe@example.com", StringComparison.Ordinal);
        (cut.Markup).ShouldContain("+1234567890", StringComparison.Ordinal);
        (cut.Markup).ShouldContain("Brazilian", StringComparison.Ordinal);
    }


    [Fact]
    public void Renders_name_column_with_firstname_and_lastname()
    {
        // Arrange
        _fakeCustomersApi.AddCustomer(BuildCustomerDto(firstName: "Jane", lastName: "Doe"));

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.Markup.Contains("Jane Doe", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Assert
        (cut.Markup).ShouldContain("Jane Doe", StringComparison.Ordinal);
    }
}
