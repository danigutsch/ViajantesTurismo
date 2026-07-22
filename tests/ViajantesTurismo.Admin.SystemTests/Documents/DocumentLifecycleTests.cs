using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Testing;
using ViajantesTurismo.Admin.SystemTests.Infrastructure;

namespace ViajantesTurismo.Admin.SystemTests.Documents;

[Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.SystemScope)]
[Trait(SharedKernel.Testing.TestTraitNames.HostName, TestTraits.AspireHost)]
[Trait(SharedKernel.Testing.TestTraitNames.SurfaceName, TestTraits.AdminSurface)]
[Trait(SharedKernel.Testing.SharedKernelTestTraitNames.CapabilityName, AdminTestTraitValues.GeneratedDocumentsCapability)]
public sealed class DocumentLifecycleTests(AspireSystemTestFixture fixture)
    : AspireSystemTestBase<AspireSystemTestFixture>(fixture)
{
    [Fact]
    public async Task Admin_can_generate_review_edit_approve_finalize_and_download_a_contract_document()
    {
        // Arrange
        const string reviewedValue = "Reviewed customer greeting";
        var tour = await ApiClient.CreateTour();
        var customer = await ApiClient.CreateCustomer();
        var booking = await ApiClient.CreateConfirmedBooking(tour.Id, customer.Id);
        await BookingWorkflow.NavigateToDetails(booking.Id);

        // Act
        await Page.GetButton("Generate contract draft").ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Document Details");
        await Expect(Page.GetHeading("Document Details")).ToBeVisibleAsync();
        var status = Page.Locator("dt:text-is('Status')")
            .Locator("xpath=following-sibling::dd[1]");
        var revision = Page.Locator("dt:text-is('Revision')")
            .Locator("xpath=following-sibling::dd[1]");
        await Expect(status).ToHaveTextAsync("DraftGenerated");
        await Expect(revision).ToHaveTextAsync("1");

        var greeting = Page.GetByLabel("Greeting", new PageGetByLabelOptions { Exact = true });
        await greeting.FillAndExpectValue(reviewedValue);
        await Page.GetButton("Save Greeting").ClickAsync();
        await Expect(status).ToHaveTextAsync("InReview");
        await Expect(greeting).ToHaveValueAsync(reviewedValue);

        await Page.GetButton("Approve").ClickAsync();
        await Expect(status).ToHaveTextAsync("Approved");
        await Page.GetButton("Finalize").ClickAsync();
        await Expect(status).ToHaveTextAsync("Finalized");

        // Assert
        await Expect(Page.Locator("input#document-field-greeting")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#document-field-greeting")).ToHaveTextAsync(reviewedValue);
        await Expect(Page.GetButton("Save Greeting")).ToHaveCountAsync(0);
        var downloadLink = Page.GetLink("Download artifact", exact: true);
        await Expect(downloadLink).ToBeVisibleAsync();
        var documentUri = new Uri(Page.Url);
        var documentIdWasParsed = Guid.TryParse(documentUri.Segments[^1], out var documentId);
        documentIdWasParsed.ShouldBeTrue();
        var expectedDownloadPath = $"{documentUri.AbsolutePath}/download";
        await Expect(downloadLink).ToHaveAttributeAsync("href", expectedDownloadPath);

        var downloadTask = Page.WaitForDownloadAsync();
        await downloadLink.ClickAsync();
        var download = await downloadTask;
        var downloadFailure = await download.FailureAsync();
        downloadFailure.ShouldBeNull();
        download.SuggestedFilename.ShouldBe($"document-{documentId:N}-r1.html");
        var downloadUri = new Uri(download.Url);
        downloadUri.AbsolutePath.ShouldBe(expectedDownloadPath);
        using var artifactStream = await download.CreateReadStreamAsync();
        using var artifactReader = new StreamReader(artifactStream);
        var artifactHtml = await artifactReader.ReadToEndAsync(TestContext.Current.CancellationToken);
        artifactHtml.ShouldContain(reviewedValue, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Contract_draft_generation_fails_safely_when_the_booking_becomes_ineligible_after_page_load()
    {
        // Arrange
        var tour = await ApiClient.CreateTour();
        var customer = await ApiClient.CreateCustomer();
        var booking = await ApiClient.CreateConfirmedBooking(tour.Id, customer.Id);
        booking.Status.ShouldBe(BookingStatusDto.Confirmed);
        await NavigateTo($"/bookings/{booking.Id}");
        await Expect(Page).ToHaveTitleAsync("Booking Details");
        var bookingStatus = Page.GetDetailsBadge("Status");
        var generateButton = Page.GetButton("Generate contract draft");
        await Expect(bookingStatus).ToHaveTextAsync(nameof(BookingStatusDto.Confirmed));
        await Expect(generateButton).ToBeVisibleAsync();
        var cancelled = await ApiClient.CancelBooking(booking.Id);
        cancelled.Status.ShouldBe(BookingStatusDto.Cancelled);

        // Act
        await generateButton.ClickAsync();

        // Assert
        var alert = Page.Locator(".alert-danger[role='alert']");
        await Expect(alert).ToHaveTextAsync("The contract draft could not be generated.");
        var currentUri = new Uri(Page.Url);
        currentUri.AbsolutePath.ShouldBe($"/bookings/{booking.Id:D}");

        await NavigateTo($"/bookings/{booking.Id}");
        await Expect(Page.GetDetailsBadge("Status")).ToHaveTextAsync(nameof(BookingStatusDto.Cancelled));
        await Expect(Page.GetButton("Generate contract draft")).ToHaveCountAsync(0);
    }
}
