using ViajantesTurismo.Admin.E2ETests.Infrastructure.Api;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Bases;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Fixtures;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Helpers;

namespace ViajantesTurismo.Admin.E2ETests.Shared;

public class ListInteractionTests(E2EFixture fixture) : E2ESerialTestBase(fixture)
{
    [Fact]
    public async Task Can_Sort_List_Columns_For_All_Entities()
    {
        // Arrange
        await ClearDatabase(TestContext.Current.CancellationToken);

        var aTour = await ApiClient.CreateTour(
            identifier: "AAA-SORT",
            name: "Aaa Sort Tour",
            startDate: DateTime.UtcNow.AddDays(10),
            endDate: DateTime.UtcNow.AddDays(17),
            price: 500m);
        var zTour = await ApiClient.CreateTour(
            identifier: "ZZZ-SORT",
            name: "Zzz Sort Tour",
            startDate: DateTime.UtcNow.AddDays(40),
            endDate: DateTime.UtcNow.AddDays(47),
            price: 1500m);

        var aCustomer = await ApiClient.CreateCustomer(firstName: "Aaa", lastName: "Sort Customer");
        var zCustomer = await ApiClient.CreateCustomer(firstName: "Zzz", lastName: "Sort Customer");

        _ = await ApiClient.CreateBooking(aTour.Id, aCustomer.Id);
        _ = await ApiClient.CreateBooking(zTour.Id, zCustomer.Id);

        // Tours: sort by Name ascending / descending
        await NavigateToAsync("/tours");
        await Expect(Page).ToHaveTitleAsync("Tours");

        var toursTable = Page.Locator("table");
        var firstTourCell = toursTable.Locator("tbody tr td:nth-child(2)").First;

        await toursTable.GetButton("Name").ClickAsync();
        await Expect(toursTable.Locator("th[aria-sort='ascending']")).ToContainTextAsync("Name");
        await Expect(firstTourCell).ToHaveTextAsync("Aaa Sort Tour");

        await toursTable.GetButton("Name").ClickAsync();
        await Expect(toursTable.Locator("th[aria-sort='descending']")).ToContainTextAsync("Name");
        await Expect(firstTourCell).ToHaveTextAsync("Zzz Sort Tour");

        // Customers: sort by Name ascending / descending
        await NavigateToAsync("/customers");
        await Expect(Page).ToHaveTitleAsync("Customers");

        var customersTable = Page.Locator("table");
        var firstCustomerName = customersTable.Locator("tbody tr td:nth-child(1)").First;

        await customersTable.GetButton("Name").ClickAsync();
        await Expect(customersTable.Locator("th[aria-sort='ascending']")).ToContainTextAsync("Name");
        await Expect(firstCustomerName).ToHaveTextAsync("Aaa Sort Customer");

        await customersTable.GetButton("Name").ClickAsync();
        await Expect(customersTable.Locator("th[aria-sort='descending']")).ToContainTextAsync("Name");
        await Expect(firstCustomerName).ToHaveTextAsync("Zzz Sort Customer");

        // Bookings: sort by Tour ascending / descending
        await NavigateToAsync("/bookings");
        await Expect(Page).ToHaveTitleAsync("Bookings");

        var bookingsTable = Page.Locator("table");

        await bookingsTable.GetButton("Tour").ClickAsync();
        await Expect(bookingsTable.Locator("th[aria-sort='ascending']")).ToContainTextAsync("Tour");
        var firstTourLink = bookingsTable.Locator("tbody tr").First.GetLink("Aaa Sort Tour");
        await Expect(firstTourLink).ToBeVisibleAsync();

        await bookingsTable.GetButton("Tour").ClickAsync();
        await Expect(bookingsTable.Locator("th[aria-sort='descending']")).ToContainTextAsync("Tour");
        var firstTourLinkDesc = bookingsTable.Locator("tbody tr").First.GetLink("Zzz Sort Tour");
        await Expect(firstTourLinkDesc).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Customer_List_Paginator_Smoke_Works_And_Preserves_Sort()
    {
        // Arrange
        await ClearDatabase(TestContext.Current.CancellationToken);

        for (var index = 0; index <= 10; index++)
        {
            await ApiClient.CreateCustomer(firstName: $"User{index:00}", lastName: "List");
        }

        // Act
        await NavigateToAsync("/customers");
        await Expect(Page).ToHaveTitleAsync("Customers");

        var customersTable = Page.Locator("table");
        await Expect(Page.Locator(".paginator")).ToBeVisibleAsync();
        var paginator = Page.Locator(".paginator");
        var paginationText = paginator.Locator(".pagination-text");

        await Expect(paginationText).ToContainTextAsync("Page 1 of 2");

        await customersTable.GetButton("Name").ClickAsync();
        await customersTable.GetButton("Name").ClickAsync();
        await Expect(customersTable.Locator("th[aria-sort='descending']")).ToContainTextAsync("Name");

        var firstNameCell = customersTable.Locator("tbody tr td:nth-child(1)").First;
        await Expect(firstNameCell).ToHaveTextAsync("User10 List");

        await paginator.Locator("button[aria-label='Go to next page']").ClickAsync();
        await Expect(paginationText).ToContainTextAsync("Page 2 of 2");
        await Expect(firstNameCell).ToHaveTextAsync("User00 List");
        await Expect(customersTable.Locator("th[aria-sort='descending']")).ToContainTextAsync("Name");

        await paginator.Locator("button[aria-label='Go to previous page']").ClickAsync();
        await Expect(paginationText).ToContainTextAsync("Page 1 of 2");
        await Expect(firstNameCell).ToHaveTextAsync("User10 List");
        await Expect(customersTable.Locator("th[aria-sort='descending']")).ToContainTextAsync("Name");
    }
}
