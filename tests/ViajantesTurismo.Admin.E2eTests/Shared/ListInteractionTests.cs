namespace ViajantesTurismo.Admin.E2ETests.Shared;

public class ListInteractionTests(E2EFixture fixture) : E2ESerialTestBase(fixture)
{
    [Fact]
    public async Task Tour_List_Sort_Smoke_Works_For_Name_Column()
    {
        // Arrange
        await ClearDatabase(TestContext.Current.CancellationToken);

        _ = await ApiClient.CreateTour(
            identifier: "AAA-SORT",
            name: "Aaa Sort Tour",
            startDate: DateTime.UtcNow.AddDays(10),
            endDate: DateTime.UtcNow.AddDays(17),
            price: 500m);
        _ = await ApiClient.CreateTour(
            identifier: "ZZZ-SORT",
            name: "Zzz Sort Tour",
            startDate: DateTime.UtcNow.AddDays(40),
            endDate: DateTime.UtcNow.AddDays(47),
            price: 1500m);

        // Act
        await NavigateTo("/tours");
        await Expect(Page).ToHaveTitleAsync("Tours");

        var toursTable = Page.Locator("table");
        var firstTourCell = toursTable.Locator("tbody tr td:nth-child(2)").First;

        // Assert: tours sort by Name ascending / descending.
        await toursTable.GetButton("Name").ClickAsync();
        await Expect(toursTable.Locator("th[aria-sort='ascending']")).ToContainTextAsync("Name");
        await Expect(firstTourCell).ToHaveTextAsync("Aaa Sort Tour");

        await toursTable.GetButton("Name").ClickAsync();
        await Expect(toursTable.Locator("th[aria-sort='descending']")).ToContainTextAsync("Name");
        await Expect(firstTourCell).ToHaveTextAsync("Zzz Sort Tour");
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
        await NavigateTo("/customers");
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
