using SharedKernel.Testing;
using SharedKernel.Testing.AspNetCore;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Contracts.Http;
using ViajantesTurismo.HostedTests.Management.Browser;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Payments;

[Trait(TestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
[Trait(TestTraitNames.CapabilityName, TestTraits.PaymentsCapability)]
[TestScope(TestScope.Integration)]
public sealed class PaymentInstructionChoicePostgreSqlTests(PostgreSqlTestServerFixture fixture) : IClassFixture<PostgreSqlTestServerFixture>
{
    [Fact]
    public async Task Instruction_choice_pages_filter_eligibility_first_and_keep_management_prerender_bounded()
    {
        // Arrange
        const int choiceCount = 803;
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await PostgreSqlPaymentScenario.Create(fixture, ct);
        var choices = await PaymentInstructionChoiceScenario.Seed(scenario, choiceCount, ct);
        await using var api = scenario.StartAdminApi();
        using var adminHttp = api.CreateClient();
        PostgreSqlPaymentScenario.Authenticate(adminHttp, "Admin");
        var payments = new BookingPaymentsApiClient(adminHttp);
        await using var management = await ManagementWebBrowserHost.StartWithPayments(adminHttp, ct);
        using var browser = new HttpClient { BaseAddress = management.BaseUri, Timeout = TimeSpan.FromSeconds(15) };

        // Act
        var quotes = await payments.GetPage(choices.BookingId,
            new BookingFinancialPageRequestDto { Section = BookingFinancialSection.InstructionSettlementQuoteChoices }, ct);
        var installments = await payments.GetPage(choices.BookingId,
            new BookingFinancialPageRequestDto { Section = BookingFinancialSection.InstructionOutstandingInstallmentChoices, Page = 999 }, ct);
        var html = await browser.GetStringAsync(new Uri($"/bookings/{choices.BookingId}/payment-instructions", UriKind.Relative), ct);

        // Assert
        var quotePage = quotes.Value.ShouldNotBeNull();
        quotePage.History.Version.ShouldBe(choices.Version);
        quotePage.Windows.ShouldHaveSingleItem().TotalCount.ShouldBe(choiceCount);
        quotePage.SettlementQuoteChoices.Count.ShouldBe(25);
        quotePage.SettlementQuoteChoices[0].Id.ShouldBe(choices.FirstQuote);
        quotePage.SettlementQuoteChoices.ShouldAllSatisfy(quote => quote.ApprovalWithdrawn.ShouldBeFalse());
        var installmentPage = installments.Value.ShouldNotBeNull();
        installmentPage.History.Version.ShouldBe(choices.Version);
        var window = installmentPage.Windows.ShouldHaveSingleItem();
        window.Page.ShouldBe(33);
        window.TotalCount.ShouldBe(choiceCount);
        installmentPage.ApprovedInstallmentChoices.Count.ShouldBe(3);
        installmentPage.ApprovedInstallmentChoices[^1].Id.ShouldBe(choices.LastInstallment);
        installmentPage.ApprovedInstallmentChoices.ShouldAllSatisfy(installment => installment.OutstandingEuro.ShouldBeGreaterThan(0m));
        html.ShouldContain("Approved quote pages", StringComparison.Ordinal);
        html.ShouldContain(choices.FirstQuote.ToString("D"), StringComparison.Ordinal);
        html.ShouldContain(choices.FirstInstallment.ToString("D"), StringComparison.Ordinal);
        html.ShouldNotContain(choices.LastQuote.ToString("D"), StringComparison.Ordinal);
        html.ShouldNotContain(choices.LastInstallment.ToString("D"), StringComparison.Ordinal);
        html.ShouldNotContain(choices.WithdrawnQuote.ToString("D"), StringComparison.Ordinal);
        html.ShouldNotContain(choices.PaidInstallment.ToString("D"), StringComparison.Ordinal);
        PrerenderedComponentState.ShouldKeepServerStateBelow(html);
    }
}
