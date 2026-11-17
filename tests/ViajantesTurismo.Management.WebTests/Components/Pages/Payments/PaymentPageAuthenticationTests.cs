using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using PandaBear.Testing;
using ViajantesTurismo.Management.Web.Components.Pages.Payments;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Payments;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.ComponentCategory)]
[TestScope(TestScope.Component)]
public sealed class PaymentPageAuthenticationTests : BunitContext
{
    private PaymentPageHttpScenario Handler => Services.GetRequiredService<PaymentPageHttpScenario>();
    private readonly FakeBookingsApiClient _bookings = new();
    private readonly ChangeableAuthenticationStateProvider _authentication = new(new RecordingClaimsPrincipal("owner"));

    public PaymentPageAuthenticationTests()
    {
        Services.AddSingleton<PaymentPageHttpScenario>();
        Services.AddSingleton(provider => new HttpClient(provider.GetRequiredService<PaymentPageHttpScenario>().Transport, disposeHandler: false) { BaseAddress = new Uri("https://payments.example.test") });
        Services.AddSingleton<IBookingsApiClient>(_bookings);
        Services.AddSingleton<IBookingPaymentsApiClient>(provider => new BookingPaymentsApiClient(provider.GetRequiredService<HttpClient>()));
        Services.AddSingleton<IPaymentAccountsApiClient>(provider => new PaymentAccountsApiClient(provider.GetRequiredService<HttpClient>()));
        Services.AddSingleton<IPaymentInstructionsApiClient>(provider => new PaymentInstructionsApiClient(provider.GetRequiredService<HttpClient>()));
        Services.AddSingleton(TimeProvider.System);
        Services.AddSingleton<AuthenticationStateProvider>(_authentication);
        Services.AddAuthorizationCore();
        Services.AddSingleton<IAuthorizationService, DefaultAuthorizationService>();
        Services.AddSingleton<IAuthorizationPolicyProvider, DefaultAuthorizationPolicyProvider>();
        Services.AddCascadingAuthenticationState();
    }

    [Fact]
    public void Accounts_follows_an_authentication_change_for_the_independent_review_check()
    {
        // Arrange
        Handler.Accounts = PaymentPageData.Accounts;
        _authentication.Change(new RecordingClaimsPrincipal("other-admin"));
        var page = Render<Accounts>();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Count.ShouldBe(3));
        PaymentPageAuthentication.ReviewButtons(page, "Independent review (optional)").Count.ShouldBe(0);

        // Act
        _authentication.Change(new RecordingClaimsPrincipal("owner"));

        // Assert
        page.WaitForAssertion(() => PaymentPageAuthentication.ReviewButtons(page, "Independent review (optional)").Count.ShouldBeGreaterThan(0));
    }

    [Fact]
    public void Finances_follows_an_authentication_change_for_the_independent_review_check()
    {
        // Arrange
        var booking = BuildBookingDto() with
        {
            Currency = "EUR"
        };
        _bookings.AddBooking(booking);
        Handler.FinancialHistory = PaymentPageData.History;
        _authentication.Change(new RecordingClaimsPrincipal("other-admin"));
        var page = Render<Finances>(parameters => parameters.Add(value => value.BookingId, booking.Id));
        page.WaitForAssertion(() => page.Find("#schedule-obligation"));
        PaymentPageAuthentication.ReviewButtons(page, "Optional independent review").Count.ShouldBe(0);

        // Act
        _authentication.Change(new RecordingClaimsPrincipal("owner"));

        // Assert
        page.WaitForAssertion(() => PaymentPageAuthentication.ReviewButtons(page, "Optional independent review").Count.ShouldBeGreaterThan(0));
    }

    [Fact]
    public void Instructions_follows_an_authentication_change_for_the_independent_review_check()
    {
        // Arrange
        PaymentPageAuthentication.ServeReviewableInstruction(Handler);
        _authentication.Change(new RecordingClaimsPrincipal("other-admin"));
        var page = Render<Instructions>(parameters => parameters.Add(value => value.BookingId, Guid.CreateVersion7()));
        page.WaitForAssertion(() => page.Find("#instruction-account"));
        PaymentPageAuthentication.ReviewButtons(page, "Independent review (optional)").Count.ShouldBe(0);

        // Act
        _authentication.Change(new RecordingClaimsPrincipal("owner"));

        // Assert
        page.WaitForAssertion(() => PaymentPageAuthentication.ReviewButtons(page, "Independent review (optional)").Count.ShouldBeGreaterThan(0));
    }

    [Fact]
    public async Task Accounts_ignores_authentication_changes_after_disposal()
    {
        // Arrange
        Handler.Accounts = PaymentPageData.Accounts;
        var page = Render<Accounts>();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Count.ShouldBe(3));

        var rendered = new RecordingClaimsPrincipal("renderer");
        _authentication.Change(rendered);
        rendered.ClaimLookups.ShouldBeGreaterThan(0);
        await DisposeComponentsAsync();
        var disposed = new RecordingClaimsPrincipal("disposed");

        // Act
        _authentication.Change(disposed);

        // Assert
        disposed.ClaimLookups.ShouldBe(0);
    }

    [Fact]
    public async Task Finances_ignores_authentication_changes_after_disposal()
    {
        // Arrange
        var booking = BuildBookingDto() with
        {
            Currency = "EUR"
        };
        _bookings.AddBooking(booking);
        var page = Render<Finances>(parameters => parameters.Add(value => value.BookingId, booking.Id));
        page.WaitForAssertion(() => page.Find("#receipt-gross"));

        var rendered = new RecordingClaimsPrincipal("renderer");
        _authentication.Change(rendered);
        rendered.ClaimLookups.ShouldBeGreaterThan(0);
        await DisposeComponentsAsync();
        var disposed = new RecordingClaimsPrincipal("disposed");

        // Act
        _authentication.Change(disposed);

        // Assert
        disposed.ClaimLookups.ShouldBe(0);
    }

    [Fact]
    public async Task Instructions_ignores_authentication_changes_after_disposal()
    {
        // Arrange
        PaymentPageAuthentication.ServeReviewableInstruction(Handler);
        var page = Render<Instructions>(parameters => parameters.Add(value => value.BookingId, Guid.CreateVersion7()));
        page.WaitForAssertion(() => page.Find("#instruction-account"));

        var rendered = new RecordingClaimsPrincipal("renderer");
        _authentication.Change(rendered);
        rendered.ClaimLookups.ShouldBeGreaterThan(0);
        await DisposeComponentsAsync();
        var disposed = new RecordingClaimsPrincipal("disposed");

        // Act
        _authentication.Change(disposed);

        // Assert
        disposed.ClaimLookups.ShouldBe(0);
    }
}
