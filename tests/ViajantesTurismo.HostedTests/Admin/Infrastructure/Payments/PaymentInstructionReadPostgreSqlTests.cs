using PandaBear.BuildingBlocks;
using PandaBear.Idempotency;
using PandaBear.Testing;
using ViajantesTurismo.Admin.Application.Payments;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.PaymentAccounts;
using ViajantesTurismo.Admin.Domain.Payments;
using ViajantesTurismo.Admin.Testing.Behavior;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Payments;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
[Trait(SharedKernelTestTraitNames.CapabilityName, TestTraits.PaymentsCapability)]
[TestScope(TestScope.Integration)]
public sealed class PaymentInstructionReadPostgreSqlTests(PostgreSqlTestServerFixture fixture) : IClassFixture<PostgreSqlTestServerFixture>
{
    [Fact]
    public async Task Retained_revision_reads_preserve_masking_and_reviews_without_loading_the_write_aggregate()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await PostgreSqlPaymentScenario.Create(fixture, ct);
        var now = scenario.Clock.GetUtcNow();
        var tour = EntityBuilders.BuildTour(new TourOptions(Pricing: new TourPricingOptions(Currency: Currency.Eur)));
        var booking = BookingTestHelpers.AddSingleCustomerBooking(tour).Value;
        var otherBooking = BookingTestHelpers.AddSingleCustomerBooking(tour).Value;
        var installment = PaymentInstallment.Create(100m, new DateOnly(2030, 1, 10)).Value;
        var schedule = tour.DraftBookingPaymentSchedule(booking.Id, 0, 100m, [installment], "owner", now).Value;
        var history = booking.FinancialHistory.ShouldNotBeNull();
        tour.ApproveBookingPaymentSchedule(booking.Id, history.Version, schedule,
            FinancialApproval.Create("owner", now, FinancialReason.InitialObligation, true).Value).IsSuccess.ShouldBeTrue();
        var recipient = PaymentRecipientDetails.CreateIban("Synthetic recipient", "DE89370400440532013000", "COBADEFFXXX", Currency.Eur).Value;
        var account = PaymentAccount.Create("Verified recipient", recipient, "owner", now).Value;
        account.Verify(account.Version, "owner", now, PaymentAccountVerificationMethod.BankStatement, now, true, true).IsSuccess.ShouldBeTrue();
        var request = new GeneratePaymentInstructionsDto
        {
            ExpectedVersion = history.Version,
            AccountId = account.Id,
            ExpectedAccountVersion = account.Version,
            InstallmentIds = [installment.Id]
        };
        Guid firstId;
        Guid lastId;
        await using (var writer = scenario.Open())
        {
            await writer.Migrate(ct);
            await writer.Seed(tour, ct);
            await writer.SeedAccount(account, ct);
            var first = await writer.Send(new GeneratePaymentInstructionsCommand(booking.Id, null, request, "owner", IdempotencyKey.From("first")), ct);
            first.IsSuccess.ShouldBeTrue();
            firstId = first.Value;
            var initial = await writer.Instructions.GetById(firstId, ct);
            var firstRevision = initial.ShouldNotBeNull();
            var review = await writer.Instructions.Review(firstId,
                new ReviewPaymentInstructionsDto { ExpectedDocumentVersion = firstRevision.DocumentVersion, ChangesRequested = true },
                "reviewer", IdempotencyKey.From("review-first"), ct);
            review.IsSuccess.ShouldBeTrue();
            var current = await writer.Instructions.GetById(firstId, ct);
            var reviewed = current.ShouldNotBeNull();
            var replacement = request with
            {
                ExpectedDocumentVersion = reviewed.DocumentVersion
            };
            var last = await writer.Send(new GeneratePaymentInstructionsCommand(booking.Id, firstId,
                replacement, "owner", IdempotencyKey.From("regenerate")), ct);
            last.IsSuccess.ShouldBeTrue();
            lastId = last.Value;
        }

        // Act
        await using var reader = scenario.Open();
        var retained = await reader.Instructions.ListForBooking(booking.Id, ct);
        var newest = await reader.Instructions.ListPageForBooking(booking.Id, page: null, pageSize: 1, ct);
        var newestSummary = await reader.Instructions.ListPageForBooking(booking.Id, page: 1, pageSize: 1, latestReviewOnly: true, ct);
        var oldest = await reader.Instructions.ListPageForBooking(booking.Id, page: 99, pageSize: 1, ct);
        var other = await reader.Instructions.ListPageForBooking(otherBooking.Id, page: 1, pageSize: 1, ct);

        // Assert
        retained.Select(item => item.Document.Id).ShouldBe([lastId, firstId]);
        retained[1].Reviews.ShouldHaveSingleItem().ReviewerId.ShouldBe("reviewer");
        retained[0].Reviews.ShouldBeEmpty();
        retained[1].MaskedRecipient.ShouldBe("…3000");
        newest.Items.ShouldHaveSingleItem().Document.Id.ShouldBe(lastId);
        newestSummary.Items.ShouldHaveSingleItem().Reviews.ShouldBeEmpty();
        newest.Page.ShouldBe(1);
        newest.Total.ShouldBe(2);
        var oldestRevision = oldest.Items.ShouldHaveSingleItem();
        oldestRevision.Document.Id.ShouldBe(firstId);
        oldestRevision.DocumentVersion.ShouldBe(retained[0].DocumentVersion);
        oldestRevision.Reviews.ShouldHaveSingleItem().ReviewerId.ShouldBe("reviewer");
        oldestRevision.MaskedRecipient.ShouldBe("…3000");
        oldest.Page.ShouldBe(2);
        oldest.Total.ShouldBe(2);
        other.Items.ShouldBeEmpty();
        other.Total.ShouldBe(0);
        reader.TrackedInstructionRevisions.ShouldBe(0);
    }
}
