namespace ViajantesTurismo.Admin.E2ETests.Tests;

public class ListInteractionTests(E2EFixture fixture) : E2ESerialTestBase(fixture)
{
    [Fact]
    public async Task Can_Sort_List_Columns_For_All_Entities()
    {
        // ── Tours: sort by Name, Start Date, Price, Capacity ──
        await NavigateToAsync("/tours");
        await Expect(Page).ToHaveTitleAsync("Tours");
        await Expect(Page.GetByText("Total tours: 5")).ToBeVisibleAsync();

        var toursTable = Page.Locator("table");

        // Do not assert default unsorted order, as backend default ordering is not guaranteed.
        var firstTourCell = toursTable.Locator("tbody tr td:nth-child(2)").First;

        // Sort by Name ascending
        await toursTable.GetButton("Name").ClickAsync();
        await Expect(toursTable.Locator("th[aria-sort='ascending']")).ToContainTextAsync("Name");
        await Expect(firstTourCell).ToHaveTextAsync("City Highlights"); // A-Z

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
        await Expect(Page.GetByText("Total customers: 15")).ToBeVisibleAsync();

        var customersTable = Page.Locator("table");
        var firstCustomerName = customersTable.Locator("tbody tr td:nth-child(1)").First;

        // Sort by Name ascending
        await customersTable.GetButton("Name").ClickAsync();
        await Expect(customersTable.Locator("th[aria-sort='ascending']")).ToContainTextAsync("Name");
        await Expect(firstCustomerName).ToHaveTextAsync("Alice Smith"); // A-Z: Alice first

        // Sort by Name descending
        await customersTable.GetButton("Name").ClickAsync();
        await Expect(customersTable.Locator("th[aria-sort='descending']")).ToContainTextAsync("Name");
        await Expect(firstCustomerName).ToHaveTextAsync("Oscar Fischer"); // Z-A: Oscar first

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
        await Expect(Page.Locator(".paginator")).Not.ToBeVisibleAsync();
        // All 5 tours visible (no items hidden on second page)
        await Expect(Page.Locator("table tbody tr")).ToHaveCountAsync(5);

        // Customers: 15 items (>10) → paginator shown (tested in pagination tests)
        await NavigateToAsync("/customers");
        await Expect(Page.GetByText("Total customers: 15")).ToBeVisibleAsync();
        await Expect(Page.Locator(".paginator")).ToBeVisibleAsync();
        await Expect(Page.Locator(".paginator .pagination-text")).ToContainTextAsync("Page 1 of 2");

        // Bookings: 10 items (≤10) → no paginator
        await NavigateToAsync("/bookings");
        await Expect(Page.GetByText("Total: 10")).ToBeVisibleAsync();
        await Expect(Page.Locator(".paginator")).Not.ToBeVisibleAsync();
        await Expect(Page.Locator("table tbody tr")).ToHaveCountAsync(10);
    }

    [Fact]
    public async Task Paginator_Shows_Correct_Page_Count()
    {
        // Customers: 15 items, page size 10 → paginator with 2 pages
        await NavigateToAsync("/customers");
        await Expect(Page.GetByText("Total customers: 15")).ToBeVisibleAsync();

        var paginator = Page.Locator(".paginator");
        await Expect(paginator).ToBeVisibleAsync();

        // Should show page indicator "Page 1 of 2"
        await Expect(paginator.Locator(".pagination-text")).ToContainTextAsync("Page 1 of 2");

        // Tours (5) and bookings (10) still show no paginator
        await NavigateToAsync("/tours");
        await Expect(Page.Locator(".paginator")).Not.ToBeVisibleAsync();

        await NavigateToAsync("/bookings");
        await Expect(Page.Locator(".paginator")).Not.ToBeVisibleAsync();
    }

    [Fact]
    public async Task Can_Navigate_Between_Pages()
    {
        await NavigateToAsync("/customers");
        await Expect(Page.GetByText("Total customers: 15")).ToBeVisibleAsync();

        var paginator = Page.Locator(".paginator");
        var paginationText = paginator.Locator(".pagination-text");

        // Page 1: shows "Page 1 of 2"
        await Expect(paginationText).ToContainTextAsync("Page 1 of 2");

        // Navigate to page 2
        await paginator.Locator("button[aria-label='Go to next page']").ClickAsync();

        // Page 2: shows "Page 2 of 2"
        await Expect(paginationText).ToContainTextAsync("Page 2 of 2");

        // Page 2 should contain Karen (11th customer alphabetically)
        await Expect(Page.GetByText("Karen Tanaka").First).ToBeVisibleAsync();

        // Navigate back to page 1
        await paginator.Locator("button[aria-label='Go to previous page']").ClickAsync();

        // Page 1: back to "Page 1 of 2"
        await Expect(paginationText).ToContainTextAsync("Page 1 of 2");

        // Page 1 should contain Alice (1st customer alphabetically)
        await Expect(Page.GetByText("Alice Smith").First).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Sorting_Persists_Across_Pages()
    {
        await NavigateToAsync("/customers");
        await Expect(Page.GetByText("Total customers: 15")).ToBeVisibleAsync();

        var customersTable = Page.Locator("table");

        // Sort by Name descending (Z-A)
        await customersTable.GetButton("Name").ClickAsync();
        await customersTable.GetButton("Name").ClickAsync();
        await Expect(customersTable.Locator("th[aria-sort='descending']")).ToContainTextAsync("Name");

        // Page 1: first item should be Oscar Fischer (Z-A)
        var firstNameCell = customersTable.Locator("tbody tr td:nth-child(1)").First;
        await Expect(firstNameCell).ToHaveTextAsync("Oscar Fischer");

        // Navigate to page 2
        var paginator = Page.Locator(".paginator");
        await paginator.Locator("button[aria-label='Go to next page']").ClickAsync();

        // Page 2: items should still be in descending order
        // Page 2 has the last 5 alphabetically (Z-A): Elena, David, Carla, Bob, Alice
        await Expect(firstNameCell).ToHaveTextAsync("Elena Rodriguez");

        // Sort indicator should persist
        await Expect(customersTable.Locator("th[aria-sort='descending']")).ToContainTextAsync("Name");
    }
}
