namespace ViajantesTurismo.Admin.E2ETests.Tests;

public class PaymentStatusConsistencyTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Bookings_List_Payment_Status_Matches_Booking_Details()
    {
        // Navigate to /bookings list page
        await NavigateToAsync("/bookings");
        await Expect(Page).ToHaveTitleAsync("Bookings");

        // Wait for table to render before collecting data
        var rows = Page.Locator("table tbody tr");
        await Expect(rows.First).ToBeVisibleAsync();
        var rowCount = await rows.CountAsync();

        var listStatuses = new Dictionary<string, string>();

        for (var i = 0; i < rowCount; i++)
        {
            var row = rows.Nth(i);
            var viewLink = row.GetLink("View");
            await Expect(viewLink).ToBeVisibleAsync();
            var href = await viewLink.GetAttributeAsync("href");
            Assert.NotNull(href);

            var paymentBadge = row.Locator("td:nth-child(8) .badge");
            await Expect(paymentBadge).ToBeVisibleAsync();
            var paymentText = (await paymentBadge.InnerTextAsync()).Trim();

            listStatuses[href] = paymentText;
        }

        // Verify at least one booking is not "Unpaid" (proves Include(Payments) is working)
        Assert.Contains(listStatuses.Values, status => status != "Unpaid");

        // Navigate to each booking's detail page and compare payment status
        foreach (var (href, listPaymentStatus) in listStatuses)
        {
            await NavigateToAsync(href);
            await Expect(Page).ToHaveTitleAsync("Booking Details");

            // Wait for badges to render, then find payment status badge
            var badges = Page.Locator("dd .badge");
            await Expect(badges.Nth(1)).ToBeVisibleAsync();
            var detailPaymentText = (await badges.Nth(1).InnerTextAsync()).Trim();

            Assert.Equal(listPaymentStatus, detailPaymentText);
        }
    }

    [Fact]
    public async Task Scoped_Bookings_Payment_Status_Matches_Global_List()
    {
        // Collect payment status from global /bookings list
        await NavigateToAsync("/bookings");
        await Expect(Page).ToHaveTitleAsync("Bookings");

        var globalRows = Page.Locator("table tbody tr");
        await Expect(globalRows.First).ToBeVisibleAsync();
        var globalCount = await globalRows.CountAsync();

        var globalStatuses = new Dictionary<string, string>();

        for (var i = 0; i < globalCount; i++)
        {
            var row = globalRows.Nth(i);
            var viewLink = row.GetLink("View");
            await Expect(viewLink).ToBeVisibleAsync();
            var href = await viewLink.GetAttributeAsync("href");
            Assert.NotNull(href);

            var paymentBadge = row.Locator("td:nth-child(8) .badge");
            await Expect(paymentBadge).ToBeVisibleAsync();
            var paymentText = (await paymentBadge.InnerTextAsync()).Trim();

            globalStatuses[href] = paymentText;
        }

        // Navigate to a tour details page and check its scoped bookings list
        await NavigateToAsync("/tours");
        var tourRow = Page.Locator("table tbody tr").First;
        await Expect(tourRow).ToBeVisibleAsync();
        await tourRow.GetLink("View").ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Tour Details");

        var scopedRows = Page.Locator(".table tbody tr");
        await Expect(scopedRows.First).ToBeVisibleAsync();
        var scopedCount = await scopedRows.CountAsync();

        for (var i = 0; i < scopedCount; i++)
        {
            var row = scopedRows.Nth(i);
            var viewLink = row.GetLink("View");
            var href = await viewLink.GetAttributeAsync("href");
            if (href is null || !globalStatuses.TryGetValue(href, out var expectedStatus))
            {
                continue;
            }

            var paymentBadge = row.Locator("td .badge").Last;
            await Expect(paymentBadge).ToBeVisibleAsync();
            var scopedText = (await paymentBadge.InnerTextAsync()).Trim();

            Assert.Equal(expectedStatus, scopedText);
        }
    }
}
