namespace ViajantesTurismo.Admin.E2ETests.Tests;

public class ListInteractionTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Can_Sort_List_Columns_For_All_Entities()
    {
        // ── Tours: sort by Name, Start Date, Price, Capacity ──
        await NavigateToAsync("/tours");
        await Expect(Page).ToHaveTitleAsync("Tours");
        await Expect(Page.GetByText("Total tours: 5")).ToBeVisibleAsync();

        var toursTable = Page.Locator("table");

        // Default order: City Highlights first (unsorted / insertion order)
        var firstTourCell = toursTable.Locator("tbody tr td:nth-child(2)").First;
        await Expect(firstTourCell).ToHaveTextAsync("City Highlights");

        // Sort by Name ascending
        await toursTable.GetButton("Name").ClickAsync();
        await Expect(toursTable.Locator("th[aria-sort='ascending']")).ToContainTextAsync("Name");
        await Expect(firstTourCell).ToHaveTextAsync("City Highlights"); // A-Z: City is still first

        // Sort by Name descending
        await toursTable.GetButton("Name").ClickAsync();
        await Expect(toursTable.Locator("th[aria-sort='descending']")).ToContainTextAsync("Name");
        await Expect(firstTourCell).ToHaveTextAsync("Nature and Adventure"); // Z-A: Nature is first

        // Sort by Start Date ascending
        await toursTable.GetButton("Start Date").ClickAsync();
        await Expect(toursTable.Locator("th[aria-sort='ascending']")).ToContainTextAsync("Start Date");
        // City Highlights has the earliest start date, so it should appear first
        await Expect(firstTourCell).ToHaveTextAsync("City Highlights");

        // Sort by Price ascending
        await toursTable.GetButton("Price").ClickAsync();
        await Expect(toursTable.Locator("th[aria-sort='ascending']")).ToContainTextAsync("Price");

        // Sort by Capacity ascending
        await toursTable.GetButton("Capacity").ClickAsync();
        await Expect(toursTable.Locator("th[aria-sort='ascending']")).ToContainTextAsync("Capacity");

        // ── Customers: sort by Name, Email, Nationality ──
        await NavigateToAsync("/customers");
        await Expect(Page).ToHaveTitleAsync("Customers");
        await Expect(Page.GetByText("Total customers: 10")).ToBeVisibleAsync();

        var customersTable = Page.Locator("table");
        var firstCustomerName = customersTable.Locator("tbody tr td:nth-child(1)").First;

        // Sort by Name ascending
        await customersTable.GetButton("Name").ClickAsync();
        await Expect(customersTable.Locator("th[aria-sort='ascending']")).ToContainTextAsync("Name");
        await Expect(firstCustomerName).ToHaveTextAsync("Alice Smith"); // A-Z: Alice first

        // Sort by Name descending
        await customersTable.GetButton("Name").ClickAsync();
        await Expect(customersTable.Locator("th[aria-sort='descending']")).ToContainTextAsync("Name");
        await Expect(firstCustomerName).ToHaveTextAsync("Jack Brown"); // Z-A: Jack first

        // Sort by Email ascending
        await customersTable.GetButton("Email").ClickAsync();
        await Expect(customersTable.Locator("th[aria-sort='ascending']")).ToContainTextAsync("Email");
        await Expect(firstCustomerName).ToHaveTextAsync("Alice Smith"); // alice@ is first alphabetically

        // Sort by Nationality ascending
        await customersTable.GetButton("Nationality").ClickAsync();
        await Expect(customersTable.Locator("th[aria-sort='ascending']")).ToContainTextAsync("Nationality");
        await Expect(firstCustomerName).ToHaveTextAsync("Bob Johnson"); // American comes first

        // ── Bookings: sort by Booking Date, Tour, Customer, Total Price ──
        await NavigateToAsync("/bookings");
        await Expect(Page).ToHaveTitleAsync("Bookings");
        await Expect(Page.GetByText("Total: 10")).ToBeVisibleAsync();

        var bookingsTable = Page.Locator("table");

        // Sort by Tour ascending
        await bookingsTable.GetButton("Tour").ClickAsync();
        await Expect(bookingsTable.Locator("th[aria-sort='ascending']")).ToContainTextAsync("Tour");
        // City Highlights first alphabetically
        var firstTourLink = bookingsTable.Locator("tbody tr").First.GetLink("City Highlights");
        await Expect(firstTourLink).ToBeVisibleAsync();

        // Sort by Tour descending
        await bookingsTable.GetButton("Tour").ClickAsync();
        await Expect(bookingsTable.Locator("th[aria-sort='descending']")).ToContainTextAsync("Tour");
        var firstTourLinkDesc = bookingsTable.Locator("tbody tr").First.GetLink("Nature and Adventure");
        await Expect(firstTourLinkDesc).ToBeVisibleAsync();

        // Sort by Customer ascending
        await bookingsTable.GetButton("Customer").ClickAsync();
        await Expect(bookingsTable.Locator("th[aria-sort='ascending']")).ToContainTextAsync("Customer");

        // Sort by Total Price ascending
        await bookingsTable.GetButton("Total Price").ClickAsync();
        await Expect(bookingsTable.Locator("th[aria-sort='ascending']")).ToContainTextAsync("Total Price");

        // Sort by Total Price descending
        await bookingsTable.GetButton("Total Price").ClickAsync();
        await Expect(bookingsTable.Locator("th[aria-sort='descending']")).ToContainTextAsync("Total Price");
    }

    [Fact]
    public async Task Pagination_Not_Shown_When_Items_At_Or_Below_Page_Size()
    {
        // Tours: 5 items (≤10) → no paginator
        await NavigateToAsync("/tours");
        await Expect(Page.GetByText("Total tours: 5")).ToBeVisibleAsync();
        await Expect(Page.Locator("nav[aria-label='pagination']")).Not.ToBeVisibleAsync();
        // All 5 tours visible (no items hidden on second page)
        await Expect(Page.Locator("table tbody tr")).ToHaveCountAsync(5);

        // Customers: 10 items (≤10) → no paginator
        await NavigateToAsync("/customers");
        await Expect(Page.GetByText("Total customers: 10")).ToBeVisibleAsync();
        await Expect(Page.Locator("nav[aria-label='pagination']")).Not.ToBeVisibleAsync();
        await Expect(Page.Locator("table tbody tr")).ToHaveCountAsync(10);

        // Bookings: 10 items (≤10) → no paginator
        await NavigateToAsync("/bookings");
        await Expect(Page.GetByText("Total: 10")).ToBeVisibleAsync();
        await Expect(Page.Locator("nav[aria-label='pagination']")).Not.ToBeVisibleAsync();
        await Expect(Page.Locator("table tbody tr")).ToHaveCountAsync(10);
    }
}
