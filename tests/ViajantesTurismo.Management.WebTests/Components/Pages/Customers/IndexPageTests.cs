using Microsoft.AspNetCore.Components.QuickGrid;
using SharedKernel.Testing;
using Index = ViajantesTurismo.Management.Web.Components.Pages.Customers.Index;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers;

[TestScope(TestScope.Component)]
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
        alert.TextContent.ShouldContain("No customers found", StringComparison.Ordinal);
        alert.TextContent.ShouldContain("Create your first customer", StringComparison.Ordinal);
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
        cut.Markup.ShouldContain("John Doe", StringComparison.Ordinal);
        cut.Markup.ShouldContain("john.doe@example.com", StringComparison.Ordinal);
        cut.Markup.ShouldContain("+1234567890", StringComparison.Ordinal);
        cut.Markup.ShouldContain("Brazilian", StringComparison.Ordinal);
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
        cut.Markup.ShouldContain("Jane Doe", StringComparison.Ordinal);
    }

    [Fact]
    public void Transport_failure_while_loading_shows_a_safe_error_message()
    {
        // Arrange
        _fakeCustomersApi.SetGetCustomerPageOutcome(FakeApiClientOutcomes.TransportFailure<GetCustomerPageDto>());

        // Act
        var cut = Render<Index>();

        // Assert
        cut.WaitForAssertion(() => cut.Find("[role='alert']").TextContent.ShouldBe("Customers could not be loaded. Try again later."));
    }

    [Theory]
    [InlineData("Name", CustomerPageSort.Name)]
    [InlineData("Email", CustomerPageSort.Email)]
    [InlineData("Nationality", CustomerPageSort.Nationality)]
    public async Task Large_customer_grid_pages_and_sorts_through_the_server(string title, CustomerPageSort sort)
    {
        // Arrange
        foreach (var index in Enumerable.Range(0, 125))
        {
            _fakeCustomersApi.AddCustomer(BuildCustomerDto(firstName: $"Fixture{index:D3}",
                email: $"customer-{index:D3}@example.test", nationality: index % 2 == 0 ? "Brazilian" : "Portuguese"));
        }

        // Act
        var cut = Render<Index>();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Fixture009", StringComparison.Ordinal));
        var initialMarkup = cut.Markup;
        var initialRequest = _fakeCustomersApi.LastCustomerPageRequest;
        var pagination = cut.FindComponent<Paginator>().Instance.State;
        await cut.InvokeAsync(() => pagination.SetCurrentPageIndexAsync(10));
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Fixture109", StringComparison.Ordinal));
        var laterMarkup = cut.Markup;
        var laterRequest = _fakeCustomersApi.LastCustomerPageRequest;
        ColumnBase<GetCustomerListItemDto> column = sort == CustomerPageSort.Name
            ? cut.FindComponents<TemplateColumn<GetCustomerListItemDto>>().ShouldHaveSingleItem(item => item.Instance.Title == title).Instance
            : cut.FindComponents<PropertyColumn<GetCustomerListItemDto, string>>().ShouldHaveSingleItem(item => item.Instance.Title == title).Instance;
        var grid = cut.FindComponent<QuickGrid<GetCustomerListItemDto>>().Instance;
        await cut.InvokeAsync(() => grid.SortByColumnAsync(column, SortDirection.Descending));
        var descendingRequest = _fakeCustomersApi.LastCustomerPageRequest;
        await cut.InvokeAsync(() => grid.SortByColumnAsync(column, SortDirection.Ascending));
        var ascendingRequest = _fakeCustomersApi.LastCustomerPageRequest;

        // Assert
        initialMarkup.ShouldContain("Total customers: 125", StringComparison.Ordinal);
        initialMarkup.ShouldNotContain("Fixture010", StringComparison.Ordinal);
        laterMarkup.ShouldNotContain("Fixture110", StringComparison.Ordinal);
        initialRequest.ShouldBe((1, CustomerPageSort.Name, false));
        laterRequest.ShouldBe((11, CustomerPageSort.Name, false));
        var (_, descendingSort, wasDescending) = descendingRequest.ShouldNotBeNull();
        descendingSort.ShouldBe(sort);
        wasDescending.ShouldBeTrue();
        var (_, ascendingSort, stillDescending) = ascendingRequest.ShouldNotBeNull();
        ascendingSort.ShouldBe(sort);
        stillDescending.ShouldBeFalse();
    }

    [Fact]
    public async Task Later_page_failure_shows_the_safe_error_without_leaving_a_stale_page()
    {
        // Arrange
        foreach (var index in Enumerable.Range(0, 15))
        {
            _fakeCustomersApi.AddCustomer(BuildCustomerDto(firstName: $"Fixture{index:D3}"));
        }

        var cut = Render<Index>();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Fixture009", StringComparison.Ordinal));
        var pagination = cut.FindComponent<Paginator>().Instance.State;
        _fakeCustomersApi.SetGetCustomerPageOutcome(FakeApiClientOutcomes.TransportFailure<GetCustomerPageDto>());

        // Act
        await cut.InvokeAsync(() => pagination.SetCurrentPageIndexAsync(1));

        // Assert
        cut.WaitForAssertion(() => cut.Find("[role='alert']").TextContent.ShouldBe("Customers could not be loaded. Try again later."));
        cut.FindAll("tbody tr").ShouldBeEmpty();
    }
}
