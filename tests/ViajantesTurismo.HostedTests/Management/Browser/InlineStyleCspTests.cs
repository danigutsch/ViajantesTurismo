using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using SharedKernel.Testing;
using SharedKernel.Testing.Playwright;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Contracts.Http;
using ViajantesTurismo.Admin.Testing.Builders;
using ViajantesTurismo.Admin.Testing.Fakes.ApiClients;

namespace ViajantesTurismo.HostedTests.Management.Browser;

/// <summary>
/// Browser journeys for pages that once relied on inline <c>style</c> attributes, which the Management Web policy blocks: their
/// presentation comes from stylesheet classes and the browser reports no <c>style-src</c> violation.
/// </summary>
[Trait(TestTraitNames.AreaName, "management-web")]
[TestScope(TestScope.System)]
public sealed class InlineStyleCspTests : ManagementWebBrowserTestBase
{
    private readonly Guid bookingId = Guid.CreateVersion7();

    public InlineStyleCspTests()
    {
        var bookings = new FakeBookingsApiClient();
        bookings.AddBooking(DtoBuilders.BuildBookingDto(id: bookingId, notes: "First line\nSecond line"));
        Registrations.AddSingleton<IBookingsApiClient>(bookings);
        Registrations.AddSingleton<ICustomersApiClient>(new FakeCustomersApiClient());
        var now = TimeProvider.System.GetUtcNow();
        var unusedDocument = new GetDocumentDto
        {
            Id = Guid.CreateVersion7(),
            BookingId = bookingId,
            Revision = 1,
            TemplateId = "tour-service-contract",
            TemplateVersion = "1",
            SourceVersion = "SOURCE-VERSION",
            Status = DocumentStatusDto.DraftGenerated,
            Fields = [],
            CreatedAt = now.UtcDateTime,
            UpdatedAt = now.UtcDateTime,
            HasFinalizedArtifact = false
        };
        var unusedHold = new GetDocumentHoldDto
        {
            Id = Guid.CreateVersion7(),
            Basis = "LegalClaim",
            PurposeCode = "PURPOSE-1",
            AuthorityReference = "AUTH-1",
            PlacedBy = "actor-place",
            PlacedAt = now,
            EffectiveFrom = now,
            ReviewDueAt = now.AddDays(60),
            IsRestraining = true
        };
        Registrations.AddSingleton<IDocumentsApiClient>(new FakeDocumentsBrowserApiClient(unusedDocument, unusedHold));
    }

    [Fact]
    public async Task About_page_sizes_its_label_column_without_a_style_violation()
    {
        // Arrange
        using var recorder = await CspViolationRecorder.Start(Page);

        // Act
        await NavigateTo("/about");

        // Assert
        await Expect(Page.Locator("td").Filter(new LocatorFilterOptions { HasText = "Version:" })).ToHaveCSSAsync("width", "200px");
        var violations = await recorder.StyleViolations();
        violations.ShouldBeEmpty();
    }

    [Fact]
    public async Task Booking_details_keep_the_line_breaks_of_the_notes_without_a_style_violation()
    {
        // Arrange
        using var recorder = await CspViolationRecorder.Start(Page);

        // Act
        await NavigateTo($"/bookings/{bookingId}");

        // Assert
        var notes = Page.Locator("p").Filter(new LocatorFilterOptions { HasText = "First line" });
        await Expect(notes).ToHaveCSSAsync("white-space", "pre-wrap");
        var violations = await recorder.StyleViolations();
        violations.ShouldBeEmpty();
    }

    [Fact]
    public async Task Customer_import_draws_a_dashed_drop_zone_without_a_style_violation()
    {
        // Arrange
        using var recorder = await CspViolationRecorder.Start(Page);

        // Act
        await NavigateTo("/customers/import");

        // Assert
        var dropZone = Page.Locator("div.border").Filter(new LocatorFilterOptions { HasText = "Drop a CSV file here" });
        await Expect(dropZone).ToHaveCSSAsync("border-top-style", "dashed");
        await Expect(dropZone.Locator("input[type=file]")).ToHaveCSSAsync("cursor", "pointer");
        var violations = await recorder.StyleViolations();
        violations.ShouldBeEmpty();
    }
}
